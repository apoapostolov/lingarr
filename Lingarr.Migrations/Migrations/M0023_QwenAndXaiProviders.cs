using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(23)]
public class M0023_QwenAndXaiProviders : Migration
{
    public override void Up()
    {
        void Seed(string key, string value)
        {
            Insert.IntoTable("settings").Row(new { key, value });
        }

        Seed("qwen_api_key", "");
        Seed("qwen_endpoint", "https://dashscope-us.aliyuncs.com/compatible-mode/v1");
        Seed("qwen_model", "qwen3.7-plus");
        Seed("qwen_request_template", "");
        Seed("qwen_mt_model", "qwen-mt-flash");
        Seed("qwen_request_timeout", "5");
        Seed("qwen_mt_request_timeout", "5");

        Seed("xai_api_key", "");
        Seed("xai_endpoint", "https://api.x.ai/v1");
        Seed("xai_model", "grok-4.5");
        Seed("xai_request_template", "");
        Seed("xai_request_timeout", "5");

        Seed("xai_oauth_model", "grok-4.5");
        Seed("xai_oauth_connection", "");
        Seed("xai_oauth_access_token", "");
        Seed("xai_oauth_refresh_token", "");
        Seed("xai_oauth_expires_at", "");
        Seed("xai_oauth_request_timeout", "5");
    }

    public override void Down()
    {
        // Keep provider configuration if an installation rolls back.
    }
}
