using Lingarr.Server.Services.Translation;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class QwenMtServiceTests
{
    [Theory]
    [InlineData("bg", "bg")]
    [InlineData("en-US", "en")]
    [InlineData("pt_BR", "pt")]
    [InlineData("zh-TW", "zh_tw")]
    [InlineData("zh-HK", "yue")]
    [InlineData("nb-NO", "nb")]
    public void NormalizeLanguageCode_UsesQwenMtCodes(string input, string expected)
    {
        Assert.Equal(expected, QwenMtService.NormalizeLanguageCode(input));
    }

    [Fact]
    public void DefaultEndpoint_UsesModelStudioUsCompatibleApi()
    {
        Assert.Equal(
            "https://dashscope-us.aliyuncs.com/compatible-mode/v1",
            QwenService.DefaultEndpoint);
    }
}
