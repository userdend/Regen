using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Regen.Data;
using Regen.Services;

[Authorize]
public class BillingController(
    RegenContext _db,
    PaymentService _paymentService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession(
        int packageId)
    {
        var userId = Guid.Parse(
            User.FindFirstValue("RegenUserId")!
        );

        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return Unauthorized();

        var successUrl =
            $"{Request.Scheme}://{Request.Host}/";

        var cancelUrl =
            $"{Request.Scheme}://{Request.Host}/";

        var checkoutUrl =
            await _paymentService.CreateCheckoutSessionAsync(
                userId,
                packageId,
                user.Email,
                successUrl,
                cancelUrl
            );

        return Redirect(checkoutUrl);
    }

    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        return RedirectToAction("Index", "Home");
    }
}