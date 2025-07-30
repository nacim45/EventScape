# EventScape - Event Management System

A comprehensive event management web application built with ASP.NET Core, featuring secure payment processing through Stripe and PayPal.

## 🔧 Azure Environment Variables Configuration

Configure these environment variables in your Azure App Service:

```
StripePublic=your_stripe_public_key_here
StripeSecret=your_stripe_secret_key_here
StripeWebhook=your_stripe_webhook_secret_here
PaypalClientID=your_paypal_client_id_here
PaypalSecret=your_paypal_secret_here
```

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