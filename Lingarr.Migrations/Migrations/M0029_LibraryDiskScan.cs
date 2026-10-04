using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(29)]
public class M0029_LibraryDiskScan : Migration
{
    public override void Up()
    {
        Alter.Table("movies").AddColumn("disk_stamp").AsString(32).Nullable();
        Alter.Table("episodes").AddColumn("disk_stamp").AsString(32).Nullable();
        Insert.IntoTable("settings").Row(new { key = "library_disk_scan_enabled", value = "false" });
    }

    public override void Down()
    {
    }
}
