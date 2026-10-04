using System;
using System.Collections.Generic;
using Lingarr.Server.Services;
using Xunit;

namespace Lingarr.Server.Tests.Jobs;

public class LibraryCoverageTests
{
    private static readonly HashSet<string> Sources = new(StringComparer.OrdinalIgnoreCase) { "en" };
    private static readonly HashSet<string> Targets = new(StringComparer.OrdinalIgnoreCase) { "bg" };

    [Fact]
    public void HasFullPair_AcceptsEnglishAndBulgarian()
    {
        Assert.True(LibraryCoverage.HasFullPair("bg,en", null, Sources, Targets));
        Assert.True(LibraryCoverage.HasFullPair(null, ["en", "bg"], Sources, Targets));
    }

    [Fact]
    public void HasFullPair_RejectsSourceWithoutTheTarget()
    {
        Assert.False(LibraryCoverage.HasFullPair("en", null, Sources, Targets));
    }

    [Fact]
    public void CheckedWithoutSource_SkipsAFolderThatHadNoSourceSubtitle()
    {
        Assert.True(LibraryCoverage.CheckedWithoutSource("", Sources));
        Assert.False(LibraryCoverage.CheckedWithoutSource(null, Sources));
        Assert.False(LibraryCoverage.CheckedWithoutSource("en", Sources));
    }
}
