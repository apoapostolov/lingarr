using Lingarr.Core.Entities;
using Lingarr.Core.Interfaces;

namespace Lingarr.Server.Services;

public static class LibraryFolderStamp
{
    public static string? Read(IMedia media) => media switch
    {
        Movie movie => movie.DiskStamp,
        Episode episode => episode.DiskStamp,
        _ => null
    };

    public static void Write(IMedia media, string? stamp)
    {
        switch (media)
        {
            case Movie movie:
                movie.DiskStamp = stamp;
                break;
            case Episode episode:
                episode.DiskStamp = stamp;
                break;
        }
    }

    public static string? Capture(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        try
        {
            var info = new DirectoryInfo(directory);
            return info.Exists
                ? info.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static bool IsUnchanged(string? directory, string? stamp)
    {
        if (string.IsNullOrWhiteSpace(stamp))
        {
            return false;
        }

        var current = Capture(directory);
        return current != null && current == stamp;
    }
}
