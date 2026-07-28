using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.PromptProfiles;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class TranslationPromptProfileServiceTests
{
    [Fact]
    public async Task PublishAsync_CreatesImmutableVersions()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        var created = await service.CreateAsync(new CreatePromptProfileRequest(
            PromptProfileTypes.System,
            "Drama",
            "Natural dramatic dialogue",
            "First {sourceLanguage} {targetLanguage}"));

        var first = await service.PublishAsync(created.Id, "First release");
        await service.SaveDraftAsync(created.Id, new SavePromptProfileDraftRequest(
            "Drama",
            "Natural dramatic dialogue",
            "Second {sourceLanguage} {targetLanguage}"));
        var second = await service.PublishAsync(created.Id, "Second release");

        Assert.Equal(2, second.CurrentVersionNumber);
        Assert.Equal(2, second.Versions.Count);
        Assert.Equal(
            "First {sourceLanguage} {targetLanguage}",
            second.Versions.Single(version => version.VersionNumber == 1).Content);
        Assert.NotEqual(first.CurrentPublishedVersionId, second.CurrentPublishedVersionId);
    }

    [Fact]
    public async Task ResolveChainAsync_AppliesOnlyToAiAndRecordsExactVersions()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        await service.GetAllAsync();
        var request = new TranslationRequest
        {
            Title = "Prompt usage test",
            SourceLanguage = "en",
            TargetLanguage = "uk",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.InProgress
        };
        database.TranslationRequests.Add(request);
        await database.SaveChangesAsync();
        var ai = new TranslationChainEntry
        {
            Id = "ai-row",
            Provider = "openrouter",
            Model = "openrouter/free"
        };
        var traditional = new TranslationChainEntry
        {
            Id = "nmt-row",
            Provider = "microsoft"
        };

        await service.ResolveChainAsync([ai, traditional], request.Id);

        Assert.NotNull(ai.ResolvedSystemPrompt);
        Assert.NotNull(ai.ResolvedSystemVersionId);
        Assert.Null(traditional.ResolvedSystemPrompt);
        var usage = Assert.Single(await database.TranslationPromptUsages.ToListAsync());
        Assert.Equal("ai-row", usage.ChainRowId);
        Assert.Equal(ai.ResolvedSystemVersionId, usage.SystemVersionId);
    }

    [Fact]
    public async Task DeleteAsync_RejectsTheActiveDefault()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);
        var profiles = await service.GetAllAsync(PromptProfileTypes.System);
        var active = Assert.Single(profiles);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync(active.Id));

        Assert.Contains("different default", exception.Message);
        Assert.NotNull(await database.TranslationPromptProfiles.FindAsync(active.Id));
    }

    private static TranslationPromptProfileService CreateService(LingarrDbContext database)
    {
        var values = new Dictionary<string, string>
        {
            [SettingKeys.Translation.AiPrompt] =
                "Translate {sourceLanguage} to {targetLanguage}.",
            [SettingKeys.Translation.AiContextPrompt] =
                "[TARGET]{lineToTranslate}[/TARGET]",
            [SettingKeys.Translation.ActiveSystemPromptProfileId] = string.Empty,
            [SettingKeys.Translation.ActiveContextPromptProfileId] = string.Empty,
            [SettingKeys.Translation.ServiceType] = "microsoft"
        };
        var settings = new Mock<ISettingService>();
        settings.Setup(item => item.GetSetting(It.IsAny<string>()))
            .ReturnsAsync((string key) => values.GetValueOrDefault(key));
        settings.Setup(item => item.UpsertSetting(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string key, string value) => values[key] = value)
            .Returns(Task.CompletedTask);
        settings.Setup(item => item.SetSetting(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string key, string value) => values[key] = value)
            .ReturnsAsync(true);
        return new TranslationPromptProfileService(database, settings.Object);
    }

    private static LingarrDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LingarrDbContext(options);
    }
}
