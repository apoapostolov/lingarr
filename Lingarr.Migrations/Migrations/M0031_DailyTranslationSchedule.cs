using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(31)]
public class M0031_DailyTranslationSchedule : Migration
{
    public override void Up()
    {
        // The code default is once a day. Replace the every-3-hours value so
        // translation runs at 02:00 UTC, before housekeeping and statistics.
        Update.Table("settings")
            .Set(new { value = "0 2 * * *" })
            .Where(new { key = "translation_schedule", value = "15 */3 * * *" });
    }

    public override void Down()
    {
    }
}
