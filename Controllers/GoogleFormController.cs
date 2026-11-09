using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Regen.Models;
using Regen.Models.Requests;
using Regen.Services;
using Regen.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Regen.Controllers;

public class GoogleFormController(
    RegenContext db,
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

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateGoogleSubmission(
        [FromBody] CreateScheduleRequest request,
        CancellationToken ct)
    {
        if (request.IntervalMilliseconds < 1000 ||
            request.IntervalMilliseconds > 600000)
        {
            return BadRequest(
                "Interval must be between 1 second and 10 minutes."
            );
        }

        if (request.Entries.Count == 0 ||
            request.Entries.Count > 1000)
        {
            return BadRequest(
                "Entries must contain between 1 and 1000 URLs."
            );
        }

        // Prototype restriction: only accept Google Forms URLs.
        foreach (var url in request.Entries)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !uri.Host.Equals(
                    "docs.google.com",
                    StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.Contains("/forms/"))
            {
                return BadRequest("Invalid Google Forms URL.");
            }
        }

        var userIdClaim = User.FindFirstValue("RegenUserId");

        if (!Guid.TryParse(userIdClaim, out var userId))
        { return Unauthorized(); }

        var now = DateTime.UtcNow;
        var batch = new ScheduledBatch
        {
            UserId = userId,
            IntervalMilliseconds = request.IntervalMilliseconds,
            CreatedAt = now
        };

        for (int i = 0; i < request.Entries.Count; i++)
        {
            batch.Entries.Add(new ScheduledEntry
            {
                Sequence = i + 1,
                Url = request.Entries[i],

                ScheduledAt = now.AddMilliseconds(
                    (long)i * request.IntervalMilliseconds
                )
            });
        }

        db.ScheduledBatches.Add(batch);

        await db.SaveChangesAsync(ct);

        return Accepted(new
        {
            batch.Id,
            Count = batch.Entries.Count,
            batch.IntervalMilliseconds,
            FirstScheduledAt = now
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetGoogleSubmissionStatus(
        Guid id,
        CancellationToken ct)
    {
        var batch = await db.ScheduledBatches
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Id,
                b.IntervalMilliseconds,
                b.CreatedAt,
                Entries = b.Entries
                    .OrderBy(e => e.Sequence)
                    .Select(e => new
                    {
                        e.Id,
                        e.Sequence,
                        e.Status,
                        e.ScheduledAt,
                        e.ProcessedAt,
                        e.LastError
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return batch is null ? NotFound() : Ok(batch);
    }
}