using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(15)]
public class M0015_SeedProviderRequestTimeouts : Migration
{
    public override void Up()
    {
        Seed("libretranslate_request_timeout", "5");
        Seed("google_request_timeout", "5");
        Seed("bing_request_timeout", "5");
        Seed("microsoft_request_timeout", "15");
        Seed("yandex_request_timeout", "5");
        Seed("deepl_request_timeout", "5");
        Seed("openai_request_timeout", "5");
        Seed("anthropic_request_timeout", "5");
        Seed("localai_request_timeout", "5");
        Seed("gemini_request_timeout", "5");
        Seed("deepseek_request_timeout", "5");
        Seed("openrouter_request_timeout", "5");
        Seed("zai_request_timeout", "5");
        Seed("opencode_go_request_timeout", "5");
    }

    public override void Down()
    {
        // Keep user-configured timeout values when rolling back.
    }

    private void Seed(string key, string value)
    {
        Execute.Sql($"""
INSERT INTO settings (key, value)
SELECT '{key}', '{value}'
WHERE NOT EXISTS (SELECT 1 FROM settings WHERE key = '{key}');
""");
    }
}
