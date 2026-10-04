using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(26)]
public class M0026_PlexConnection : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "plex_client_identifier", value = Guid.NewGuid().ToString("D") });
        Insert.IntoTable("settings").Row(new { key = "plex_username", value = "" });
        Insert.IntoTable("settings").Row(new { key = "plex_server_name", value = "" });
        Insert.IntoTable("settings").Row(new { key = "plex_server_machine_id", value = "" });
        Insert.IntoTable("settings").Row(new { key = "plex_auth_method", value = "" });
        Insert.IntoTable("settings").Row(new { key = "plex_set_selected_subtitle", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "plex_default_subtitle_language", value = "" });
    }

    public override void Down()
    {
    }
}
