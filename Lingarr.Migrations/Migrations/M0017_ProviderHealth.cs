using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(17)]
public class M0017_ProviderHealth : Migration
{
    public override void Up()
    {
        Create.Table("provider_operational_events")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("provider").AsString(100).NotNullable()
            .WithColumn("model").AsCustom("TEXT").Nullable()
            .WithColumn("operation").AsString(30).NotNullable()
            .WithColumn("outcome").AsString(20).NotNullable()
            .WithColumn("error_family").AsString(40).Nullable()
            .WithColumn("is_transient").AsBoolean().NotNullable()
            .WithColumn("duration_ms").AsInt64().NotNullable()
            .WithColumn("retry_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("translation_request_id").AsInt32().Nullable()
            .WithColumn("occurred_at").AsDateTime().NotNullable()
            .WithColumn("policy_version").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_provider_operational_events_provider_occurred_at")
            .OnTable("provider_operational_events")
            .OnColumn("provider").Ascending()
            .OnColumn("occurred_at").Ascending();

        Create.Table("provider_health_snapshots")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("provider").AsString(100).NotNullable()
            .WithColumn("state").AsString(30).NotNullable()
            .WithColumn("reason").AsCustom("TEXT").NotNullable()
            .WithColumn("last_success_at").AsDateTime().Nullable()
            .WithColumn("last_failure_at").AsDateTime().Nullable()
            .WithColumn("last_warning_at").AsDateTime().Nullable()
            .WithColumn("consecutive_failures").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("success_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("failure_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("median_duration_ms").AsInt64().Nullable()
            .WithColumn("evaluated_at").AsDateTime().NotNullable()
            .WithColumn("policy_version").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ux_provider_health_snapshots_provider")
            .OnTable("provider_health_snapshots")
            .OnColumn("provider")
            .Ascending()
            .WithOptions()
            .Unique();
    }

    public override void Down()
    {
        Delete.Table("provider_health_snapshots");
        Delete.Table("provider_operational_events");
    }
}
