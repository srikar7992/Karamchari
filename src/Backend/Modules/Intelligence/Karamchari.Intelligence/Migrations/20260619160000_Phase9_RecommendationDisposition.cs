using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations;

/// <inheritdoc />
public partial class Phase9_RecommendationDisposition : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Intel_RecommendationDispositions ─────────────────────────────────
        // Records the fate of every recommendation presented to an owner.
        // Without this, acceptance rate (recommended / acted-upon) is unmeasurable,
        // making it impossible to distinguish "ineffective interventions" from
        // "recommendations nobody wants to act on."
        migrationBuilder.CreateTable(
            name: "Intel_RecommendationDispositions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Disposition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ActorId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_RecommendationDispositions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_RecommendationDispositions_TenantId_RecommendationId",
            table: "Intel_RecommendationDispositions",
            columns: new[] { "TenantId", "RecommendationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Intel_RecommendationDispositions_TenantId_EmployeeId",
            table: "Intel_RecommendationDispositions",
            columns: new[] { "TenantId", "EmployeeId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Intel_RecommendationDispositions");
    }
}
