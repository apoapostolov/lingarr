using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.ProviderHealth;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class DashboardActivityServiceTests
{
    [Fact]
    public async Task GetAsync_SummarizesRecentWorkAndFallbackRecovery()
    {
        await using var database = CreateDatabase();
        var now = DateTime.UtcNow;
        var request = new TranslationRequest
        {
            Title = "Dashboard test",
            SourceLanguage = "en",
            TargetLanguage = "uk",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Completed,
            CompletedAt = now.AddMinutes(-10),
            QualityScore = 92,
            QualityGrade = "Good",
            QualityStatus = "completed"
        };
        database.TranslationRequests.Add(request);
        await database.SaveChangesAsync();
        database.TranslationRequestLines.AddRange(
            new TranslationRequestLine
            {
                TranslationRequestId = request.Id,
                Position = 1,
                Source = "Hello",
                Target = "Привіт",
                Service = "microsoft"
            },
            new TranslationRequestLine
            {
                TranslationRequestId = request.Id,
                Position = 2,
                Source = "Goodbye",
                Target = "До побачення",
                Service = "microsoft"
            });
        database.ProviderOperationalEvents.AddRange(
            new ProviderOperationalEvent
            {
                Provider = "openrouter",
                Operation = "translate",
                Outcome = "failure",
                TranslationRequestId = request.Id,
                OccurredAt = now.AddMinutes(-12)
            },
            new ProviderOperationalEvent
            {
                Provider = "microsoft",
                Operation = "translate",
                Outcome = "success",
                TranslationRequestId = request.Id,
                OccurredAt = now.AddMinutes(-11)
            });
        await database.SaveChangesAsync();

        var result = await CreateService(database).GetAsync(48);

        Assert.Equal(1, result.CompletedFiles);
        Assert.Equal(2, result.TranslatedLines);
        Assert.Equal(1, result.QualityPassed);
        Assert.Equal(1, result.FallbackRecoveries);
        Assert.Equal("Microsoft", Assert.Single(result.TopProviders).Name);
        Assert.Contains(result.Narrative, sentence => sentence.Contains("last 48 hours"));
        Assert.Contains(result.Narrative, sentence =>
            sentence == "Microsoft completed 1 subtitle file.");
        Assert.DoesNotContain(result.Narrative, sentence => sentence.Contains("dialogue lines"));
        Assert.DoesNotContain(result.Narrative, sentence => sentence.Contains("translated lines"));
        Assert.DoesNotContain(result.Narrative, sentence => sentence.Contains("busiest language pair"));
    }

    [Fact]
    public async Task GetAsync_NoActivity_ReturnsUnderstandableEmptyNarrative()
    {
        await using var database = CreateDatabase();

        var result = await CreateService(database).GetAsync(24);

        Assert.Equal(0, result.CompletedFiles);
        Assert.Contains(result.Narrative, sentence =>
            sentence == "No subtitle files completed in the last 24 hours.");
    }

    [Fact]
    public async Task GetAsync_MeteredLlmUsage_AddsTokensAndEstimatedCost()
    {
        await using var database = CreateDatabase();
        var now = DateTime.UtcNow;
        var request = new TranslationRequest
        {
            Title = "Metered dashboard test",
            SourceLanguage = "en",
            TargetLanguage = "bg",
            MediaType = MediaType.Movie,
            Status = TranslationStatus.Completed,
            CompletedAt = now.AddMinutes(-5)
        };
        database.TranslationRequests.Add(request);
        await database.SaveChangesAsync();
        database.ProviderOperationalEvents.Add(new ProviderOperationalEvent
        {
            Provider = "openai",
            Model = "gpt-4o-mini",
            Operation = "batch",
            Outcome = "success",
            TranslationRequestId = request.Id,
            InputTokens = 40_558,
            OutputTokens = 56_484,
            EstimatedCostUsd = 4.48m,
            OccurredAt = now.AddMinutes(-6)
        });
        await database.SaveChangesAsync();

        var result = await CreateService(database).GetAsync(48);

        Assert.Contains(result.Narrative, sentence =>
            sentence.Contains("40,558 input tokens") &&
            sentence.Contains("56,484 output tokens") &&
            sentence.Contains("$4.48"));
    }

    [Fact]
    public async Task GetAsync_Cache_ServesRepeatedCallsFromCacheWithoutRecompute()
    {
        await using var database = CreateDatabase();
        var service = CreateService(database);

        var first = await service.GetAsync(48);
        var firstGenerated = first.GeneratedAt;

        // A second call inside the fresh window must return the identical cached
        // payload (same GeneratedAt) without recomputing — proving the DB-heavy
        // path is skipped on cache hits.
        var second = await service.GetAsync(48);

        Assert.Equal(firstGenerated, second.GeneratedAt);
        Assert.Equal(first.CompletedFiles, second.CompletedFiles);
    }

    private static DashboardActivityService CreateService(LingarrDbContext database)
    {
        var settings = new Mock<ISettingService>();
        settings.Setup(item => item.GetSetting(SettingKeys.Dashboard.ActivityWindowHours))
            .ReturnsAsync("48");
        var health = new Mock<IProviderHealthService>();
        health.Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProviderHealthResponse>());

        // Isolated per-test cache so no state leaks between tests. A real scope
        // factory backed by an empty service provider keeps the background-refresh
        // path resolvable without standing up the full DI graph.
        var cache = new MemoryCache(Options.Create(
            new MemoryCacheOptions()));
        var scopeFactory = new ServiceCollection().BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        var logger = new Mock<ILogger<DashboardActivityService>>();
        return new DashboardActivityService(
            database,
            settings.Object,
            health.Object,
            cache,
            scopeFactory,
            logger.Object);
    }

    private static LingarrDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LingarrDbContext(options);
    }
}
