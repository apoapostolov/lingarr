using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(19)]
public class M0019_DashboardActivityWindow : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings")
            .Row(new { key = "dashboard_activity_window_hours", value = "48" });
    }

    public override void Down()
    {
        Delete.FromTable("settings")
            .Row(new { key = "dashboard_activity_window_hours" });
    }
}
