using Lingarr.Server.Services;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class MediaLibraryRefreshServiceTests
{
    [Theory]
    [InlineData("/media/media/movies/Foo (2021)", @"E:\media\movies\Foo (2021)")]
    [InlineData("/media/media/tvshows/Show/Season 1", @"E:\media\tvshows\Show\Season 1")]
    [InlineData("/mnt/e/media/movies/Foo", @"E:\media\movies\Foo")]
    public void ToWindowsMediaPath_MapsContainerAndWslPaths(string input, string expected)
    {
        Assert.Equal(expected, MediaLibraryRefreshService.ToWindowsMediaPath(input));
    }
}
