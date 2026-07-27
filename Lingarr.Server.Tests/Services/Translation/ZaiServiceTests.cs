using Lingarr.Server.Services.Translation;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class ZaiServiceTests
{
    [Fact]
    public void NormalizeCodingEndpoint_Null_ReturnsGlobalCoding()
    {
        Assert.Equal(ZaiService.CodingPlanGlobalEndpoint, ZaiService.NormalizeCodingEndpoint(null));
    }

    [Fact]
    public void NormalizeCodingEndpoint_Empty_ReturnsGlobalCoding()
    {
        Assert.Equal(ZaiService.CodingPlanGlobalEndpoint, ZaiService.NormalizeCodingEndpoint("  "));
    }

    [Fact]
    public void NormalizeCodingEndpoint_GeneralApi_RewritesToCoding()
    {
        Assert.Equal(
            ZaiService.CodingPlanGlobalEndpoint,
            ZaiService.NormalizeCodingEndpoint(ZaiService.GeneralApiGlobalEndpoint));
        Assert.Equal(
            ZaiService.CodingPlanGlobalEndpoint,
            ZaiService.NormalizeCodingEndpoint("https://api.z.ai/api/paas/v4/"));
    }

    [Fact]
    public void NormalizeCodingEndpoint_GeneralCn_RewritesToCodingCn()
    {
        Assert.Equal(
            ZaiService.CodingPlanCnEndpoint,
            ZaiService.NormalizeCodingEndpoint("https://open.bigmodel.cn/api/paas/v4"));
    }

    [Fact]
    public void NormalizeCodingEndpoint_AlreadyCoding_Keeps()
    {
        Assert.Equal(
            ZaiService.CodingPlanGlobalEndpoint,
            ZaiService.NormalizeCodingEndpoint(ZaiService.CodingPlanGlobalEndpoint));
        Assert.Equal(
            ZaiService.CodingPlanCnEndpoint,
            ZaiService.NormalizeCodingEndpoint(ZaiService.CodingPlanCnEndpoint + "/"));
    }

    [Fact]
    public void NormalizeCodingEndpoint_CustomProxy_Keeps()
    {
        var proxy = "https://proxy.example.local/zai/v1";
        Assert.Equal(proxy, ZaiService.NormalizeCodingEndpoint(proxy));
    }

    [Fact]
    public void CodingPlanConstants_MatchOfficialDocs()
    {
        Assert.Equal("https://api.z.ai/api/coding/paas/v4", ZaiService.CodingPlanGlobalEndpoint);
        Assert.Equal("https://open.bigmodel.cn/api/coding/paas/v4", ZaiService.CodingPlanCnEndpoint);
        Assert.Equal("https://api.z.ai/api/paas/v4", ZaiService.GeneralApiGlobalEndpoint);
    }
}
