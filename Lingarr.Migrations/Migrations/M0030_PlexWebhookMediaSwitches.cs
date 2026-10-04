using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(30)]
public class M0030_PlexWebhookMediaSwitches : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "plex_translate_movies_on_library_new", value = "true" });
        Insert.IntoTable("settings").Row(new { key = "plex_translate_episodes_on_library_new", value = "true" });
    }

    public override void Down()
    {
    }
}
