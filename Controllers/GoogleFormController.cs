using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;
using Regen.Services;

namespace Regen.Controllers;

public class GoogleFormController(
    GoogleFormService googleFormService,
    HttpClient httpClient,
    ILogger<GoogleFormController> logger
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetGoogleFormContent()
    {
        var url = "https://docs.google.com/forms/d/e/1FAIpQLSeX0ycXzgmcllp9LDPIDPYJw_YIkX7HxMvjIWBOcmpp--pAeg/viewform?usp=header";
        var html = await httpClient.GetStringAsync(url);
        var json = googleFormService.HtmlToJson(html);
        return Content(json, "application/json");
    }
}
