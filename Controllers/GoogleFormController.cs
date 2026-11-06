using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;
using Regen.Services;

namespace Regen.Controllers;

public class GoogleFormController(
    GoogleFormService googleFormService,
    IHttpClientFactory httpClientFactory,
    ILogger<GoogleFormController> logger
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGoogleFormContent([FromQuery] string? form)
    {
        if (string.IsNullOrEmpty(form))
        {
            logger.LogWarning("Incoming request send empty URL.");
            return BadRequest(new { error = "Target URL is required in the headers." });
        }

        try
        {
            var client = httpClientFactory.CreateClient("GoogleForm");
            var html = await client.GetStringAsync(form);
            var json = googleFormService.HtmlToJson(html);

            return Content(json, "application/json");
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"URL {form} cannot be processed.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleFormOriginalUrl([FromQuery] string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("Incoming request send empty URL.");
            return BadRequest(new { error = "Target URL is required in the headers." });
        }

        try
        {
            var client = httpClientFactory.CreateClient("NoRedirect");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request);

            var status = (int)response.StatusCode;
            if (!(status >= 300 && status < 400))
            {
                throw new HttpRequestException($"Request failed with status code {status}");
            }

            string? originalUrl = null;
            if (response.Headers.NonValidated.TryGetValues("Location", out var values))
            {
                originalUrl = values.ToString();
            }

            return Content(originalUrl ?? string.Empty, "text/html; charset=utf-8");
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Error retrieving the original URL for {url}.");
        }
    }
}
