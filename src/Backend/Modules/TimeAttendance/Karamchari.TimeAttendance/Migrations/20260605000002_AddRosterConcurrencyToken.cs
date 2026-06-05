using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations;

/// <summary>
/// Adds a SQL Server rowversion column to Roster_Rosters for optimistic concurrency control.
/// Two schedulers racing on the same roster will receive a 409 Conflict on the second write.
/// </summary>
public partial class AddRosterConcurrencyToken : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            schema: "__tenant__",
            table: "Roster_Rosters",
            rowVersion: true,
            nullable: false,
            defaultValue: Array.Empty<byte>());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RowVersion",
            schema: "__tenant__",
            table: "Roster_Rosters");
    }
}
