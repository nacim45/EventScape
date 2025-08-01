# EventScape - Event Management System

A comprehensive event management web application built with ASP.NET Core, featuring secure payment processing through Stripe and PayPal.

## 🔧 **Azure Environment Variables Solution - Efficient Summary**

### **The Problem:**
Azure App Service uses environment variable names like `StripePublic`, but ASP.NET Core expects configuration keys like `Stripe:PublicKey`. Without mapping, the app couldn't find the payment keys.

### **The Solution in `Program.cs`:**

```csharp
// Map Azure environment variables to configuration
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    // Map Azure environment variable names to configuration keys
    { "Stripe:PublicKey", Environment.GetEnvironmentVariable("StripePublic") ?? builder.Configuration["Stripe:PublicKey"] },
    { "Stripe:SecretKey", Environment.GetEnvironmentVariable("StripeSecret") ?? builder.Configuration["Stripe:SecretKey"] },
    { "Stripe:WebhookSecret", Environment.GetEnvironmentVariable("StripeWebhook") ?? builder.Configuration["Stripe:WebhookSecret"] },
    { "PayPal:ClientId", Environment.GetEnvironmentVariable("PaypalClientID") ?? builder.Configuration["PayPal:ClientId"] },
    { "PayPal:Secret", Environment.GetEnvironmentVariable("PaypalSecret") ?? builder.Configuration["PayPal:Secret"] }
});
```

### **What This Does:**

1. **Direct Mapping**: `StripePublic` (Azure) → `Stripe:PublicKey` (App)
2. **Fallback**: Uses local config if environment variable doesn't exist
3. **No Configuration Changes**: Works with existing PaymentController and PaymentModel code
4. **Simple & Safe**: Uses built-in ASP.NET Core configuration system

### **Azure Setup:**
In Azure App Service → Configuration → Application Settings:
- `StripePublic` = your_stripe_public_key
- `StripeSecret` = your_stripe_secret_key  
- `StripeWebhook` = your_webhook_secret
- `PaypalClientID` = your_paypal_client_id
- `PaypalSecret` = your_paypal_secret

### **Result:**
✅ Environment variables automatically available as `_configuration["Stripe:PublicKey"]` throughout the app  
✅ No code changes needed in controllers/models  
✅ Works in both local development and Azure production

**One simple mapping block solved the entire Azure environment variable integration!** 🎯

## 🛠️ Local Development Setup

For local testing, create `appsettings.Development.json`:

```json
{
  "Stripe": {
    "PublicKey": "your_stripe_public_key_here",
    "SecretKey": "your_stripe_secret_key_here",
    "WebhookSecret": "your_stripe_webhook_secret_here"
  },
  "PayPal": {
    "ClientId": "your_paypal_client_id_here",
    "Secret": "your_paypal_secret_here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "soft20181_starter.Controllers.PaymentController": "Debug",
      "soft20181_starter.Controllers.PayPalController": "Debug"
    }
  }
}
```

## 🔍 Testing Payment Configuration

Use these endpoints to verify your payment setup:

- **Configuration Test**: `/api/payment/test-config`
- **Health Check**: `/api/payment/health`

## 📋 Stripe Webhook Setup

1. Go to [Stripe Dashboard > Webhooks](https://dashboard.stripe.com/webhooks)
2. Add endpoint: `https://your-domain.azurewebsites.net/api/payment/stripe-webhook`
3. Select events:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed` 
   - `payment_intent.canceled`
4. Copy the webhook secret to your environment variables

## 🚀 Deployment

1. Set environment variables in Azure App Service
2. Deploy using Azure DevOps pipelines or GitHub Actions
3. Verify payment configuration via test endpoints

## 🔒 Security Notes

- Never commit real API keys to version control
- Use Azure Key Vault for production secrets
- Environment variables are automatically mapped in `Program.cs`

## 🎯 Features

- User authentication and authorization
- Event creation and management
- Secure payment processing (Stripe & PayPal)
- Responsive design
- Admin interface
- Real-time event updates

## 📱 Technologies

- ASP.NET Core 8.0
- Entity Framework Core
- Stripe.NET
- SQLite (development) / SQL Server (production)
- Bootstrap CSS
- JavaScript

## 🐛 Troubleshooting

**"Stripe payment not available"**: Check environment variables configuration
**"PayPal is loading"**: Verify PayPal credentials and sandbox/live mode settings

## 📄 License

All rights reserved © 2024 EventScape 