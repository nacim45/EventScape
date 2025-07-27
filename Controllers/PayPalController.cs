using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using soft20181_starter.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;

namespace soft20181_starter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayPalController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EventAppDbContext _context;
        private readonly ILogger<PayPalController> _logger;

        public PayPalController(IConfiguration config, IHttpClientFactory httpClientFactory, EventAppDbContext context, ILogger<PayPalController> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _logger = logger;
            
            // Validate PayPal configuration on startup
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];
            
            // Log configuration status
            _logger.LogInformation("PayPal Controller Initialization:");
            _logger.LogInformation("PayPal Client ID: {Configured} (Length: {Length})", 
                !string.IsNullOrEmpty(clientId), clientId?.Length ?? 0);
            _logger.LogInformation("PayPal Secret: {Configured} (Length: {Length})", 
                !string.IsNullOrEmpty(secret), secret?.Length ?? 0);
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("PayPal credentials not configured. PayPal payment processing will be disabled.");
            }
            else
            {
                _logger.LogInformation("PayPal configuration loaded successfully.");
            }
        }

        /// <summary>
        /// Creates a PayPal order - Official PayPal implementation
        /// </summary>
        [HttpPost("create-order")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] PayPalOrderRequest request)
        {
            try
            {
                // Check if PayPal is configured
                var clientId = _config["PayPal:ClientId"];
                var secret = _config["PayPal:Secret"];
                
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret))
                {
                    _logger.LogWarning("PayPal order creation attempt with unconfigured PayPal");
                    return StatusCode(503, new { error = "PayPal payment system is not configured. Please contact support." });
                }

                _logger.LogInformation("Creating PayPal order for user {UserId} with amount {Amount}", 
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, request.Amount);

                // Validate request
                if (request.Amount <= 0)
                {
                    _logger.LogWarning("Invalid amount requested: {Amount}", request.Amount);
                    return BadRequest(new { error = "Invalid amount" });
                }

                // Check if PayPal credentials are configured
                var client = _httpClientFactory.CreateClient();
                
                try
                {
                    var accessToken = await GetAccessTokenAsync(client);
                    _logger.LogInformation("PayPal access token obtained successfully");

                    // Set authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Create PayPal order request body following official documentation
                    var orderRequest = new
                    {
                        intent = "CAPTURE",
                        purchase_units = new[]
                        {
                            new {
                                amount = new {
                                    currency_code = request.Currency ?? "GBP",
                                    value = request.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                },
                                description = "EventScape Event Payment",
                                custom_id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            }
                        },
                        application_context = new
                        {
                            brand_name = "EventScape",
                            landing_page = "LOGIN",
                            user_action = "PAY_NOW",
                            return_url = $"{Request.Scheme}://{Request.Host}/PaymentSuccess",
                            cancel_url = $"{Request.Scheme}://{Request.Host}/Payment"
                        }
                    };

                    var jsonContent = JsonSerializer.Serialize(orderRequest);
                    _logger.LogDebug("PayPal order request: {Request}", jsonContent);

                    var response = await client.PostAsync(
                        "https://api-m.paypal.com/v2/checkout/orders",
                        new StringContent(jsonContent, Encoding.UTF8, "application/json")
                    );

                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("PayPal create order response: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("PayPal create order failed: {StatusCode} - {Response}", 
                            response.StatusCode, responseContent);
                        return StatusCode((int)response.StatusCode, new { error = responseContent });
                    }

                    var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent);
                    _logger.LogInformation("PayPal order created successfully: {OrderId}", orderResponse?.Id);
                    return Ok(orderResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PayPal API communication");
                    return StatusCode(500, new { error = $"PayPal API error: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Captures a PayPal payment - Official PayPal implementation
        /// </summary>
        [HttpPost("capture-order")]
        [Authorize]
        public async Task<IActionResult> CaptureOrder([FromBody] PayPalCaptureRequest request)
        {
            try
            {
                _logger.LogInformation("Capturing PayPal order {OrderId}", request.OrderId);

                if (string.IsNullOrEmpty(request.OrderId))
                {
                    return BadRequest(new { error = "Order ID is required" });
                }

                var client = _httpClientFactory.CreateClient();
                var accessToken = await GetAccessTokenAsync(client);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.PostAsync(
                    $"https://api-m.paypal.com/v2/checkout/orders/{request.OrderId}/capture",
                    null
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("PayPal capture response: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayPal capture failed: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    return StatusCode((int)response.StatusCode, new { error = responseContent });
                }

                var captureResponse = JsonSerializer.Deserialize<PayPalCaptureResponse>(responseContent);
                
                // Process successful payment
                if (captureResponse.Status == "COMPLETED")
                {
                    await ProcessSuccessfulPaymentAsync(captureResponse);
                }

                return Ok(captureResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check endpoint for PayPal integration
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var clientId = _config["PayPal:ClientId"];
                var secret = _config["PayPal:Secret"];
                
                var healthInfo = new
                {
                    status = "checking",
                    message = "Checking PayPal integration...",
                    timestamp = DateTime.UtcNow,
                    config = new
                    {
                        clientIdConfigured = !string.IsNullOrEmpty(clientId),
                        secretConfigured = !string.IsNullOrEmpty(secret),
                        clientIdLength = clientId?.Length ?? 0,
                        secretLength = secret?.Length ?? 0
                    }
                };

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret))
                {
                    return StatusCode(500, new
                    {
                        status = "unhealthy",
                        error = "PayPal credentials not configured",
                        details = healthInfo,
                        timestamp = DateTime.UtcNow
                    });
                }

            var client = _httpClientFactory.CreateClient();
                var accessToken = await GetAccessTokenAsync(client);
                
                return Ok(new { 
                    status = "healthy", 
                    message = "PayPal integration is working correctly",
                    accessTokenLength = accessToken?.Length ?? 0,
                    accessTokenPrefix = !string.IsNullOrEmpty(accessToken) ? accessToken.Substring(0, Math.Min(10, accessToken.Length)) + "..." : "null",
                    timestamp = DateTime.UtcNow,
                    config = healthInfo.config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayPal health check failed");
                return StatusCode(500, new { 
                    status = "unhealthy", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Handles PayPal Webhook events (e.g., payment completed, refunded, etc.)
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var headers = Request.Headers;

            // --- Signature Verification Placeholder ---
            // For full security, verify the webhook signature using PayPal's API.
            // See: https://developer.paypal.com/docs/api/webhooks/v1/#verify-webhook-signature_post
            // You can use the PayPal .NET SDK or make a direct API call.
            // Example (pseudo-code):
            // var isValid = await VerifyPayPalWebhookSignature(headers, body);
            // if (!isValid) return Unauthorized();

            // --- Parse the event ---
            try
            {
                var jsonDoc = JsonDocument.Parse(body);
                var root = jsonDoc.RootElement;
                var eventType = root.GetProperty("event_type").GetString();
                var resource = root.GetProperty("resource");

                _logger.LogInformation("Received PayPal webhook event: {EventType}", eventType);

                switch (eventType)
                {
                    case "PAYMENT.SALE.COMPLETED":
                        // Handle completed payment
                        await HandlePayPalPaymentCompleted(resource);
                        break;
                    case "PAYMENT.SALE.REFUNDED":
                        // Handle refund
                        await HandlePayPalRefund(resource);
                        break;
                    // Add more cases as needed
                    default:
                        _logger.LogInformation("Unhandled PayPal webhook event: {EventType}", eventType);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal webhook");
                return BadRequest();
            }
        }

        // --- Helper methods for webhook event handling ---
        private async Task HandlePayPalPaymentCompleted(JsonElement resource)
        {
            try
            {
                var orderId = resource.GetProperty("parent_payment").GetString();
                var saleId = resource.GetProperty("id").GetString();
                var amount = resource.GetProperty("amount").GetProperty("total").GetString();
                var currency = resource.GetProperty("amount").GetProperty("currency").GetString();
                var payerEmail = resource.GetProperty("payer" ).GetProperty("payer_info").GetProperty("email").GetString();

                // Log the payment
                _logger.LogInformation("PayPal payment completed. SaleId: {SaleId}, OrderId: {OrderId}, Amount: {Amount} {Currency}, Payer: {PayerEmail}", saleId, orderId, amount, currency, payerEmail);

                // TODO: Update your database records as needed
                // Example: Mark the transaction as paid, update event attendance, etc.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal payment completed event");
            }
        }

        private async Task HandlePayPalRefund(JsonElement resource)
        {
            try
            {
                var saleId = resource.GetProperty("sale_id").GetString();
                var refundId = resource.GetProperty("id").GetString();
                var amount = resource.GetProperty("amount").GetProperty("total").GetString();
                var currency = resource.GetProperty("amount").GetProperty("currency").GetString();

                _logger.LogInformation("PayPal payment refunded. RefundId: {RefundId}, SaleId: {SaleId}, Amount: {Amount} {Currency}", refundId, saleId, amount, currency);

                // TODO: Update your database records as needed
                // Example: Mark the transaction as refunded, update event attendance, etc.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal refund event");
            }
        }

        /// <summary>
        /// Get PayPal access token using OAuth 2.0 - Official implementation
        /// </summary>
        private async Task<string> GetAccessTokenAsync(HttpClient client)
        {
            try
            {
                var clientId = _config["PayPal:ClientId"];
                var secret = _config["PayPal:Secret"];

                _logger.LogDebug("Attempting to get PayPal access token with ClientId: {ClientId}", 
                    string.IsNullOrEmpty(clientId) ? "MISSING" : "CONFIGURED");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret))
                {
                    _logger.LogError("PayPal credentials not configured. ClientId: {ClientId}, Secret: {Secret}", 
                        string.IsNullOrEmpty(clientId) ? "MISSING" : "CONFIGURED", 
                        string.IsNullOrEmpty(secret) ? "MISSING" : "CONFIGURED");
                    throw new InvalidOperationException("PayPal credentials not configured");
                }

                // Clear existing headers
                client.DefaultRequestHeaders.Clear();

                // Set Basic Authentication
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                // Create form data for OAuth token request
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                };

                var content = new FormUrlEncodedContent(formData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                _logger.LogDebug("Requesting PayPal access token from: https://api-m.paypal.com/v1/oauth2/token");

                var response = await client.PostAsync("https://api-m.paypal.com/v1/oauth2/token", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("PayPal token response: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayPal token request failed: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    throw new InvalidOperationException($"PayPal authentication failed: {response.StatusCode} - {responseContent}");
                }

                // Try to parse the response as JSON first
                try
                {
                    var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(responseContent);
                    
                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        _logger.LogDebug("PayPal access token obtained successfully");
                        return tokenResponse.AccessToken;
                    }
                    else
                    {
                        // Log the raw response for debugging
                        _logger.LogError("PayPal access token is null or empty in parsed response. Raw response: {Response}", responseContent);

                        // Try to parse the error message from PayPal
                        try
                        {
                            using var doc = JsonDocument.Parse(responseContent);
                            if (doc.RootElement.TryGetProperty("error", out var errorProp))
                            {
                                var error = errorProp.GetString();
                                var desc = doc.RootElement.TryGetProperty("error_description", out var descProp) ? descProp.GetString() : "";
                                throw new InvalidOperationException($"PayPal token error: {error} - {desc}");
                            }
                        }
                        catch { /* ignore parse errors */ }

                        throw new InvalidOperationException("PayPal access token is null or empty in parsed response");
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to parse PayPal token response as JSON. Raw response: {Response}", responseContent);
                    
                    // Try to extract token manually from response
                    if (responseContent.Contains("access_token"))
                    {
                        try
                        {
                            // Manual extraction as fallback
                            var startIndex = responseContent.IndexOf("\"access_token\":\"") + 16;
                            var endIndex = responseContent.IndexOf("\"", startIndex);
                            if (startIndex > 15 && endIndex > startIndex)
                            {
                                var accessToken = responseContent.Substring(startIndex, endIndex - startIndex);
                                if (!string.IsNullOrEmpty(accessToken))
                                {
                                    _logger.LogDebug("PayPal access token extracted manually from response");
                                    return accessToken;
                                }
                            }
                        }
                        catch (Exception extractEx)
                        {
                            _logger.LogError(extractEx, "Failed to manually extract access token from response");
                        }
                    }
                    
                    throw new InvalidOperationException($"Failed to parse PayPal token response: {jsonEx.Message}. Raw response: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal access token");
                throw;
            }
        }

        /// <summary>
        /// Process successful PayPal payment and update database
        /// </summary>
        private async Task ProcessSuccessfulPaymentAsync(PayPalCaptureResponse captureResponse)
        {
            try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found for payment processing");
                    return;
                }

            var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for payment processing: {UserId}", userId);
                    return;
                }

                // Get unpaid events for the user
            var attendingEvents = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Where(ea => ea.UserId == userId && ea.PaymentStatus != "paid")
                .ToListAsync();

                if (!attendingEvents.Any())
                {
                    _logger.LogWarning("No unpaid events found for user: {UserId}", userId);
                    return;
                }

                // Calculate total amount
                var totalAmount = attendingEvents.Sum(ea => 
                {
                    string priceStr = ea.Event.price.ToString().Replace("£", "").Trim();
                    if (priceStr.Equals("Free", StringComparison.OrdinalIgnoreCase))
                    {
                        return 0m;
                    }
                    return decimal.Parse(priceStr, System.Globalization.CultureInfo.InvariantCulture);
                });

                // Create payment transaction record
            var transaction = new PaymentTransaction
            {
                    PaymentIntentId = "", // Empty for PayPal transactions
                    PayPalOrderId = captureResponse.Id,
                UserId = userId,
                UserName = user.UserName,
                UserEmail = user.Email,
                    Amount = totalAmount,
                    Currency = "GBP",
                Status = "succeeded",
                CreatedAt = DateTime.UtcNow,
                EventIds = string.Join(",", attendingEvents.Select(ea => ea.EventId.ToString()))
            };

            _context.PaymentTransactions.Add(transaction);

                // Update event attendance status
            foreach (var attendance in attendingEvents)
            {
                attendance.PaymentStatus = "paid";
                attendance.PaymentDate = DateTime.UtcNow;
                    attendance.PaymentIntentId = captureResponse.Id;
            }

            await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed PayPal payment for user {UserId}. Transaction ID: {TransactionId}", 
                    userId, captureResponse.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal payment");
                throw;
            }
        }

        /// <summary>
        /// Server-side PayPal redirect for fallback when client-side SDK fails
        /// </summary>
        [HttpPost("redirect-payment")]
        [Authorize]
        public async Task<IActionResult> RedirectToPayPal([FromBody] PayPalRedirectRequest request)
        {
            try
            {
                _logger.LogInformation("Creating server-side PayPal redirect for user {UserId} with amount {Amount}", 
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, request.Amount);

                if (request.Amount <= 0)
                {
                    return BadRequest(new { error = "Invalid amount" });
                }

                var client = _httpClientFactory.CreateClient();
                var accessToken = await GetAccessTokenAsync(client);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Create PayPal order for redirect
                var orderRequest = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new {
                            amount = new {
                                currency_code = "GBP",
                                value = request.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                            },
                            description = "EventScape Event Payment",
                            custom_id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        }
                    },
                    application_context = new
                    {
                        brand_name = "EventScape",
                        landing_page = "LOGIN",
                        user_action = "PAY_NOW",
                        return_url = $"{Request.Scheme}://{Request.Host}/PaymentSuccess",
                        cancel_url = $"{Request.Scheme}://{Request.Host}/Payment"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(orderRequest);
                _logger.LogDebug("PayPal redirect order request: {Request}", jsonContent);

                var response = await client.PostAsync(
                    "https://api-m.paypal.com/v2/checkout/orders",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("PayPal redirect order response: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayPal redirect order failed: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    return StatusCode((int)response.StatusCode, new { error = responseContent });
                }

                var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent);
                
                // Find the approval URL from the links
                var approvalLink = orderResponse.Links.FirstOrDefault(l => l.Rel == "approve");
                if (approvalLink == null)
                {
                    return BadRequest(new { error = "PayPal approval URL not found" });
                }

                return Ok(new { redirectUrl = approvalLink.Href });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal redirect");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // PayPal API Models
    public class PayPalOrderRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GBP";
    }

    public class PayPalCaptureRequest
    {
        public string OrderId { get; set; } = string.Empty;
    }

    public class PayPalOrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("links")]
        public List<PayPalLink> Links { get; set; } = new();
    }

    public class PayPalCaptureResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PayPalAmount Amount { get; set; } = new();
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class PayPalAmount
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class PayPalLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("rel")]
        public string Rel { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
    }

    public class PayPalTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = string.Empty;

        [JsonPropertyName("nonce")]
        public string Nonce { get; set; } = string.Empty;
    }

    public class PayPalRedirectRequest
    {
        public decimal Amount { get; set; }
    }
} 