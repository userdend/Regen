using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class ModalsController : Controller
{
  public IActionResult Examples() => View();
}
