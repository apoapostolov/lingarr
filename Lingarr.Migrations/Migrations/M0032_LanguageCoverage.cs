using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(32)]
public class M0032_LanguageCoverage : Migration
{
    public override void Up()
    {
        Alter.Table("movies").AddColumn("language_coverage").AsString(80).Nullable();
        Alter.Table("episodes").AddColumn("language_coverage").AsString(80).Nullable();
    }

    public override void Down()
    {
    }
}
