using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations;

/// <inheritdoc />
public partial class Phase10_AcceptanceRate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Intel_InterventionEffectiveness — acceptance-rate columns ─────────
        // Acceptance rate measures whether managers act on a recommendation,
        // independently of whether the intervention was effective once applied.
        // A template with 90% success but 2% acceptance is not operationally useful.
        // Storing counts alongside the rate preserves sample-size context:
        //   1/1 = 100%  vs  1000/1000 = 100% are not equally trustworthy.
        migrationBuilder.AddColumn<decimal>(
            name: "RecommendationAcceptanceRate",
            table: "Intel_InterventionEffectiveness",
            type: "decimal(5,3)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RecommendationAcceptanceCount",
            table: "Intel_InterventionEffectiveness",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "RecommendationDispositionCount",
            table: "Intel_InterventionEffectiveness",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RecommendationAcceptanceRate",
            table: "Intel_InterventionEffectiveness");

        migrationBuilder.DropColumn(
            name: "RecommendationAcceptanceCount",
            table: "Intel_InterventionEffectiveness");

        migrationBuilder.DropColumn(
            name: "RecommendationDispositionCount",
            table: "Intel_InterventionEffectiveness");
    }
}
