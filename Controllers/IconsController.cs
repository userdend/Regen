using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class IconsController : Controller
{
  public IActionResult RiIcons() => View();
}
