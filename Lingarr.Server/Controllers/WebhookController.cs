using Hangfire;
using Lingarr.Server.Attributes;
using Lingarr.Server.Jobs;
using Lingarr.Server.Models.Webhooks;
using Lingarr.Server.Services.Integration.Plex;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IBackgroundJobClient backgroundJobClient,
        ILogger<WebhookController> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Receives webhook events from Radarr
    /// </summary>
    /// <param name="payload">The webhook payload from Radarr</param>
    /// <returns>Returns 200 OK if webhook was queued, 400 if invalid payload</returns>
    [HttpPost("radarr")]
    public async Task<IActionResult> RadarrWebhook([FromBody] RadarrWebhookPayload payload)
    {
        if (payload.Movie == null || payload.Movie.Id <= 0)
        {
            _logger.LogWarning("Invalid Radarr webhook payload: missing or invalid movie data");
            return BadRequest(new { message = "Invalid webhook payload: missing movie data" });
        }

        _backgroundJobClient.Enqueue<WebhookJob>(job => job.ProcessRadarrWebhook(payload));
        _logger.LogInformation("Queued Radarr webhook processing job for movie ID {MovieId}", payload.Movie.Id);
        return Ok(new { message = "Webhook received and queued for processing" });
    }

    /// <summary>
    /// Receives webhook events from Sonarr
    /// </summary>
    /// <param name="payload">The webhook payload from Sonarr</param>
    /// <returns>Returns 200 OK if webhook was queued, 400 if invalid payload</returns>
    [HttpPost("sonarr")]
    public async Task<IActionResult> SonarrWebhook([FromBody] SonarrWebhookPayload payload)
    {
        if (payload.Series == null || payload.Series.Id <= 0 || payload.Episodes == null || !payload.Episodes.Any())
        {
            _logger.LogWarning("Invalid Sonarr webhook payload: missing series or episode data");
            return BadRequest(new { message = "Invalid webhook payload: missing series or episode data" });
        }

        _backgroundJobClient.Enqueue<WebhookJob>(job => job.ProcessSonarrWebhook(payload));
        _logger.LogInformation("Queued Sonarr webhook processing job for series ID {SeriesId}, episodes: {EpisodeIds}",
            payload.Series.Id, string.Join(", ", payload.Episodes.Select(e => e.Id)));
        return Ok(new { message = "Webhook received and queued for processing" });
    }

    /// <summary>
    /// Receives a Plex webhook. A newly added movie or episode is queued.
    /// Plex posts multipart form data with a JSON field named payload.
    /// </summary>
    [HttpPost("plex")]
    public async Task<IActionResult> PlexWebhook()
    {
        var json = await ReadPlexPayload();
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning("Plex webhook had no payload.");
            return BadRequest(new { message = "Plex webhook payload is missing." });
        }

        var decision = PlexWebhookReader.Read(json);
        if (decision is PlexWebhookDecision.Unreadable)
        {
            _logger.LogWarning("Plex webhook payload was not JSON.");
            return BadRequest(new { message = "Plex webhook payload was not JSON." });
        }

        if (decision is not PlexWebhookDecision.Added added)
        {
            return Ok(new { message = "Ignored" });
        }

        _backgroundJobClient.Enqueue<WebhookJob>(job => job.ProcessPlexWebhook(added.Item));
        _logger.LogInformation(
            "Queued Plex library.new for {Title} ({RatingKey})",
            added.Item.Title,
            added.Item.RatingKey);
        return Ok(new { message = "Webhook received and queued for processing" });
    }

    private async Task<string?> ReadPlexPayload()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            return form["payload"].FirstOrDefault();
        }

        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }
}
