using System.Collections.Generic;
using Lingarr.Server.Services.Subtitle;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

public class EmbeddedSubtitleExtractorTests
{
    [Fact]
    public void SelectEnglishTextTrack_PrefersDialogueOverSigns()
    {
        var streams = new List<EmbeddedSubtitleStream>
        {
            new(3, "ass", "eng", "Signs"),
            new(4, "ass", "eng", "Dialogue")
        };

        var plan = EmbeddedSubtitleExtractor.SelectEnglishTextTrack(streams, "BLUE LOCK - S01E14");

        Assert.NotNull(plan);
        Assert.Equal(4, plan!.StreamIndex);
        Assert.Equal("BLUE LOCK - S01E14.en.srt", plan.DestinationFileName);
    }

    [Fact]
    public void SelectEnglishTextTrack_PrefersNonSdhWhenAvailable()
    {
        var streams = new List<EmbeddedSubtitleStream>
        {
            new(2, "subrip", "eng", "SDH"),
            new(3, "subrip", "eng", "English")
        };

        var plan = EmbeddedSubtitleExtractor.SelectEnglishTextTrack(streams, "Show - S01E01");

        Assert.Equal(3, plan!.StreamIndex);
    }

    [Fact]
    public void SelectEnglishTextTrack_IgnoresImageOnly()
    {
        var streams = new List<EmbeddedSubtitleStream>
        {
            new(2, "hdmv_pgs_subtitle", "eng", null)
        };

        Assert.Null(EmbeddedSubtitleExtractor.SelectEnglishTextTrack(streams, "Avatar"));
        Assert.True(EmbeddedSubtitleExtractor.HasOnlyImageSubtitles(streams));
    }

    [Fact]
    public void ParseProbeJson_ReadsStreams()
    {
        const string json = """
            {"streams":[{"index":2,"codec_name":"subrip","tags":{"language":"eng","title":"English"}}]}
            """;

        var streams = EmbeddedSubtitleExtractor.ParseProbeJson(json);

        Assert.Single(streams);
        Assert.Equal(2, streams[0].Index);
        Assert.Equal("eng", streams[0].Language);
    }
}
