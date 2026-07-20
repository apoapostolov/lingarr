using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

/// <summary>
/// Regression: openrouter/free must be the first model option (Bedroom free-metamodel default).
/// </summary>
public class OpenRouterModelOrderingTests
{
    [Fact]
    public async Task GetModels_PlacesOpenRouterFreeFirst()
    {
        var payload = """
        {
          "data": [
            { "id": "anthropic/claude-sonnet-4", "name": "Claude Sonnet 4", "pricing": { "prompt": "0.001", "completion": "0.002" } },
            { "id": "openrouter/auto", "name": "Auto" },
            { "id": "openrouter/free", "name": "Free" },
            { "id": "meta/llama", "name": "Llama", "pricing": { "prompt": "0", "completion": "0" } }
          ]
        }
        """;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

        var settings = new Mock<ISettingService>();
        settings.Setup(s => s.GetEncryptedSetting(SettingKeys.Translation.OpenRouter.ApiKey))
            .ReturnsAsync("test-key");

        var templates = new Mock<IRequestTemplateService>();
        var language = new LanguageCodeService();

        var service = new OpenRouterService(
            settings.Object,
            new HttpClient(handler.Object),
            NullLogger<OpenRouterService>.Instance,
            language,
            templates.Object);

        var models = await service.GetModels();
        Assert.NotNull(models.Options);
        Assert.True(models.Options.Count >= 3);
        Assert.Equal("openrouter/free", models.Options[0].Value);
        Assert.Equal("openrouter/auto", models.Options[1].Value);
        // free injected once even if present in payload
        Assert.Equal(1, models.Options.Count(o => o.Value == "openrouter/free"));
    }

    [Fact]
    public async Task GetModels_InjectsFreeWhenCatalogueOmitsIt()
    {
        var payload = """
        {
          "data": [
            { "id": "some/paid-model", "name": "Paid" }
          ]
        }
        """;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

        var settings = new Mock<ISettingService>();
        settings.Setup(s => s.GetEncryptedSetting(SettingKeys.Translation.OpenRouter.ApiKey))
            .ReturnsAsync("test-key");

        var service = new OpenRouterService(
            settings.Object,
            new HttpClient(handler.Object),
            NullLogger<OpenRouterService>.Instance,
            new LanguageCodeService(),
            new Mock<IRequestTemplateService>().Object);

        var models = await service.GetModels();
        Assert.Equal("openrouter/free", models.Options[0].Value);
        Assert.Contains(models.Options, o => o.Value == "some/paid-model");
    }
}
