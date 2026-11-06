using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class LogisticsController : Controller
{
  public IActionResult Dashboard() => View();
  public IActionResult Fleet() => View();
}
