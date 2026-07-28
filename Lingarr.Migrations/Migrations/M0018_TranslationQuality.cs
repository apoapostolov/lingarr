using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(18)]
public class M0018_TranslationQuality : Migration
{
    public override void Up()
    {
        Alter.Table("translation_requests")
            .AddColumn("quality_score").AsInt32().Nullable()
            .AddColumn("quality_grade").AsString(40).Nullable()
            .AddColumn("quality_status").AsString(30).Nullable();

        Create.Table("translation_quality_assessments")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("translation_request_id").AsInt32()
                .NotNullable()
                .ForeignKey(
                    "fk_translation_quality_assessments_translation_requests_id",
                    "translation_requests",
                    "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("ruleset_version").AsInt32().NotNullable()
            .WithColumn("score").AsInt32().Nullable()
            .WithColumn("grade").AsString(40).NotNullable()
            .WithColumn("average_line_score").AsDouble().Nullable()
            .WithColumn("low_tail_score").AsDouble().Nullable()
            .WithColumn("critical_count").AsInt32().NotNullable()
            .WithColumn("error_count").AsInt32().NotNullable()
            .WithColumn("warning_count").AsInt32().NotNullable()
            .WithColumn("line_count").AsInt32().NotNullable()
            .WithColumn("evaluated_at").AsDateTime().NotNullable()
            .WithColumn("evaluation_status").AsString(30).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_translation_quality_assessments_request_evaluated")
            .OnTable("translation_quality_assessments")
            .OnColumn("translation_request_id").Ascending()
            .OnColumn("evaluated_at").Ascending();

        Create.Table("translation_line_quality_findings")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("translation_quality_assessment_id").AsInt32()
                .NotNullable()
                .ForeignKey(
                    "fk_translation_line_quality_findings_assessments_id",
                    "translation_quality_assessments",
                    "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("translation_request_line_id").AsInt32().Nullable()
            .WithColumn("line_position").AsInt32().Nullable()
            .WithColumn("rule_id").AsString(100).NotNullable()
            .WithColumn("category").AsString(40).NotNullable()
            .WithColumn("severity").AsString(20).NotNullable()
            .WithColumn("penalty").AsInt32().NotNullable()
            .WithColumn("summary").AsCustom("TEXT").NotNullable()
            .WithColumn("metadata_json").AsCustom("TEXT").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_translation_line_quality_findings_assessment")
            .OnTable("translation_line_quality_findings")
            .OnColumn("translation_quality_assessment_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("translation_line_quality_findings");
        Delete.Table("translation_quality_assessments");
        Delete.Column("quality_status").FromTable("translation_requests");
        Delete.Column("quality_grade").FromTable("translation_requests");
        Delete.Column("quality_score").FromTable("translation_requests");
    }
}
