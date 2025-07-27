using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;
using Stripe;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Pages
{
    [Authorize]
    public class PaymentModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentModel> _logger;
        private readonly string _stripeSecretKey;

        public PaymentModel(
            EventAppDbContext context, 
            IConfiguration configuration,
            ILogger<PaymentModel> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            
            // Robust configuration validation with detailed logging
            StripePublicKey = _configuration["Stripe:PublicKey"];
            _stripeSecretKey = _configuration["Stripe:SecretKey"];
            PayPalClientId = _configuration["PayPal:ClientId"];
            
            // Log configuration status
            _logger.LogInformation("Payment Page Initialization:");
            _logger.LogInformation("Stripe Public Key: {Configured} (Length: {Length})", 
                !string.IsNullOrEmpty(StripePublicKey), StripePublicKey?.Length ?? 0);
            _logger.LogInformation("PayPal Client ID: {Configured} (Length: {Length})", 
                !string.IsNullOrEmpty(PayPalClientId), PayPalClientId?.Length ?? 0);
            
            // Set Stripe configuration if available
            if (!string.IsNullOrEmpty(_stripeSecretKey))
            {
                StripeConfiguration.ApiKey = _stripeSecretKey;
                _logger.LogInformation("Stripe configuration set successfully.");
            }
            else
            {
                _logger.LogWarning("Stripe secret key not configured.");
            }
        }

        public string StripePublicKey { get; }
        public string PayPalClientId { get; }
        public decimal TotalAmount { get; private set; }
        public string UserName { get; private set; } = string.Empty;
        public string UserSurname { get; private set; } = string.Empty;
        public string UserEmail { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserName = user.UserName;
            UserSurname = user.Surname;
            UserEmail = user.Email;

            // Calculate total amount from attending events
            var attendingEvents = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Where(ea => ea.UserId == userId)
                .ToListAsync();

            // Parse price string by removing currency symbol and converting to decimal
            TotalAmount = attendingEvents.Sum(ea => 
            {
                string priceStr = ea.Event.price.ToString().Replace("£", "").Trim();
                if (priceStr.Equals("Free", StringComparison.OrdinalIgnoreCase))
                {
                    return 0m;
                }
                return decimal.Parse(priceStr, CultureInfo.InvariantCulture);
            });

            if (TotalAmount <= 0)
            {
                return RedirectToPage("/MyEvents");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreatePaymentIntentAsync([FromBody] PaymentIntentRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Payment attempt by unauthenticated user");
                    return BadRequest("User not authenticated");
                }

                _logger.LogInformation("Creating payment intent for user {UserId} with amount {Amount}", 
                    userId, request.Amount);

                // Create a PaymentIntent with the order amount and currency
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Currency = "gbp",
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "userEmail", request.Email },
                        { "userName", request.Name }
                    },
                    Description = $"Payment for events - User: {request.Name}"
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                _logger.LogInformation("Payment intent created successfully. Intent ID: {IntentId}", 
                    intent.Id);

                return new JsonResult(new { clientSecret = intent.ClientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for user {UserId}", 
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return BadRequest(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSendTicketEmailAsync([FromBody] TicketEmailRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Ticket email attempt by unauthenticated user");
                    return BadRequest("User not authenticated");
                }

                _logger.LogInformation("Processing payment confirmation for intent {IntentId}", 
                    request.PaymentIntentId);

                // Get the payment intent to verify the payment was successful
                var service = new PaymentIntentService();
                var intent = await service.GetAsync(request.PaymentIntentId);

                if (intent.Status != "succeeded")
                {
                    _logger.LogWarning("Payment not successful for intent {IntentId}. Status: {Status}", 
                        request.PaymentIntentId, intent.Status);
                    return BadRequest("Payment not successful");
                }

                _logger.LogInformation("Payment successful for intent {IntentId}. Amount: {Amount}", 
                    request.PaymentIntentId, intent.Amount);

                // Get the user's attending events
                var attendingEvents = await _context.EventAttendances
                    .Include(ea => ea.Event)
                    .Where(ea => ea.UserId == userId)
                    .ToListAsync();

                _logger.LogInformation("Updating {Count} events for user {UserId}", 
                    attendingEvents.Count, userId);

                // Update payment status in the database
                foreach (var attendance in attendingEvents)
                {
                    attendance.PaymentStatus = "Paid";
                    attendance.PaymentDate = DateTime.UtcNow;
                    attendance.PaymentIntentId = request.PaymentIntentId;
                    
                    _logger.LogInformation("Updated payment status for event {EventId}", 
                        attendance.EventId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed payment and updated database for user {UserId}", 
                    userId);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment confirmation for intent {IntentId}", 
                    request.PaymentIntentId);
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class PaymentIntentRequest
    {
        public decimal Amount { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }

    public class TicketEmailRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
} 