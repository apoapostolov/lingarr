using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_LlmUsageMetrics : Migration
{
    public override void Up()
    {
        Alter.Table("provider_operational_events")
            .AddColumn("input_tokens").AsInt64().Nullable()
            .AddColumn("output_tokens").AsInt64().Nullable()
            .AddColumn("estimated_cost_usd").AsDecimal(18, 8).Nullable();
    }

    public override void Down()
    {
        Delete.Column("estimated_cost_usd").FromTable("provider_operational_events");
        Delete.Column("output_tokens").FromTable("provider_operational_events");
        Delete.Column("input_tokens").FromTable("provider_operational_events");
    }
}
