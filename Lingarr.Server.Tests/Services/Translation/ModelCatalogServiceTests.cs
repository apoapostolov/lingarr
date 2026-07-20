using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Translation;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class ModelCatalogServiceTests
{
    private sealed class StubTranslationService : ITranslationService
    {
        private readonly ModelsResponse _response;
        public int GetModelsCalls { get; private set; }

        public StubTranslationService(ModelsResponse response) => _response = response;

        public string? ModelName => null;

        public Task<string> TranslateAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            List<string>? contextLinesBefore,
            List<string>? contextLinesAfter,
            CancellationToken cancellationToken) =>
            Task.FromResult(text);

        public Task<List<SourceLanguage>> GetLanguages() =>
            Task.FromResult(new List<SourceLanguage>());

        public Task<ModelsResponse> GetModels()
        {
            GetModelsCalls++;
            return Task.FromResult(_response);
        }

        public Task<LanguagePair?> GetLanguagePair(
            string requestedSource,
            string requestedTarget,
            CancellationToken cancellationToken) =>
            Task.FromResult<LanguagePair?>(null);
    }

    [Fact]
    public async Task GetModelsAsync_CachesSuccessfulResult()
    {
        var stub = new StubTranslationService(new ModelsResponse
        {
            Options = [new LabelValue { Label = "m1", Value = "m1" }]
        });
        var factory = new Mock<ITranslationServiceFactory>();
        factory.Setup(f => f.CreateTranslationService("deepseek")).Returns(stub);
        var catalog = new ModelCatalogService(factory.Object, NullLogger<ModelCatalogService>.Instance);

        var first = await catalog.GetModelsAsync("deepseek");
        var second = await catalog.GetModelsAsync("deepseek");

        Assert.Single(first.Options);
        Assert.Single(second.Options);
        Assert.Equal(1, stub.GetModelsCalls);
    }

    [Fact]
    public async Task GetModelsAsync_RefreshBypassesCache()
    {
        var stub = new StubTranslationService(new ModelsResponse
        {
            Options = [new LabelValue { Label = "m1", Value = "m1" }]
        });
        var factory = new Mock<ITranslationServiceFactory>();
        factory.Setup(f => f.CreateTranslationService("openrouter")).Returns(stub);
        var catalog = new ModelCatalogService(factory.Object, NullLogger<ModelCatalogService>.Instance);

        await catalog.GetModelsAsync("openrouter");
        await catalog.GetModelsAsync("openrouter", forceRefresh: true);

        Assert.Equal(2, stub.GetModelsCalls);
    }

    [Fact]
    public async Task GetModelsAsync_OnFailure_ReturnsStaleCache()
    {
        var good = new StubTranslationService(new ModelsResponse
        {
            Options = [new LabelValue { Label = "cached", Value = "cached" }]
        });
        var factory = new Mock<ITranslationServiceFactory>();
        factory.SetupSequence(f => f.CreateTranslationService("zai"))
            .Returns(good)
            .Throws(new InvalidOperationException("boom"));

        var catalog = new ModelCatalogService(factory.Object, NullLogger<ModelCatalogService>.Instance);
        await catalog.GetModelsAsync("zai");
        var stale = await catalog.GetModelsAsync("zai", forceRefresh: true);

        Assert.Single(stale.Options);
        Assert.Equal("cached", stale.Options[0].Value);
        Assert.Contains("cached", stale.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
