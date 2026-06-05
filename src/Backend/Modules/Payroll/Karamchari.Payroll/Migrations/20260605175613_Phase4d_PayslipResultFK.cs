using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1707

namespace Karamchari.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class Phase4d_PayslipResultFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PayrollResultId",
                schema: "__tenant__",
                table: "Payslips",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedAtUtc",
                schema: "__tenant__",
                table: "EmployeePayrollResults",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PayrollResultId",
                schema: "__tenant__",
                table: "Payslips",
                column: "PayrollResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payslips_PayrollResultId",
                schema: "__tenant__",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "PayrollResultId",
                schema: "__tenant__",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                schema: "__tenant__",
                table: "EmployeePayrollResults");
        }
    }
}
