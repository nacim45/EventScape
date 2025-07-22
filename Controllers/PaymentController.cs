using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using soft20181_starter.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;

namespace soft20181_starter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly EventAppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;
        private readonly string _stripeSecretKey;

        public PaymentController(
            EventAppDbContext context, 
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stripeSecretKey = _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe secret key is not configured");
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized payment attempt - No user ID found");
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Unauthorized payment attempt - User not found: {UserId}", userId);
                    return Unauthorized();
                }

                // Calculate total amount from attending events
                var attendingEvents = await _context.EventAttendances
                    .Include(ea => ea.Event)
                    .Where(ea => ea.UserId == userId && ea.PaymentStatus != "paid")
                    .ToListAsync();

                if (!attendingEvents.Any())
                {
                    _logger.LogWarning("No unpaid events found for user: {UserId}", userId);
                    return BadRequest("No events to pay for");
                }

                var totalAmount = attendingEvents.Sum(ea =>
                {
                    // Ensure the price is treated as a string and handle currency symbols
                    string priceStr = ea.Event.price.ToString().Replace("£", "").Replace("┬ú", "").Trim();
                    if (priceStr.Equals("Free", StringComparison.OrdinalIgnoreCase))
                    {
                        return 0m;
                    }
                    if (decimal.TryParse(priceStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal parsedPrice))
                    {
                        return parsedPrice;
                    }
                    _logger.LogWarning("Could not parse price string '{PriceStr}' for event {EventId}", priceStr, ea.EventId);
                    return 0m; // Default to 0 if parsing fails
                });
                if (totalAmount <= 0)
                {
                    _logger.LogWarning("Invalid payment amount for user: {UserId}. Calculated total: {TotalAmount}", userId, totalAmount);
                    return BadRequest("Invalid payment amount");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100), // Convert to cents
                    Currency = "gbp",
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "userName", user.UserName },
                        { "userEmail", user.Email },
                        { "eventIds", string.Join(",", attendingEvents.Select(ea => ea.EventId.ToString())) }
                    },
                    ReceiptEmail = user.Email,
                    Description = $"Payment for {attendingEvents.Count} event(s)",
                    SetupFutureUsage = "off_session" // Enable future payments
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    PaymentIntentId = intent.Id,
                    UserId = userId,
                    UserName = user.UserName,
                    UserEmail = user.Email,
                    Amount = totalAmount,
                    Currency = "GBP",
                    Status = intent.Status,
                    CreatedAt = DateTime.UtcNow,
                    EventIds = string.Join(",", attendingEvents.Select(ea => ea.EventId.ToString()))
                };

                _context.PaymentTransactions.Add(transaction);
                _logger.LogInformation("Attempting to save payment transaction for user {UserId}", userId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment intent created for user {UserId} with amount {Amount}", userId, totalAmount);

                return Ok(new
                {
                    clientSecret = intent.ClientSecret,
                    requiresAction = intent.Status == "requires_action",
                    paymentIntentId = intent.Id
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error while creating payment intent for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { error = "Payment processing error. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signatureHeader,
                    _configuration["Stripe:WebhookSecret"]
                );

                _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandleSuccessfulPayment(stripeEvent.Data.Object as PaymentIntent);
                        break;
                    case "payment_intent.payment_failed":
                        await HandleFailedPayment(stripeEvent.Data.Object as PaymentIntent);
                        break;
                    case "charge.refunded":
                        await HandleRefund(stripeEvent.Data.Object as Charge);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500);
            }
        }

        [HttpPost("refund")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> RefundPayment([FromBody] RefundRequest request)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PaymentIntentId == request.PaymentIntentId);

                if (transaction == null)
                {
                    return NotFound("Transaction not found");
                }

                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = request.PaymentIntentId,
                    Reason = request.Reason
                };

                var refund = await refundService.CreateAsync(refundOptions);

                transaction.Status = "refunded";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment refunded for transaction {PaymentIntentId}", request.PaymentIntentId);

                return Ok(new { refundId = refund.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentIntentId}", request.PaymentIntentId);
                return StatusCode(500, new { error = "Error processing refund" });
            }
        }

        private async Task HandleSuccessfulPayment(PaymentIntent paymentIntent)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PaymentIntentId == paymentIntent.Id);

                if (transaction != null)
                {
                    transaction.Status = "succeeded";
                    transaction.UpdatedAt = DateTime.UtcNow;

                    // Update event attendance status
                    var eventIds = transaction.EventIds.Split(',');
                    var attendances = await _context.EventAttendances
                        .Where(ea => eventIds.Contains(ea.EventId.ToString()) && ea.UserId == transaction.UserId)
                        .ToListAsync();

                    foreach (var attendance in attendances)
                    {
                        attendance.PaymentStatus = "paid";
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Payment succeeded for transaction {PaymentIntentId}", paymentIntent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling successful payment for {PaymentIntentId}", paymentIntent.Id);
            }
        }

        private async Task HandleFailedPayment(PaymentIntent paymentIntent)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PaymentIntentId == paymentIntent.Id);

                if (transaction != null)
                {
                    transaction.Status = "failed";
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogWarning("Payment failed for transaction {PaymentIntentId}", paymentIntent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling failed payment for {PaymentIntentId}", paymentIntent.Id);
            }
        }

        private async Task HandleRefund(Charge charge)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PaymentIntentId == charge.PaymentIntentId);

                if (transaction != null)
                {
                    transaction.Status = "refunded";
                    transaction.UpdatedAt = DateTime.UtcNow;

                    // Update event attendance status
                    var eventIds = transaction.EventIds.Split(',');
                    var attendances = await _context.EventAttendances
                        .Where(ea => eventIds.Contains(ea.EventId.ToString()) && ea.UserId == transaction.UserId)
                        .ToListAsync();

                    foreach (var attendance in attendances)
                    {
                        attendance.PaymentStatus = "refunded";
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Payment refunded for transaction {PaymentIntentId}", charge.PaymentIntentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling refund for {PaymentIntentId}", charge.PaymentIntentId);
            }
        }
    }

    public class PaymentIntentRequest
    {
        public required decimal Amount { get; set; }
    }

    public class RefundRequest
    {
        public required string PaymentIntentId { get; set; }
        public required string Reason { get; set; }
    }
} 