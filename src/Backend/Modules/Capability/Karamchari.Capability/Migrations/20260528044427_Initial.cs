using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations
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
                name: "Capability_CertificationAchievements",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Authority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_CertificationAchievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_Definitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapabilityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresLicense = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_Definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_GrowthPlans",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRoleOrCapability = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TargetCompletionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_GrowthPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_LearningEnrollments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    DeadlineUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletionIdempotencyKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_LearningEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_LearningModules",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    ExternalProviderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_LearningModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_Profiles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_SkillDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_SkillDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_TenantEntitlements",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CapabilityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnabledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_TenantEntitlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_GrowthMilestones",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrowthPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAchieved = table.Column<bool>(type: "bit", nullable: false),
                    AchievedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_GrowthMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capability_GrowthMilestones_Capability_GrowthPlans_GrowthPlanId",
                        column: x => x.GrowthPlanId,
                        principalSchema: "__tenant__",
                        principalTable: "Capability_GrowthPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Capability_VerifiedSkills",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_VerifiedSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capability_VerifiedSkills_Capability_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "__tenant__",
                        principalTable: "Capability_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_CertificationAchievements_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Capability_CertificationAchievements",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_Definitions_CapabilityId",
                schema: "__tenant__",
                table: "Capability_Definitions",
                column: "CapabilityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capability_GrowthMilestones_GrowthPlanId",
                schema: "__tenant__",
                table: "Capability_GrowthMilestones",
                column: "GrowthPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Capability_GrowthPlans_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Capability_GrowthPlans",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_LearningEnrollments_TenantId_EmployeeId_ModuleId",
                schema: "__tenant__",
                table: "Capability_LearningEnrollments",
                columns: new[] { "TenantId", "EmployeeId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capability_LearningModules_TenantId_Title",
                schema: "__tenant__",
                table: "Capability_LearningModules",
                columns: new[] { "TenantId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_Profiles_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Capability_Profiles",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capability_SkillDefinitions_TenantId_Name",
                schema: "__tenant__",
                table: "Capability_SkillDefinitions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capability_TenantEntitlements_TenantId_CapabilityId",
                schema: "__tenant__",
                table: "Capability_TenantEntitlements",
                columns: new[] { "TenantId", "CapabilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capability_VerifiedSkills_ProfileId_SkillId",
                schema: "__tenant__",
                table: "Capability_VerifiedSkills",
                columns: new[] { "ProfileId", "SkillId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capability_CertificationAchievements",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_Definitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_GrowthMilestones",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_LearningEnrollments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_LearningModules",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_SkillDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_TenantEntitlements",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_VerifiedSkills",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_GrowthPlans",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_Profiles",
                schema: "__tenant__");
        }
    }
}
