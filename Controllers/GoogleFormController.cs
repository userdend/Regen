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
using System.ClientModel;

namespace Regen.Controllers;

[Authorize]
public class GoogleFormController(
    OpenAIService openAIService,
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

        // Validate Google Forms URLs
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

        // Get authenticated user
        var userIdClaim = User.FindFirstValue("RegenUserId");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var creditCost = request.Entries.Count;

        // Start database transaction
        await using var transaction =
            await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Atomic credit deduction
            var affectedRows = await db.Users
                .Where(u => u.Id == userId && u.Credit >= creditCost)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        u => u.Credit,
                        u => u.Credit - creditCost
                    ),
                    ct
                );

            // Deduction failed
            if (affectedRows == 0)
            {
                var userExists = await db.Users
                    .AnyAsync(u => u.Id == userId, ct);

                if (!userExists)
                    return Unauthorized();

                return BadRequest("Insufficient credits.");
            }

            // Create scheduled batch
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

            // Save configuration if requested
            if (request.SaveConfiguration)
            {
                var savedConfig = new SavedConfiguration
                {
                    UserId = userId,
                    Name = $"Configuration {now:yyyy-MM-dd HH:mm:ss}",
                    Url = request.DataMap.Url,
                    Config = request.DataMap.Config,
                    IntervalMilliseconds = request.IntervalMilliseconds
                };

                db.SavedConfigurations.Add(savedConfig);
            }

            // Persist batch and configuration
            await db.SaveChangesAsync(ct);

            // Commit credit deduction + batch + configuration
            await transaction.CommitAsync(ct);

            return Accepted(new
            {
                batch.Id,
                Count = batch.Entries.Count,
                batch.IntervalMilliseconds,
                FirstScheduledAt = now
            });
        }
        catch
        {
            // Rollback all database changes
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetSavedConfiguration(Guid configId)
    {
        var userId = Guid.Parse(
            User.FindFirst("RegenUserId")!.Value
        );

        var savedConfiguration = await db.SavedConfigurations
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Where(b => b.Id == configId)
            .Select(b => new
            {
                b.Url,
                b.Config,
                b.IntervalMilliseconds,
            })
            .FirstOrDefaultAsync();

        return savedConfiguration is null ? NotFound() : Ok(savedConfiguration);
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSyntheticResponse(
       [FromBody] GenerateSyntheticRequest request,
       CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid ||
            request.Questions == null ||
            request.Questions.Count == 0)
        {
            return BadRequest(new
            {
                error = "Invalid request data."
            });
        }

        try
        {
            var respondents =
                await openAIService.GenerateSyntheticResponseAsync(
                    request,
                    cancellationToken
                );

            return Ok(new { respondents });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return new StatusCodeResult(499);
        }
        catch (ClientResultException ex)
        when (ex.Status == 429 &&
              ex.Message.Contains("insufficient_quota"))
        {
            logger.LogError(ex, "OpenAI API quota exhausted.");

            return StatusCode(503, new
            {
                error = "AI generation is temporarily unavailable. Please try again later.",
                code = "AI_SERVICE_UNAVAILABLE"
            });
        }
        catch (ClientResultException ex)
            when (ex.Status == 429)
        {
            logger.LogWarning(ex, "OpenAI rate limit exceeded.");

            return StatusCode(429, new
            {
                error = "The AI service is currently busy. Please try again shortly.",
                code = "AI_RATE_LIMITED"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate synthetic responses.");

            return StatusCode(500, new
            {
                error = "Something went wrong while generating responses. Please try again.",
                code = "AI_GENERATION_FAILED"
            });
        }
    }
}