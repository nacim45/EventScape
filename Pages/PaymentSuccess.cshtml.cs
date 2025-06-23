using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;

namespace soft20181_starter.Pages
{
    public class PaymentSuccessModel : PageModel
    {
        private readonly EventAppDbContext _context;

        public PaymentSuccessModel(EventAppDbContext context)
        {
            _context = context;
        }

        public string PaymentIntentId { get; private set; }
        public string PaymentMethod { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime PaymentDate { get; private set; }

        public async Task<IActionResult> OnGetAsync(string paymentIntentId, string orderId)
        {
            // Handle both Stripe payment intent IDs and PayPal order IDs
            var transactionId = paymentIntentId ?? orderId;
            
            if (string.IsNullOrEmpty(transactionId))
            {
                return RedirectToPage("/MyEvents");
            }

            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == transactionId || t.PayPalOrderId == transactionId);

            if (transaction == null)
            {
                return RedirectToPage("/MyEvents");
            }

            PaymentIntentId = transaction.PaymentIntentId ?? transaction.PayPalOrderId;
            PaymentMethod = !string.IsNullOrEmpty(transaction.PayPalOrderId) ? "PayPal" : "Stripe";
            Amount = transaction.Amount;
            PaymentDate = transaction.CreatedAt;

            return Page();
        }
    }
} 