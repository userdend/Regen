using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class ChartsController : Controller
{
  public IActionResult Apex() => View();
  public IActionResult Chartjs() => View();
}
