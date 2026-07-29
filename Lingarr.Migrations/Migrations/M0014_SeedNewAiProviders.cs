using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(14)]
public class M0014_SeedNewAiProviders : Migration
{
    public override void Up()
    {
        // FluentMigrator quotes the reserved "key" column correctly for every database.
        void Seed(string key, string value)
        {
            Insert.IntoTable("settings").Row(new { key, value });
        }

        Seed("openrouter_api_key", "");
        Seed("openrouter_endpoint", "https://openrouter.ai/api/v1/");
        // openrouter/free is the preferred Bedroom default (free metamodel for bulk/low-quality OK work).
        Seed("openrouter_model", "openrouter/free");
        Seed("openrouter_temperature", "0.3");
        Seed("openrouter_max_tokens", "4096");
        Seed("openrouter_request_template", "");
        Seed("zai_api_key", "");
        Seed("zai_endpoint", "https://api.z.ai/api/coding/paas/v4");
        Seed("zai_model", "glm-5.2");  // Coding Plan default
        Seed("zai_request_template", "");
        Seed("opencode_go_api_key", "");
        Seed("opencode_go_endpoint", "https://opencode.ai/zen/go/v1");
        Seed("opencode_go_model", "deepseek-v4-flash");
        Seed("opencode_go_request_template", "");
    }

    public override void Down()
    {
        // no-op: keep settings
    }
}
