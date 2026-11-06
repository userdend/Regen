using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class WizardsController : Controller
{
  public IActionResult Checkout() => View();
  public IActionResult CreateDeal() => View();
  public IActionResult PropertyListing() => View();
}
