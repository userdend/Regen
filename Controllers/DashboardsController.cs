using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class DashboardsController : Controller
{
  public IActionResult Index() => View();
  public IActionResult CRM() => View();
}
