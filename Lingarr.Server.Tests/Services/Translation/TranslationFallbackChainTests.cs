using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Translation;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

/// <summary>
/// Smoke/regression: ordered provider chain falls through on failure.
/// </summary>
public class TranslationFallbackChainTests
{
    private static Mock<ITranslationService> MockService(string name, Func<string, string>? translate = null, bool fail = false)
    {
        var mock = new Mock<ITranslationService>();
        mock.Setup(t => t.GetLanguagePair(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string s, string t, CancellationToken _) => new LanguagePair
            {
                Source = s,
                Target = t
            });
        if (fail)
        {
            mock.Setup(t => t.TranslateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TranslationException($"{name} failed"));
        }
        else
        {
            mock.Setup(t => t.TranslateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string text, string _, string _, List<string>? _, List<string>? _, CancellationToken _) =>
                    translate?.Invoke(text) ?? $"[{name}]{text}");
        }
        return mock;
    }

    [Fact]
    public async Task TranslateSubtitleLine_FallsBackToSecondProvider_WhenFirstFails()
    {
        var primary = MockService("openrouter", fail: true);
        var fallback = MockService("microsoft", t => $"MS:{t}");

        var entries = new List<TranslationServiceEntry>
        {
            new("openrouter", primary.Object, null, "openrouter/free"),
            new("microsoft", fallback.Object, null)
        };

        var svc = new SubtitleTranslationService(entries, NullLogger.Instance);
        var result = await svc.TranslateSubtitleLine(new TranslateAbleSubtitleLine
        {
            SubtitleLine = "Hello",
            SourceLanguage = "en",
            TargetLanguage = "bg"
        }, CancellationToken.None);

        Assert.Equal("MS:Hello", result.Translation);
        Assert.Equal("microsoft", result.Service);
    }

    [Fact]
    public async Task TranslateSubtitleLine_UsesPrimary_WhenItSucceeds()
    {
        var primary = MockService("deepseek", t => $"DS:{t}");
        var fallback = MockService("microsoft", t => $"MS:{t}");

        var entries = new List<TranslationServiceEntry>
        {
            new("deepseek", primary.Object, null, "deepseek-chat"),
            new("microsoft", fallback.Object, null)
        };

        var svc = new SubtitleTranslationService(entries, NullLogger.Instance);
        var result = await svc.TranslateSubtitleLine(new TranslateAbleSubtitleLine
        {
            SubtitleLine = "Hi",
            SourceLanguage = "en",
            TargetLanguage = "es"
        }, CancellationToken.None);

        Assert.Equal("DS:Hi", result.Translation);
        Assert.Equal("deepseek", result.Service);
        fallback.Verify(
            t => t.TranslateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<string>?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TranslateSubtitleLine_AllFail_Throws()
    {
        var a = MockService("a", fail: true);
        var b = MockService("b", fail: true);
        var entries = new List<TranslationServiceEntry>
        {
            new("a", a.Object, null),
            new("b", b.Object, null)
        };
        var svc = new SubtitleTranslationService(entries, NullLogger.Instance);
        await Assert.ThrowsAsync<TranslationException>(() =>
            svc.TranslateSubtitleLine(new TranslateAbleSubtitleLine
            {
                SubtitleLine = "x",
                SourceLanguage = "en",
                TargetLanguage = "fr"
            }, CancellationToken.None));
    }
}
