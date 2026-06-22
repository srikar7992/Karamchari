using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations;

/// <inheritdoc />
public partial class Phase8_InterventionInstance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Intel_InterventionInstances ──────────────────────────────────────
        // Bridges "what should be done" (WorkforceRecommendation) to
        // "what was actually done" (owner-executed intervention workflow).
        migrationBuilder.CreateTable(
            name: "Intel_InterventionInstances",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OwnerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Pending"),
                AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                OutcomeNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_InterventionInstances", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_InterventionInstances_TenantId_EmployeeId",
            table: "Intel_InterventionInstances",
            columns: new[] { "TenantId", "EmployeeId" });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_InterventionInstances_TenantId_RecommendationId",
            table: "Intel_InterventionInstances",
            columns: new[] { "TenantId", "RecommendationId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Intel_InterventionInstances");
    }
}
