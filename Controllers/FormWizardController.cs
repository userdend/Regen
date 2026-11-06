using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class FormWizardController : Controller
{
public IActionResult Icons() => View();
public IActionResult Numbered() => View();
}
