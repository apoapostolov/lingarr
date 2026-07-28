using System.Security.Cryptography;
using System.Text;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.PromptProfiles;
using Lingarr.Server.Services.Translation;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class TranslationPromptProfileService : ITranslationPromptProfileService
{
    private const int MaxContentLength = 40_000;
    private readonly LingarrDbContext _dbContext;
    private readonly ISettingService _settings;

    public TranslationPromptProfileService(
        LingarrDbContext dbContext,
        ISettingService settings)
    {
        _dbContext = dbContext;
        _settings = settings;
    }

    public async Task<IReadOnlyList<PromptProfileResponse>> GetAllAsync(
        string? type = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var profiles = await ProfileQuery()
            .Where(profile =>
                (type == null || profile.Type == type) &&
                (includeArchived || !profile.IsArchived))
            .OrderBy(profile => profile.Type)
            .ThenBy(profile => profile.Name)
            .ToListAsync(cancellationToken);
        var assignments = await CurrentAssignmentsAsync(cancellationToken);
        return profiles.Select(profile => ToResponse(
            profile,
            assignments.GetValueOrDefault(profile.Id))).ToList();
    }

    public async Task<PromptProfileResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var profile = await ProfileQuery()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (profile == null)
            return null;
        var assignments = await CurrentAssignmentsAsync(cancellationToken);
        return ToResponse(profile, assignments.GetValueOrDefault(profile.Id));
    }

    public async Task<PromptProfileResponse> CreateAsync(
        CreatePromptProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = ValidateType(request.Type);
        var name = await ValidateNameAsync(type, request.Name, null, cancellationToken);
        var content = ValidateContent(request.Content ?? string.Empty);
        var profile = new TranslationPromptProfile
        {
            Type = type,
            Name = name,
            Description = request.Description?.Trim() ?? string.Empty,
            DraftContent = content
        };
        _dbContext.TranslationPromptProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(profile.Id, cancellationToken))!;
    }

    public async Task<PromptProfileResponse> SaveDraftAsync(
        int id,
        SavePromptProfileDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.TranslationPromptProfiles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        profile.Name = await ValidateNameAsync(
            profile.Type, request.Name, id, cancellationToken);
        profile.Description = request.Description?.Trim() ?? string.Empty;
        profile.DraftContent = ValidateContent(request.Content);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<PromptProfileResponse> PublishAsync(
        int id,
        string? changeNote,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.TranslationPromptProfiles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        var content = ValidateContent(profile.DraftContent);
        var nextVersion = await _dbContext.TranslationPromptProfileVersions
            .Where(version => version.ProfileId == id)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var version = new TranslationPromptProfileVersion
        {
            ProfileId = id,
            VersionNumber = nextVersion + 1,
            Content = content,
            ChangeNote = changeNote?.Trim() ?? string.Empty,
            ContentHash = Hash(content)
        };
        _dbContext.TranslationPromptProfileVersions.Add(version);
        await _dbContext.SaveChangesAsync(cancellationToken);
        profile.CurrentPublishedVersionId = version.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<PromptProfileResponse> RestoreAsync(
        int id,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.TranslationPromptProfiles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        var version = await _dbContext.TranslationPromptProfileVersions
            .FirstOrDefaultAsync(item =>
                item.Id == versionId && item.ProfileId == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        profile.DraftContent = version.Content;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<PromptProfileDeleteResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.TranslationPromptProfiles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        var usedHistorically = await _dbContext.TranslationPromptUsages.AnyAsync(
            usage => usage.SystemProfileId == id || usage.ContextProfileId == id,
            cancellationToken);
        if (usedHistorically)
        {
            profile.IsArchived = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new PromptProfileDeleteResponse(true);
        }
        _dbContext.TranslationPromptProfiles.Remove(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new PromptProfileDeleteResponse(false);
    }

    public async Task ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var profile = await ProfileQuery()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        var version = profile.CurrentPublishedVersionId.HasValue
            ? profile.Versions.FirstOrDefault(item =>
                item.Id == profile.CurrentPublishedVersionId.Value)
            : null;
        if (version == null)
            throw new InvalidOperationException("Publish this profile before making it the default.");

        var activeKey = profile.Type == PromptProfileTypes.System
            ? SettingKeys.Translation.ActiveSystemPromptProfileId
            : SettingKeys.Translation.ActiveContextPromptProfileId;
        var legacyKey = profile.Type == PromptProfileTypes.System
            ? SettingKeys.Translation.AiPrompt
            : SettingKeys.Translation.AiContextPrompt;
        await _settings.UpsertSetting(activeKey, profile.Id.ToString());
        await _settings.UpsertSetting(legacyKey, version.Content);
    }

    public async Task ResolveChainAsync(
        IReadOnlyList<TranslationChainEntry> entries,
        int? translationRequestId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var activeSystemId = ParseId(await _settings.GetSetting(
            SettingKeys.Translation.ActiveSystemPromptProfileId));
        var activeContextId = ParseId(await _settings.GetSetting(
            SettingKeys.Translation.ActiveContextPromptProfileId));
        var requestedIds = entries
            .Where(entry => TranslationChain.SupportsModel(entry.Provider))
            .SelectMany(entry => new int?[]
            {
                entry.SystemPromptProfileId ?? activeSystemId,
                entry.ContextPromptProfileId ?? activeContextId
            })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var profiles = await ProfileQuery()
            .Where(profile => requestedIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);

        foreach (var entry in entries.Where(item =>
                     TranslationChain.SupportsModel(item.Provider)))
        {
            var system = Resolve(
                profiles, entry.SystemPromptProfileId ?? activeSystemId,
                PromptProfileTypes.System);
            var context = Resolve(
                profiles, entry.ContextPromptProfileId ?? activeContextId,
                PromptProfileTypes.Context);
            entry.ResolvedSystemPrompt = system?.version.Content;
            entry.ResolvedContextPrompt = context?.version.Content;
            entry.ResolvedSystemVersionId = system?.version.Id;
            entry.ResolvedContextVersionId = context?.version.Id;
            entry.ResolvedSystemContentHash = system?.version.ContentHash;
            entry.ResolvedContextContentHash = context?.version.ContentHash;

            if (translationRequestId.HasValue)
            {
                var exists = await _dbContext.TranslationPromptUsages.AnyAsync(
                    usage =>
                        usage.TranslationRequestId == translationRequestId.Value &&
                        usage.ChainRowId == entry.Id,
                    cancellationToken);
                if (!exists)
                {
                    _dbContext.TranslationPromptUsages.Add(new TranslationPromptUsage
                    {
                        TranslationRequestId = translationRequestId.Value,
                        ChainRowId = entry.Id,
                        Provider = entry.ProviderNormalized,
                        Model = entry.Model,
                        SystemProfileId = system?.profile.Id,
                        SystemVersionId = system?.version.Id,
                        SystemContentHash = system?.version.ContentHash,
                        ContextProfileId = context?.profile.Id,
                        ContextVersionId = context?.version.Id,
                        ContextContentHash = context?.version.ContentHash
                    });
                }
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        var profiles = await ProfileQuery().ToListAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            foreach (var version in profile.Versions.Where(item =>
                         item.ContentHash == "legacy-import"))
                version.ContentHash = Hash(version.Content);
        }

        foreach (var type in PromptProfileTypes.All)
        {
            var profile = profiles.FirstOrDefault(item =>
                item.Type == type && !item.IsArchived);
            if (profile == null)
            {
                var legacyKey = type == PromptProfileTypes.System
                    ? SettingKeys.Translation.AiPrompt
                    : SettingKeys.Translation.AiContextPrompt;
                var content = await _settings.GetSetting(legacyKey) ?? string.Empty;
                profile = new TranslationPromptProfile
                {
                    Type = type,
                    Name = type == PromptProfileTypes.System
                        ? "Default translation instructions"
                        : "Default surrounding context",
                    Description = "Imported from the original prompt setting.",
                    DraftContent = content
                };
                _dbContext.TranslationPromptProfiles.Add(profile);
                await _dbContext.SaveChangesAsync(cancellationToken);
                var version = new TranslationPromptProfileVersion
                {
                    ProfileId = profile.Id,
                    VersionNumber = 1,
                    Content = content,
                    ChangeNote = "Imported from the original prompt setting",
                    ContentHash = Hash(content)
                };
                _dbContext.TranslationPromptProfileVersions.Add(version);
                await _dbContext.SaveChangesAsync(cancellationToken);
                profile.CurrentPublishedVersionId = version.Id;
                profiles.Add(profile);
            }

            var activeKey = type == PromptProfileTypes.System
                ? SettingKeys.Translation.ActiveSystemPromptProfileId
                : SettingKeys.Translation.ActiveContextPromptProfileId;
            var active = await _settings.GetSetting(activeKey);
            if (!ParseId(active).HasValue)
                await _settings.UpsertSetting(activeKey, profile.Id.ToString());
        }

        var rawChain = await _settings.GetSetting(SettingKeys.Translation.ServiceType);
        if (!string.IsNullOrWhiteSpace(rawChain) &&
            rawChain.TrimStart().StartsWith('[') &&
            !rawChain.Contains("\"id\"", StringComparison.OrdinalIgnoreCase))
        {
            await _settings.SetSetting(
                SettingKeys.Translation.ServiceType,
                TranslationChain.Serialize(TranslationChain.Parse(rawChain)));
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TranslationPromptProfile> ProfileQuery() =>
        _dbContext.TranslationPromptProfiles
            .AsSplitQuery()
            .Include(profile => profile.Versions);

    private async Task<Dictionary<int, int>> CurrentAssignmentsAsync(
        CancellationToken cancellationToken)
    {
        var raw = await _settings.GetSetting(SettingKeys.Translation.ServiceType);
        return TranslationChain.Parse(raw)
            .SelectMany(entry => new[]
            {
                entry.SystemPromptProfileId,
                entry.ContextPromptProfileId
            })
            .Where(id => id.HasValue)
            .GroupBy(id => id!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static (TranslationPromptProfile profile, TranslationPromptProfileVersion version)? Resolve(
        IReadOnlyDictionary<int, TranslationPromptProfile> profiles,
        int? id,
        string expectedType)
    {
        if (!id.HasValue)
            return null;
        if (!profiles.TryGetValue(id.Value, out var profile) ||
            profile.Type != expectedType ||
            !profile.CurrentPublishedVersionId.HasValue)
            throw new InvalidOperationException(
                $"The assigned {expectedType} prompt profile is unavailable or has no published version.");
        var version = profile.Versions.FirstOrDefault(item =>
            item.Id == profile.CurrentPublishedVersionId.Value);
        if (version == null)
            throw new InvalidOperationException(
                $"The assigned {expectedType} prompt profile has no published version.");
        return (profile, version);
    }

    private static PromptProfileResponse ToResponse(
        TranslationPromptProfile profile,
        int assignments)
    {
        var versions = profile.Versions
            .OrderByDescending(item => item.VersionNumber)
            .Select(item => new PromptProfileVersionResponse(
                item.Id,
                item.VersionNumber,
                item.Content,
                item.ChangeNote,
                item.ContentHash,
                item.CreatedAt))
            .ToList();
        var current = versions.FirstOrDefault(item =>
            item.Id == profile.CurrentPublishedVersionId);
        return new PromptProfileResponse(
            profile.Id,
            profile.Type,
            profile.Name,
            profile.Description,
            profile.DraftContent,
            profile.CurrentPublishedVersionId,
            current?.VersionNumber,
            current == null || current.ContentHash != Hash(profile.DraftContent),
            profile.IsArchived,
            assignments,
            profile.UpdatedAt,
            versions);
    }

    private async Task<string> ValidateNameAsync(
        string type,
        string name,
        int? exceptId,
        CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 120)
            throw new ArgumentException("Profile name is required and must be 120 characters or fewer.");
        var exists = await _dbContext.TranslationPromptProfiles.AnyAsync(
            profile =>
                profile.Type == type &&
                profile.Name == normalized &&
                profile.Id != exceptId &&
                !profile.IsArchived,
            cancellationToken);
        if (exists)
            throw new ArgumentException("A profile with this name already exists.");
        return normalized;
    }

    private static string ValidateType(string type)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (!PromptProfileTypes.All.Contains(normalized))
            throw new ArgumentException("Profile type must be system or context.");
        return normalized;
    }

    private static string ValidateContent(string content)
    {
        if (content.Length > MaxContentLength)
            throw new ArgumentException($"Prompt content must be {MaxContentLength:N0} characters or fewer.");
        return content;
    }

    private static int? ParseId(string? value) =>
        int.TryParse(value, out var id) && id > 0 ? id : null;

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
