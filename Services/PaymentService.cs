using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Regen.Data;
using Regen.Models;

namespace Regen.Services;

public class PaymentService
{
    private readonly RegenContext _db;
    private readonly IConfiguration _config;

    public PaymentService(
        RegenContext db,
        IConfiguration config)
    {
        _db = db;
        _config = config;

        StripeConfiguration.ApiKey =
            _config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        int packageId,
        string email,
        string successUrl,
        string cancelUrl)
    {
        // Get active package
        var package = await _db.CreditPackages
            .FirstOrDefaultAsync(x =>
                x.Id == packageId && x.IsActive);

        if (package == null)
        {
            throw new InvalidOperationException("Credit package not found.");
        }

        // Create pending purchase
        var purchase = new CreditPurchase
        {
            UserId = userId,
            CreditPackageId = package.Id,
            Credits = package.Credits,
            Amount = (long)(package.Amount * 100),
            Currency = package.Currency.ToLower(),
            Status = "Pending",

            // Temporary ID until Stripe creates the session
            StripeSessionId = $"pending_{Guid.NewGuid():N}"
        };

        _db.CreditPurchases.Add(purchase);
        await _db.SaveChangesAsync();

        try
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",

                CustomerEmail = email,

                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = package.StripePriceId,
                        Quantity = 1
                    }
                },

                ClientReferenceId = purchase.Id.ToString(),

                Metadata = new Dictionary<string, string>
                {
                    { "purchase_id", purchase.Id.ToString() },
                    { "user_id", userId.ToString() },
                    { "package_id", package.Id.ToString() }
                },

                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();

            var session = await service.CreateAsync(options);

            purchase.StripeSessionId = session.Id;

            await _db.SaveChangesAsync();

            return session.Url;
        }
        catch (Exception ex)
        {
            purchase.Status = "Failed";
            await _db.SaveChangesAsync();

            throw new InvalidOperationException(
                "Failed to create Stripe Checkout Session.",
                ex
            );
        }
    }
}