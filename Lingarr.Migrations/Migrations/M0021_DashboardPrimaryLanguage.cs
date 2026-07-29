using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(21)]
public class M0021_DashboardPrimaryLanguage : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings")
            .Row(new { key = "dashboard_primary_language", value = "" });
    }

    public override void Down()
    {
        Delete.FromTable("settings")
            .Row(new { key = "dashboard_primary_language" });
    }
}
