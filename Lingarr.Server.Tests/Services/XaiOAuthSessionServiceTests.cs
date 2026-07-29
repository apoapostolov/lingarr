using Lingarr.Server.Services;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class XaiOAuthSessionServiceTests
{
    [Fact]
    public void DeviceFlowConstants_MatchTheSupportedXaiOidcFlow()
    {
        Assert.Equal(
            "https://auth.x.ai/oauth2/device/code",
            XaiOAuthSessionService.DeviceCodeEndpoint);
        Assert.Equal(
            "https://auth.x.ai/oauth2/token",
            XaiOAuthSessionService.TokenEndpoint);
        Assert.Contains("offline_access", XaiOAuthSessionService.Scope);
        Assert.Contains("grok-cli:access", XaiOAuthSessionService.Scope);
        Assert.Contains("api:access", XaiOAuthSessionService.Scope);
    }
}
