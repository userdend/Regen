using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class FormValidationController : Controller
{
public IActionResult Validation() => View();
}
