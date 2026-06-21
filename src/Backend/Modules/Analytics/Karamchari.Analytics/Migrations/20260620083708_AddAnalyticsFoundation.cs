using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Analytics.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "Agg_MonthlyHeadcount",
                schema: "analytics",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeadcountStart = table.Column<int>(type: "int", nullable: false),
                    HeadcountEnd = table.Column<int>(type: "int", nullable: false),
                    Hires = table.Column<int>(type: "int", nullable: false),
                    Attrition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agg_MonthlyHeadcount", x => new { x.TenantId, x.Year, x.Month, x.DepartmentId });
                });

            migrationBuilder.CreateTable(
                name: "Dim_Date",
                schema: "analytics",
                columns: table => new
                {
                    DateKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Quarter = table.Column<int>(type: "int", nullable: false),
                    WeekOfYear = table.Column<int>(type: "int", nullable: false),
                    IsWeekend = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dim_Date", x => x.DateKey);
                });

            migrationBuilder.CreateTable(
                name: "Dim_Employee",
                schema: "analytics",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dim_Employee", x => x.EmployeeId);
                });

            migrationBuilder.CreateTable(
                name: "Fact_Attrition",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenureMonths = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fact_Attrition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fact_Hiring",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceChannel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OfferToJoinDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fact_Hiring", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fact_WorkforceDaily",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LeaveBalance = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fact_WorkforceDaily", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dim_Employee_TenantId_IsActive",
                schema: "analytics",
                table: "Dim_Employee",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Fact_Attrition_TenantId_DateKey",
                schema: "analytics",
                table: "Fact_Attrition",
                columns: new[] { "TenantId", "DateKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Fact_Hiring_TenantId_DateKey",
                schema: "analytics",
                table: "Fact_Hiring",
                columns: new[] { "TenantId", "DateKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Fact_WorkforceDaily_TenantId_DateKey",
                schema: "analytics",
                table: "Fact_WorkforceDaily",
                columns: new[] { "TenantId", "DateKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Fact_WorkforceDaily_TenantId_EmployeeId_DateKey",
                schema: "analytics",
                table: "Fact_WorkforceDaily",
                columns: new[] { "TenantId", "EmployeeId", "DateKey" },
                unique: true);

            migrationBuilder.EnsureSchema("analytics");
            migrationBuilder.Sql(@"
DECLARE @d DATE = '2020-01-01'; DECLARE @end DATE = '2030-12-31';
WHILE @d <= @end BEGIN
  INSERT INTO analytics.[Dim_Date] (DateKey, [Date], [Year], [Month], Quarter, WeekOfYear, IsWeekend)
  VALUES (CAST(FORMAT(@d,'yyyyMMdd') AS INT), @d, YEAR(@d), MONTH(@d),
    DATEPART(QUARTER,@d), DATEPART(WEEK,@d),
    CASE WHEN DATEPART(WEEKDAY,@d) IN (1,7) THEN 1 ELSE 0 END);
  SET @d = DATEADD(DAY,1,@d);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agg_MonthlyHeadcount",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "Dim_Date",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "Dim_Employee",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "Fact_Attrition",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "Fact_Hiring",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "Fact_WorkforceDaily",
                schema: "analytics");

            migrationBuilder.Sql("DELETE FROM analytics.[Dim_Date] WHERE DateKey BETWEEN 20200101 AND 20301231;");
        }
    }
}
