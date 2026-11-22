using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;
using Regen.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Regen.Data;
using Microsoft.EntityFrameworkCore;

namespace Regen.Controllers;

public class HomeController(RegenContext db) : Controller
{
    [Authorize]
    public async Task<IActionResult> Generator()
    {
        var userId = Guid.Parse(
        User.FindFirstValue("RegenUserId")!
    );

        var configurations = await db.SavedConfigurations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new SavedConfigurationViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Url = c.Url,
                IntervalMilliseconds = c.IntervalMilliseconds,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return View(configurations);
    }

    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> Pricing()
    {
        var packages = await db.CreditPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.Amount)
            .ToListAsync();

        return View(packages);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult PrivacyPolicy()
    {
        return View();
    }

    public IActionResult TermsOfService()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
