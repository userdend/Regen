using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Regen.Services;

namespace Regen.Controllers;

public class PagesController(AccountDeletionService accountDeletionService) : Controller
{
  public IActionResult AccountSettings() => View();
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> DeleteAccount(
        CancellationToken ct)
  {
    var userIdClaim = User.FindFirstValue("RegenUserId");

    if (!Guid.TryParse(userIdClaim, out var userId))
    {
      return Unauthorized();
    }

    await accountDeletionService.DeleteAsync(userId, ct);

    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

    return RedirectToAction("Login", "Auth");
  }
  // public IActionResult AccountSettingsBilling() => View();
  // public IActionResult AccountSettingsConnections() => View();
  // public IActionResult AccountSettingsNotifications() => View();
  // public IActionResult AccountSettingsSecurity() => View();
  public IActionResult FAQ() => View();
  // public IActionResult MiscComingSoon() => View();
  // public IActionResult MiscError() => View();
  // public IActionResult MiscNotAuthorized() => View();
  // public IActionResult MiscUnderMaintenance() => View();
  // public IActionResult MiscServerError() => View();
  // public IActionResult Pricing() => View();
  // public IActionResult ProfileConnections() => View();
  // public IActionResult ProfileUser() => View();
  // public IActionResult ProfileTeams() => View();
  // public IActionResult ProfileProjects() => View();
}
