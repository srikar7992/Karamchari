using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.PlatformIntelligence.Migrations
{
    /// <inheritdoc />
    public partial class Phase82_RiskAndRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Platform_WorkforceRisks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RationaleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_WorkforceRisks", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_WorkforceRisks_TenantId_Category",
                table: "Platform_WorkforceRisks",
                columns: new[] { "TenantId", "Category" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "Platform_Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    EstimatedSavings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RiskReductionPercent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ExpectedBenefitJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedRiskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_Recommendations", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_Recommendations_TenantId_Status_GeneratedAt",
                table: "Platform_Recommendations",
                columns: new[] { "TenantId", "Status", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Platform_Recommendations_LinkedRiskId",
                table: "Platform_Recommendations",
                column: "LinkedRiskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Platform_Recommendations");
            migrationBuilder.DropTable(name: "Platform_WorkforceRisks");
        }
    }
}
