using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Regen.Controllers;

public class AuthController : Controller
{
  public IActionResult Login()
  {
    if (User.Identity?.IsAuthenticated == true)
    {
      return RedirectToAction("Generator", "Home");
    }

    return View();
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public IActionResult GoogleLogin()
  {
    var properties = new AuthenticationProperties
    {
      RedirectUri = Url.Action("Generator", "Home")
    };

    return Challenge(
        properties,
        GoogleDefaults.AuthenticationScheme
    );
  }

  [Authorize]
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    return RedirectToAction("Login", "Auth");
  }

  public IActionResult ForgotPasswordBasic() => View();
  public IActionResult ForgotPasswordCover() => View();
  public IActionResult LoginBasic() => View();
  public IActionResult LoginCover() => View();
  public IActionResult RegisterBasic() => View();
  public IActionResult RegisterCover() => View();
  public IActionResult RegisterMultiSteps() => View();
  public IActionResult ResetPasswordBasic() => View();
  public IActionResult ResetPasswordCover() => View();
  public IActionResult TwoStepsBasic() => View();
  public IActionResult TwoStepsCover() => View();
  public IActionResult VerifyEmailBasic() => View();
  public IActionResult VerifyEmailCover() => View();
}
