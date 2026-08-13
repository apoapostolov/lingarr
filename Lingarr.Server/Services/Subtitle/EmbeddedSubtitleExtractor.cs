using System.Diagnostics;
using System.Text.Json;

namespace Lingarr.Server.Services.Subtitle;

public sealed record EmbeddedSubtitleStream(
    int Index,
    string Codec,
    string? Language,
    string? Title);

public sealed record EmbeddedExtractPlan(
    int StreamIndex,
    string DestinationFileName,
    bool IsImageOnly);

public static class EmbeddedSubtitleExtractor
{
    private static readonly HashSet<string> TextCodecs = new(StringComparer.OrdinalIgnoreCase)
    {
        "subrip", "srt", "ass", "ssa", "mov_text", "webvtt", "text"
    };

    private static readonly HashSet<string> ImageCodecs = new(StringComparer.OrdinalIgnoreCase)
    {
        "hdmv_pgs_subtitle", "pgssub", "dvd_subtitle", "dvdsub", "dvb_subtitle"
    };

    public static IReadOnlyList<EmbeddedSubtitleStream> ParseProbeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("streams", out var streams))
        {
            return [];
        }

        var result = new List<EmbeddedSubtitleStream>();
        foreach (var stream in streams.EnumerateArray())
        {
            var index = stream.TryGetProperty("index", out var indexElement)
                ? indexElement.GetInt32()
                : -1;
            var codec = stream.TryGetProperty("codec_name", out var codecElement)
                ? codecElement.GetString() ?? ""
                : "";
            string? language = null;
            string? title = null;
            if (stream.TryGetProperty("tags", out var tags))
            {
                if (tags.TryGetProperty("language", out var languageElement))
                {
                    language = languageElement.GetString();
                }

                if (tags.TryGetProperty("title", out var titleElement))
                {
                    title = titleElement.GetString();
                }
            }

            result.Add(new EmbeddedSubtitleStream(index, codec, language, title));
        }

        return result;
    }

    public static EmbeddedExtractPlan? SelectEnglishTextTrack(
        IReadOnlyList<EmbeddedSubtitleStream> streams,
        string mediaFileName)
    {
        var candidates = streams
            .Where(stream => IsEnglish(stream) && TextCodecs.Contains(stream.Codec))
            .Where(stream => !IsSignsOnly(stream))
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var preferred = candidates.FirstOrDefault(stream => !IsSdh(stream)) ?? candidates[0];
        return new EmbeddedExtractPlan(
            preferred.Index,
            SubtitleNaming.BuildDestinationFileName(mediaFileName, "en", SubtitleCaption.None, ".srt"),
            false);
    }

    public static bool HasOnlyImageSubtitles(IReadOnlyList<EmbeddedSubtitleStream> streams) =>
        streams.Count > 0
        && streams.All(stream => ImageCodecs.Contains(stream.Codec));

    public static ProcessStartInfo BuildProbeCommand(string videoPath) =>
        new()
        {
            FileName = "ffprobe",
            ArgumentList =
            {
                "-v", "error",
                "-select_streams", "s",
                "-show_entries", "stream=index,codec_name:stream_tags=language,title",
                "-of", "json",
                videoPath
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

    public static ProcessStartInfo BuildExtractCommand(
        string videoPath,
        int streamIndex,
        string destinationPath) =>
        new()
        {
            FileName = "ffmpeg",
            ArgumentList =
            {
                "-y",
                "-i", videoPath,
                "-map", $"0:{streamIndex}",
                "-c:s", "srt",
                destinationPath
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

    private static bool IsEnglish(EmbeddedSubtitleStream stream)
    {
        var language = SubtitleNaming.NormalizeLanguage(stream.Language);
        if (language == "en")
        {
            return true;
        }

        return string.Equals(stream.Language, "en", StringComparison.OrdinalIgnoreCase)
               || string.Equals(stream.Language, "eng", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSignsOnly(EmbeddedSubtitleStream stream) =>
        stream.Title != null
        && stream.Title.Contains("sign", StringComparison.OrdinalIgnoreCase)
        && !stream.Title.Contains("dialogue", StringComparison.OrdinalIgnoreCase);

    private static bool IsSdh(EmbeddedSubtitleStream stream) =>
        stream.Title != null
        && (stream.Title.Contains("sdh", StringComparison.OrdinalIgnoreCase)
            || stream.Title.Contains("caption", StringComparison.OrdinalIgnoreCase));
}
