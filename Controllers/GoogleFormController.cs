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
    [HttpPost]
    public async Task<IActionResult> GetGoogleFormContent([FromBody] JsonElement request)
    {
        string? url = request.TryGetProperty("url", out var value)
        ? value.GetString()
        : null;

        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("Incoming request send empty URL.");
            return BadRequest(new { error = "Target URL is required in the headers." });
        }

        try
        {
            var client = httpClientFactory.CreateClient("GoogleForm");
            var html = await client.GetStringAsync(url);
            var json = googleFormService.HtmlToJson(html);

            return Content(json, "application/json");
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"URL {url} cannot be processed.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetGoogleFormOriginalUrl([FromBody] JsonElement request)
    {
        string? url = request.TryGetProperty("url", out var value)
        ? value.GetString()
        : null;

        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("Incoming request send empty URL.");
            return BadRequest(new { error = "Target URL is required in the headers." });
        }

        try
        {
            var client = httpClientFactory.CreateClient("NoRedirect");

            using var getOriginalUrl = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(getOriginalUrl);

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
