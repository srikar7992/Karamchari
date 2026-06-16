using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class Phase4cResultImmutabilityHardening : Migration
    {
        // NEUTRALIZED (Phase 0 seam fix). This migration was scaffolded from a
        // stale model snapshot that did not include 20260605173555_Phase4b_CalculationEngine,
        // so EF re-emitted Phase4b's exact diff (drop ShiftPremiumRules.EndTime/StartTime,
        // OvertimePolicies.DailyMultiplier, etc.). On a clean database Phase4b applies the
        // diff and Phase4c then re-drops the same already-removed columns, failing with
        // SQL error 4924 ("column does not exist") and aborting --provision-dev-tenants
        // before any tenant/user seeding runs. The two migrations' operation sets are
        // byte-identical, so the model state after Phase4b already equals the state the
        // PayrollDbContextModelSnapshot expects; this migration therefore contributes
        // nothing and is reduced to a no-op. The history row (20260605174803) is retained
        // so the applied-migrations chain and snapshot stay consistent.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — duplicate of Phase4b_CalculationEngine.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — duplicate of Phase4b_CalculationEngine.
        }
    }
}
