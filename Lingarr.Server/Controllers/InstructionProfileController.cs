using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.PromptProfiles;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/instruction-profile")]
public class InstructionProfileController : ControllerBase
{
    private readonly ITranslationPromptProfileService _profiles;

    public InstructionProfileController(ITranslationPromptProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromptProfileResponse>>> List(
        [FromQuery] string? type,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken) =>
        Ok(await _profiles.GetAllAsync(type, includeArchived, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromptProfileResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetAsync(id, cancellationToken);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<PromptProfileResponse>> Create(
        CreatePromptProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profiles.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id:int}/draft")]
    public async Task<ActionResult<PromptProfileResponse>> SaveDraft(
        int id,
        SavePromptProfileDraftRequest request,
        CancellationToken cancellationToken) =>
        await Execute(() => _profiles.SaveDraftAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<PromptProfileResponse>> Publish(
        int id,
        PublishPromptProfileRequest request,
        CancellationToken cancellationToken) =>
        await Execute(() => _profiles.PublishAsync(id, request.ChangeNote, cancellationToken));

    [HttpPost("{id:int}/restore/{versionId:int}")]
    public async Task<ActionResult<PromptProfileResponse>> Restore(
        int id,
        int versionId,
        CancellationToken cancellationToken) =>
        await Execute(() => _profiles.RestoreAsync(id, versionId, cancellationToken));

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult> Activate(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _profiles.ActivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<PromptProfileDeleteResponse>> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _profiles.DeleteAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static async Task<ActionResult<PromptProfileResponse>> Execute(
        Func<Task<PromptProfileResponse>> action)
    {
        try
        {
            return new OkObjectResult(await action());
        }
        catch (KeyNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (ArgumentException exception)
        {
            return new BadRequestObjectResult(exception.Message);
        }
    }
}
