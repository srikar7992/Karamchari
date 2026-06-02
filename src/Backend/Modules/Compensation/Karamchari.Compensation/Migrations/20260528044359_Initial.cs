using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Compensation.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "CompensationBands",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobFamily = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CareerLevelCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BandType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MinimumAnnual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MidpointAnnual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumAnnual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationBands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCompensationRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAnnualCTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LastRevisedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCompensationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncrementBudgetPools",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewYear = table.Column<int>(type: "int", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncrementBudgetPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeritMatrices",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewYear = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeritMatrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompensationRevisions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompensationRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousAnnualCTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewAnnualCTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncrementPercent = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EmployeeCompensationRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompensationRevisions_EmployeeCompensationRecords_EmployeeCompensationRecordId",
                        column: x => x.EmployeeCompensationRecordId,
                        principalSchema: "__tenant__",
                        principalTable: "EmployeeCompensationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeritMatrixCells",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatrixId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CompaRatioMin = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    CompaRatioMax = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MinIncrementPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MidIncrementPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MaxIncrementPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MeritMatrixId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeritMatrixCells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeritMatrixCells_MeritMatrices_MeritMatrixId",
                        column: x => x.MeritMatrixId,
                        principalSchema: "__tenant__",
                        principalTable: "MeritMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompensationBands_JobFamily_Level_Type_Date",
                schema: "__tenant__",
                table: "CompensationBands",
                columns: new[] { "TenantId", "JobFamily", "CareerLevelCode", "BandType", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CompensationRevisions_EmployeeCompensationRecordId",
                schema: "__tenant__",
                table: "CompensationRevisions",
                column: "EmployeeCompensationRecordId");

            migrationBuilder.CreateIndex(
                name: "UIX_CompensationRevisions_ExternalId",
                schema: "__tenant__",
                table: "CompensationRevisions",
                columns: new[] { "CompensationRecordId", "ExternalRevisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_EmployeeCompensationRecords_Employee",
                schema: "__tenant__",
                table: "EmployeeCompensationRecords",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_IncrementBudgetPools_Dept_Year",
                schema: "__tenant__",
                table: "IncrementBudgetPools",
                columns: new[] { "TenantId", "DepartmentId", "ReviewYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_MeritMatrices_Tenant_Year",
                schema: "__tenant__",
                table: "MeritMatrices",
                columns: new[] { "TenantId", "ReviewYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeritMatrixCells_Matrix_Rating_Range",
                schema: "__tenant__",
                table: "MeritMatrixCells",
                columns: new[] { "MatrixId", "Rating", "CompaRatioMin" });

            migrationBuilder.CreateIndex(
                name: "IX_MeritMatrixCells_MeritMatrixId",
                schema: "__tenant__",
                table: "MeritMatrixCells",
                column: "MeritMatrixId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompensationBands",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CompensationRevisions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "IncrementBudgetPools",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "MeritMatrixCells",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "EmployeeCompensationRecords",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "MeritMatrices",
                schema: "__tenant__");
        }
    }
}
