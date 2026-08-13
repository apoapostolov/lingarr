using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(20)]
public class M0020_TranslationPromptProfiles : Migration
{
    public override void Up()
    {
        Create.Table("translation_prompt_profiles")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("type").AsString(20).NotNullable()
            .WithColumn("name").AsString(120).NotNullable()
            .WithColumn("description").AsCustom("TEXT").NotNullable()
            .WithColumn("draft_content").AsCustom("TEXT").NotNullable()
            .WithColumn("current_published_version_id").AsInt32().Nullable()
            .WithColumn("is_archived").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ux_translation_prompt_profiles_type_name")
            .OnTable("translation_prompt_profiles")
            .OnColumn("type").Ascending()
            .OnColumn("name").Ascending()
            .WithOptions().Unique();

        Create.Table("translation_prompt_profile_versions")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("profile_id").AsInt32().NotNullable()
                .ForeignKey(
                    "fk_translation_prompt_versions_profiles_id",
                    "translation_prompt_profiles",
                    "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("version_number").AsInt32().NotNullable()
            .WithColumn("content").AsCustom("TEXT").NotNullable()
            .WithColumn("change_note").AsCustom("TEXT").NotNullable()
            .WithColumn("content_hash").AsString(64).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ux_translation_prompt_versions_profile_version")
            .OnTable("translation_prompt_profile_versions")
            .OnColumn("profile_id").Ascending()
            .OnColumn("version_number").Ascending()
            .WithOptions().Unique();

        Create.Table("translation_prompt_usages")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("translation_request_id").AsInt32().NotNullable()
                .ForeignKey(
                    "fk_translation_prompt_usages_requests_id",
                    "translation_requests",
                    "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("chain_row_id").AsString(80).NotNullable()
            .WithColumn("provider").AsString(80).NotNullable()
            .WithColumn("model").AsString(200).Nullable()
            .WithColumn("system_profile_id").AsInt32().Nullable()
            .WithColumn("system_version_id").AsInt32().Nullable()
            .WithColumn("system_content_hash").AsString(64).Nullable()
            .WithColumn("context_profile_id").AsInt32().Nullable()
            .WithColumn("context_version_id").AsInt32().Nullable()
            .WithColumn("context_content_hash").AsString(64).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ux_translation_prompt_usages_request_row")
            .OnTable("translation_prompt_usages")
            .OnColumn("translation_request_id").Ascending()
            .OnColumn("chain_row_id").Ascending()
            .WithOptions().Unique();

        Insert.IntoTable("settings")
            .Row(new { key = "active_system_prompt_profile_id", value = "" });
        Insert.IntoTable("settings")
            .Row(new { key = "active_context_prompt_profile_id", value = "" });

        ImportLegacyProfile(
            "system",
            "Default translation instructions",
            "Imported from the original System Prompt setting.",
            "ai_prompt");
        ImportLegacyProfile(
            "context",
            "Default surrounding context",
            "Imported from the original Context Prompt setting.",
            "ai_context_prompt");
        Execute.Sql("""
            INSERT INTO translation_prompt_profile_versions
                (profile_id, version_number, content, change_note, content_hash, created_at, updated_at)
            SELECT
                p.id,
                1,
                p.draft_content,
                'Imported during prompt profile migration',
                'legacy-import',
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            FROM translation_prompt_profiles p;
            """);
        Execute.Sql("""
            UPDATE translation_prompt_profiles
            SET current_published_version_id = (
                SELECT MAX(v.id)
                FROM translation_prompt_profile_versions v
                WHERE v.profile_id = translation_prompt_profiles.id
            );
            """);

        void ImportLegacyProfile(
            string type,
            string name,
            string description,
            string settingKey)
        {
            string BuildSql(string quotedKey) => $"""
                INSERT INTO translation_prompt_profiles
                    (type, name, description, draft_content, current_published_version_id, is_archived, created_at, updated_at)
                SELECT
                    '{type}',
                    '{name}',
                    '{description}',
                    value,
                    NULL,
                    FALSE,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM settings
                WHERE {quotedKey} = '{settingKey}';
                """;

            IfDatabase("mysql").Execute.Sql(BuildSql("`key`"));
            IfDatabase("sqlite", "postgresql").Execute.Sql(BuildSql("\"key\""));
        }
    }

    public override void Down()
    {
        Delete.Table("translation_prompt_usages");
        Delete.Table("translation_prompt_profile_versions");
        Delete.Table("translation_prompt_profiles");
        Delete.FromTable("settings")
            .Row(new { key = "active_system_prompt_profile_id" });
        Delete.FromTable("settings")
            .Row(new { key = "active_context_prompt_profile_id" });
    }
}
