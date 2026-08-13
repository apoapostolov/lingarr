using System;
using System.Linq;
using Lingarr.Server.Services.Translation;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class TranslationTextChunkerTests
{
    [Fact]
    public void Split_ShouldReturnOriginalText_WhenWithinLimit()
    {
        var text = "Short subtitle line";

        var chunks = TranslationTextChunker.Split(text, 1000);

        Assert.Single(chunks);
        Assert.Equal(text, chunks[0]);
    }

    [Fact]
    public void Split_ShouldPreferWhitespaceBoundary_WhenTextExceedsLimit()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 260));

        var chunks = TranslationTextChunker.Split(text, 1000);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= 1000));
        Assert.Equal(text, string.Join(" ", chunks));
    }

    [Fact]
    public void Split_ShouldFallbackToHardCut_WhenNoWhitespaceExists()
    {
        var text = new string('a', 2100);

        var chunks = TranslationTextChunker.Split(text, 1000);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= 1000));
        Assert.Equal(text, string.Concat(chunks));
    }

    [Fact]
    public void Split_ShouldReturnEmpty_WhenTextIsWhitespace()
    {
        Assert.Empty(TranslationTextChunker.Split("   ", 1000));
    }

    [Fact]
    public void Split_ShouldThrow_WhenMaxLengthIsNotPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TranslationTextChunker.Split("text", 0));
    }
}
