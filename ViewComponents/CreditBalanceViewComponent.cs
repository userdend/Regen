
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Regen.Data;

namespace Regen.ViewComponents;

public class CreditBalanceViewComponent(
    RegenContext _db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.FindFirstValue("RegenUserId");

        if (!Guid.TryParse(userId, out var id))
            return View(0);

        var balance = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => u.Credit)
            .FirstOrDefaultAsync();

        return View(balance);
    }
}
