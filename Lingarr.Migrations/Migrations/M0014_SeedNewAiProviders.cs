using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(14)]
public class M0014_SeedNewAiProviders : Migration
{
    public override void Up()
    {
        // Idempotent seeds for new AI provider settings
        void Seed(string key, string value)
        {
            Execute.Sql($@"
INSERT INTO settings (key, value)
SELECT '{key}', '{value}'
WHERE NOT EXISTS (SELECT 1 FROM settings WHERE key = '{key}');
");
        }

        Seed("openrouter_api_key", "");
        Seed("openrouter_endpoint", "https://openrouter.ai/api/v1/");
        Seed("openrouter_model", "");
        Seed("openrouter_temperature", "0.3");
        Seed("openrouter_max_tokens", "4096");
        Seed("openrouter_request_template", "");
        Seed("zai_api_key", "");
        Seed("zai_endpoint", "https://api.z.ai/api/paas/v4");
        Seed("zai_model", "glm-5.2");
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
