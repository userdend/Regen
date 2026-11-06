using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class AccessController : Controller
{
public IActionResult Permission() => View();
public IActionResult Roles() => View();
}
