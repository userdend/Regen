using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Regen.Data;
using Regen.Services;

namespace Regen.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public class StripeWebhookController(
    RegenContext _db,
    IConfiguration _config,
    ILogger<StripeWebhookController> _logger,
    NotificationService _notificationService) : ControllerBase
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
            _logger.LogWarning(
                ex,
                "Stripe webhook validation failed; returning BadRequest."
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
        {
            _logger.LogInformation(
                "Ignoring unsupported Stripe event {EventType} with ID {EventId}.",
                stripeEvent.Type,
                stripeEvent.Id
            );
            return Ok();
        }

        var session = stripeEvent.Data.Object as Session;

        if (session == null)
        {
            _logger.LogWarning(
                "Stripe event {EventId} of type {EventType} did not contain a Checkout Session.",
                stripeEvent.Id,
                stripeEvent.Type
            );
            return BadRequest("Invalid Checkout Session.");
        }

        if (session.Metadata == null ||
            !session.Metadata.TryGetValue(
                "purchase_id", out var purchaseIdString))
        {
            _logger.LogWarning(
                "Stripe event {EventId} is missing the purchase_id metadata.",
                stripeEvent.Id
            );
            return BadRequest("Missing purchase_id.");
        }

        if (!Guid.TryParse(purchaseIdString, out var purchaseId))
        {
            _logger.LogWarning(
                "Stripe event {EventId} contained an invalid purchase_id.",
                stripeEvent.Id
            );
            return BadRequest("Invalid purchase_id.");
        }

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
                    _logger.LogInformation(
                        "Stripe event {EventId} for purchase {PurchaseId} is awaiting payment confirmation; status is {PaymentStatus}.",
                        stripeEvent.Id,
                        purchaseId,
                        session.PaymentStatus
                    );

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

                    await _notificationService.CreateAsync(
                        purchase.UserId,
                        "Payment successful",
                        $"{purchase.Credits} credits have been added to your account.",
                        "success",
                        "/Home/Index"
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
                        "Purchase {PurchaseId} was not pending when Stripe event {EventId} was processed; no credits added.",
                        purchaseId,
                        stripeEvent.Id
                    );
                }
            }

            // 5. Handle failed payments
            else if (stripeEvent.Type ==
                     "checkout.session.async_payment_failed")
            {
                var updated = await _db.CreditPurchases
                    .Where(x =>
                        x.Id == purchaseId &&
                        x.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, "Failed")
                    );

                _logger.LogInformation(
                    updated > 0
                        ? "Purchase {PurchaseId} marked as Failed from Stripe event {EventId}."
                        : "Purchase {PurchaseId} was already finalized; no Failed status update was applied.",
                    purchaseId,
                    stripeEvent.Id
                );
            }

            // 6. Handle expired sessions
            else if (stripeEvent.Type ==
                     "checkout.session.expired")
            {
                var updated = await _db.CreditPurchases
                    .Where(x =>
                        x.Id == purchaseId &&
                        x.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, "Expired")
                    );

                _logger.LogInformation(
                    updated > 0
                        ? "Purchase {PurchaseId} marked as Expired from Stripe event {EventId}."
                        : "Purchase {PurchaseId} was already finalized; no Expired status update was applied.",
                    purchaseId,
                    stripeEvent.Id
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
                "Failed to process Stripe event {EventId} of type {EventType} for purchase {PurchaseId}.",
                stripeEvent.Id,
                stripeEvent.Type,
                purchaseId
            );

            // Transaction is rolled back when disposed.
            // Stripe can retry this event.
            return StatusCode(500);
        }
    }
}
