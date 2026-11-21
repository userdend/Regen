using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Regen.Data;

namespace Regen.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public class StripeWebhookController(
    RegenContext _db,
    IConfiguration _config,
    ILogger<StripeWebhookController> _logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(
            HttpContext.Request.Body
        ).ReadToEndAsync();

        var signature = Request.Headers["Stripe-Signature"];

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _config["Stripe:WebhookSecret"],
                throwOnApiVersionMismatch: false
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe webhook validation failed."
            );

            return BadRequest("Invalid webhook.");
        }

        _logger.LogInformation(
            "Received Stripe event: {Type}, Event ID: {EventId}",
            stripeEvent.Type,
            stripeEvent.Id
        );

        var supportedEvents = new[]
        {
            "checkout.session.completed",
            "checkout.session.async_payment_succeeded",
            "checkout.session.async_payment_failed",
            "checkout.session.expired"
        };

        if (!supportedEvents.Contains(stripeEvent.Type))
            return Ok();

        var session = stripeEvent.Data.Object as Session;

        if (session == null)
            return BadRequest("Invalid Checkout Session.");

        if (session.Metadata == null ||
            !session.Metadata.TryGetValue(
                "purchase_id", out var purchaseIdString))
        {
            return BadRequest("Missing purchase_id.");
        }

        if (!Guid.TryParse(purchaseIdString, out var purchaseId))
            return BadRequest("Invalid purchase_id.");

        try
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            // 1. Claim Stripe event atomically
            var inserted = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""StripeWebhookEvents""
                    (""Id"", ""Type"", ""ProcessedAt"")
                VALUES
                    ({stripeEvent.Id}, {stripeEvent.Type}, {DateTime.UtcNow})
                ON CONFLICT (""Id"") DO NOTHING
            ");

            // Duplicate event: already processed
            if (inserted == 0)
            {
                _logger.LogInformation(
                    "Duplicate Stripe event ignored: {EventId}",
                    stripeEvent.Id
                );

                await transaction.CommitAsync();
                return Ok();
            }

            // 2. Find purchase
            var purchase = await _db.CreditPurchases
                .FirstOrDefaultAsync(x => x.Id == purchaseId);

            if (purchase == null)
                throw new InvalidOperationException(
                    $"Purchase not found: {purchaseId}"
                );

            // 3. Validate Stripe session
            if (purchase.StripeSessionId != session.Id)
                throw new InvalidOperationException(
                    "Stripe session mismatch."
                );

            // 4. Handle successful payments
            if (stripeEvent.Type == "checkout.session.completed" ||
                stripeEvent.Type == "checkout.session.async_payment_succeeded")
            {
                // Wait for confirmed payment
                if (session.PaymentStatus != "paid")
                {
                    await transaction.CommitAsync();
                    return Ok();
                }

                // Validate amount and currency
                if (purchase.Amount != session.AmountTotal ||
                    !string.Equals(
                        purchase.Currency,
                        session.Currency,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Payment amount or currency mismatch."
                    );
                }

                // Only process pending purchases
                var updated = await _db.CreditPurchases
                    .Where(x =>
                        x.Id == purchaseId &&
                        x.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, "Paid")
                        .SetProperty(
                            x => x.CompletedAt,
                            DateTime.UtcNow
                        )
                        .SetProperty(
                            x => x.StripePaymentIntentId,
                            session.PaymentIntentId
                        )
                    );

                if (updated > 0)
                {
                    // Add credits exactly once
                    var userUpdated = await _db.Users
                        .Where(x => x.Id == purchase.UserId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(
                                x => x.Credit,
                                x => x.Credit + purchase.Credits
                            )
                        );

                    if (userUpdated == 0)
                        throw new InvalidOperationException(
                            "Purchase user not found."
                        );

                    _logger.LogInformation(
                        "Purchase {PurchaseId} completed. Added {Credits} credits.",
                        purchaseId,
                        purchase.Credits
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Purchase {PurchaseId} already processed. No credits added.",
                        purchaseId
                    );
                }
            }

            // 5. Handle failed payments
            else if (stripeEvent.Type ==
                     "checkout.session.async_payment_failed")
            {
                await _db.CreditPurchases
                    .Where(x =>
                        x.Id == purchaseId &&
                        x.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, "Failed")
                    );

                _logger.LogInformation(
                    "Purchase {PurchaseId} marked as Failed.",
                    purchaseId
                );
            }

            // 6. Handle expired sessions
            else if (stripeEvent.Type ==
                     "checkout.session.expired")
            {
                await _db.CreditPurchases
                    .Where(x =>
                        x.Id == purchaseId &&
                        x.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, "Expired")
                    );

                _logger.LogInformation(
                    "Purchase {PurchaseId} marked as Expired.",
                    purchaseId
                );
            }

            // 7. Commit event + purchase + credits together
            await transaction.CommitAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process Stripe event {EventId}.",
                stripeEvent.Id
            );

            // Transaction is rolled back when disposed.
            // Stripe can retry this event.
            return StatusCode(500);
        }
    }
}