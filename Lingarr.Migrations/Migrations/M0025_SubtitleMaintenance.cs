using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(25)]
public class M0025_SubtitleMaintenance : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "subtitle_naming_enabled", value = "true" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_extract_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_maintenance_schedule", value = "0 3 * * 0" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_extract_max_per_run", value = "80" });
        Insert.IntoTable("settings").Row(new { key = "plex_url", value = "" });
        Insert.IntoTable("settings").Row(new { key = "plex_token", value = "" });
        Insert.IntoTable("settings").Row(new { key = "jellyfin_url", value = "" });
        Insert.IntoTable("settings").Row(new { key = "jellyfin_token", value = "" });
    }

    public override void Down()
    {
    }
}
