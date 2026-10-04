using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(28)]
public class M0028_PlexTranslateOnLibraryNew : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "plex_translate_on_library_new", value = "true" });
    }

    public override void Down()
    {
    }
}
