using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1707

namespace Karamchari.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_IndiaPayrollProfileExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Create IndiaPayrollProfiles ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "IndiaPayrollProfiles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptedForVoluntaryPF = table.Column<bool>(type: "bit", nullable: false),
                    IsEsicLocked = table.Column<bool>(type: "bit", nullable: false),
                    StateCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TaxRegime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMetro = table.Column<bool>(type: "bit", nullable: false),
                    Pan = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Uan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EsicNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndiaPayrollProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndiaPayrollProfiles_PayrollProfiles_PayrollProfileId",
                        column: x => x.PayrollProfileId,
                        principalSchema: "__tenant__",
                        principalTable: "PayrollProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndiaPayrollProfiles_PayrollProfileId",
                schema: "__tenant__",
                table: "IndiaPayrollProfiles",
                column: "PayrollProfileId",
                unique: true);

            // ── Remove India-specific columns from PayrollProfiles ───────────────
            migrationBuilder.DropColumn(
                name: "EsicNumber",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "IsEsicLocked",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "IsMetro",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "OptedForVoluntaryPF",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "Pan",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "StateCode",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "TaxRegime",
                schema: "__tenant__",
                table: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "Uan",
                schema: "__tenant__",
                table: "PayrollProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Re-add India columns to PayrollProfiles ──────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "EsicNumber",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<bool>(
                name: "IsEsicLocked",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMetro",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OptedForVoluntaryPF",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Pan",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "TS");

            migrationBuilder.AddColumn<int>(
                name: "TaxRegime",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Uan",
                schema: "__tenant__",
                table: "PayrollProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            // ── Drop IndiaPayrollProfiles ─────────────────────────────────────────
            migrationBuilder.DropTable(
                name: "IndiaPayrollProfiles",
                schema: "__tenant__");
        }
    }
}
