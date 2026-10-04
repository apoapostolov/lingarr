using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(27)]
public class M0027_PlexIgnoreEnvironment : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "plex_ignore_environment", value = "false" });
    }

    public override void Down()
    {
    }
}
