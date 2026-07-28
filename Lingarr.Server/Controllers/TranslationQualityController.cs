using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.TranslationQuality;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/translation-request/{translationRequestId:int}/quality")]
public class TranslationQualityController : ControllerBase
{
    private readonly ITranslationQualityService _qualityService;

    public TranslationQualityController(ITranslationQualityService qualityService)
    {
        _qualityService = qualityService;
    }

    [HttpGet]
    public async Task<ActionResult<TranslationQualityDetail>> Get(
        int translationRequestId,
        [FromQuery] string? severity,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var result = await _qualityService.GetAsync(
            translationRequestId, severity, category, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("re-evaluate")]
    public async Task<ActionResult<TranslationQualitySummary>> ReEvaluate(
        int translationRequestId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _qualityService.EvaluateAsync(
                translationRequestId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("findings")]
    public async Task<ActionResult<IReadOnlyList<TranslationQualityFindingResponse>>> GetFindings(
        int translationRequestId,
        [FromQuery] string? severity,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var result = await _qualityService.GetAsync(
            translationRequestId, severity, category, cancellationToken);
        return result == null ? NotFound() : Ok(result.Findings);
    }
}
