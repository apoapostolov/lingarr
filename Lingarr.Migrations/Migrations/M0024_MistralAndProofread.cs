using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(24)]
public class M0024_MistralAndProofread : Migration
{
    public override void Up()
    {
        void Seed(string key, string value)
        {
            Insert.IntoTable("settings").Row(new { key, value });
        }

        Seed("mistral_api_key", "");
        Seed("mistral_endpoint", "https://api.mistral.ai/v1");
        Seed("mistral_model", "mistral-small-latest");
        Seed("mistral_request_template", "");
        Seed("mistral_request_timeout", "5");

        Seed("proofread_prompt", "");
        Seed("proofread_user_prompt", "");
    }

    public override void Down()
    {
        // Keep provider and prompt rows if an installation rolls back.
    }
}
