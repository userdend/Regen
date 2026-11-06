using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;

namespace Regen.Controllers;

public class AcademyController : Controller
{
  public IActionResult Dashboard() => View();
  public IActionResult MyCourse() => View();
  public IActionResult CourseDetails() => View();
}
