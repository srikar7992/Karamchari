using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations;

/// <inheritdoc />
public partial class Phase7_InterventionLibrary : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Intel_InterventionTemplates ──────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Intel_InterventionTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                WorkflowTemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PreconditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_InterventionTemplates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_InterventionTemplates_TenantId_Name",
            table: "Intel_InterventionTemplates",
            columns: new[] { "TenantId", "Name" },
            unique: true);

        // ── Intel_InterventionEffectiveness ──────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Intel_InterventionEffectiveness",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SignalType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Applications = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                SuccessRate = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false, defaultValue: 0.5m),
                MedianOutcomeDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                Confidence = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false, defaultValue: 0.0m),
                LastComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_InterventionEffectiveness", x => x.Id);
                table.ForeignKey(
                    name: "FK_Intel_InterventionEffectiveness_Intel_InterventionTemplates_TemplateId",
                    column: x => x.TemplateId,
                    principalTable: "Intel_InterventionTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_InterventionEffectiveness_TenantId_TemplateId_SignalType",
            table: "Intel_InterventionEffectiveness",
            columns: new[] { "TenantId", "TemplateId", "SignalType" },
            unique: true);

        // ── Extend Intel_WorkforceRecommendations ────────────────────────────
        migrationBuilder.AddColumn<Guid>(
            name: "TemplateId",
            table: "Intel_WorkforceRecommendations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OwnerType",
            table: "Intel_WorkforceRecommendations",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "System");

        migrationBuilder.AddColumn<string>(
            name: "OwnerId",
            table: "Intel_WorkforceRecommendations",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        // ── Extend Intel_InterventionOutcomes ────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "ObservedOutcomeSource",
            table: "Intel_InterventionOutcomes",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "ScoreModel");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ObservedOutcomeSource",
            table: "Intel_InterventionOutcomes");

        migrationBuilder.DropColumn(
            name: "OwnerId",
            table: "Intel_WorkforceRecommendations");

        migrationBuilder.DropColumn(
            name: "OwnerType",
            table: "Intel_WorkforceRecommendations");

        migrationBuilder.DropColumn(
            name: "TemplateId",
            table: "Intel_WorkforceRecommendations");

        migrationBuilder.DropTable(name: "Intel_InterventionEffectiveness");
        migrationBuilder.DropTable(name: "Intel_InterventionTemplates");
    }
}
