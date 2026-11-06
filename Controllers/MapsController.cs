using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class MapsController : Controller
{
  public IActionResult Leaflet() => View();
}
