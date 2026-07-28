using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(16)]
public class M0016_RemoveTelemetrySettings : Migration
{
    private static readonly string[] Keys =
    [
        "telemetry_enabled",
        "telemetry_last_submission",
        "telemetry_last_reported_lines",
        "telemetry_last_reported_files",
        "telemetry_last_reported_characters"
    ];

    public override void Up()
    {
        foreach (var key in Keys)
        {
            Delete.FromTable("settings").Row(new { key });
        }
    }

    public override void Down()
    {
        // Do not restore retired outbound reporting settings on rollback.
    }
}
