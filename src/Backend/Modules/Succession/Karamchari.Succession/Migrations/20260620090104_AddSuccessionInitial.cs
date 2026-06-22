using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Succession.Migrations
{
    /// <inheritdoc />
    public partial class AddSuccessionInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "SuccessionPlans",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PositionTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IncumbentEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalentReviews",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewCycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PerformanceScore = table.Column<int>(type: "int", nullable: false),
                    PotentialScore = table.Column<int>(type: "int", nullable: false),
                    NineBoxPosition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DevelopmentAreas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsSuccessionCandidate = table.Column<bool>(type: "bit", nullable: false),
                    IsFlightRisk = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuccessionNominations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NominatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SuccessionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessionNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuccessionNominations_SuccessionPlans_SuccessionPlanId",
                        column: x => x.SuccessionPlanId,
                        principalSchema: "__tenant__",
                        principalTable: "SuccessionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionNominations_SuccessionPlanId",
                schema: "__tenant__",
                table: "SuccessionNominations",
                column: "SuccessionPlanId");

            migrationBuilder.CreateIndex(
                name: "UIX_SuccessionNominations_Plan_Employee",
                schema: "__tenant__",
                table: "SuccessionNominations",
                columns: new[] { "PlanId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionPlans_Tenant_Critical",
                schema: "__tenant__",
                table: "SuccessionPlans",
                columns: new[] { "TenantId", "IsCritical" });

            migrationBuilder.CreateIndex(
                name: "IX_TalentReviews_Tenant_NineBox",
                schema: "__tenant__",
                table: "TalentReviews",
                columns: new[] { "TenantId", "NineBoxPosition" });

            migrationBuilder.CreateIndex(
                name: "UIX_TalentReviews_Tenant_Employee_Cycle",
                schema: "__tenant__",
                table: "TalentReviews",
                columns: new[] { "TenantId", "EmployeeId", "ReviewCycleName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuccessionNominations",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "TalentReviews",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "SuccessionPlans",
                schema: "__tenant__");
        }
    }
}
