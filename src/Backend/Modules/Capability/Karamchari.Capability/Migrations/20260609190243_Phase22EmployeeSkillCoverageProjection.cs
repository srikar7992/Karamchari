using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations
{
    /// <inheritdoc />
    public partial class Phase22EmployeeSkillCoverageProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDelegations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DelegatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DelegateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDelegations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalInstances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApprovalDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_EmployeeCapabilityProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    VerifiedSkills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActiveGigsCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_EmployeeCapabilityProjections", x => new { x.TenantId, x.EmployeeId });
                });

            migrationBuilder.CreateTable(
                name: "Capability_EmployeeSkillCoverageProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    MatchedSkillCount = table.Column<int>(type: "int", nullable: false),
                    MissingSkillCount = table.Column<int>(type: "int", nullable: false),
                    TotalRequiredSkillCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_EmployeeSkillCoverageProjections", x => new { x.TenantId, x.EmployeeId, x.RoleRequirementId });
                });

            migrationBuilder.CreateTable(
                name: "Capability_Opportunities",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    AllocatedHoursPerWeek = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovalWorkflow = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_Opportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_OpportunityParticipants",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AllocatedHours = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_OpportunityParticipants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_SkillEvidences",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_SkillEvidences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_SkillGraphRelationships",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_SkillGraphRelationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capability_SkillNodes",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SkillCluster = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_SkillNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectionCheckpoints",
                schema: "__tenant__",
                columns: table => new
                {
                    ProjectionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PartitionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastProcessedEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastProcessedPosition = table.Column<long>(type: "bigint", nullable: false),
                    LastProcessedVersion = table.Column<int>(type: "int", nullable: false),
                    ProcessedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionCheckpoints", x => new { x.ProjectionName, x.PartitionKey });
                });

            migrationBuilder.CreateTable(
                name: "ProjectionDeadLetters",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionDeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredEvents",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CausationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EffectiveUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TransactionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStages",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalStages_ApprovalDefinitions_ApprovalDefinitionId",
                        column: x => x.ApprovalDefinitionId,
                        principalSchema: "__tenant__",
                        principalTable: "ApprovalDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_ApprovalInstances_ApprovalInstanceId",
                        column: x => x.ApprovalInstanceId,
                        principalSchema: "__tenant__",
                        principalTable: "ApprovalInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Capability_OpportunityRequirements",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_OpportunityRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capability_OpportunityRequirements_Capability_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalSchema: "__tenant__",
                        principalTable: "Capability_Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_ApprovalInstanceId",
                schema: "__tenant__",
                table: "ApprovalDecisions",
                column: "ApprovalInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_TenantId_DelegateId",
                schema: "__tenant__",
                table: "ApprovalDelegations",
                columns: new[] { "TenantId", "DelegateId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_TenantId_DelegatorId",
                schema: "__tenant__",
                table: "ApprovalDelegations",
                columns: new[] { "TenantId", "DelegatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstance_Queue",
                schema: "__tenant__",
                table: "ApprovalInstances",
                columns: new[] { "TenantId", "Status", "DueUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstance_ResourceId",
                schema: "__tenant__",
                table: "ApprovalInstances",
                columns: new[] { "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstance_Status",
                schema: "__tenant__",
                table: "ApprovalInstances",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_ApprovalDefinitionId",
                schema: "__tenant__",
                table: "ApprovalStages",
                column: "ApprovalDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCoverage_EmployeeGaps",
                schema: "__tenant__",
                table: "Capability_EmployeeSkillCoverageProjections",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillCoverage_RoleRanking",
                schema: "__tenant__",
                table: "Capability_EmployeeSkillCoverageProjections",
                columns: new[] { "TenantId", "RoleRequirementId", "CoveragePercent" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_Opportunities_TenantId_ManagerId",
                schema: "__tenant__",
                table: "Capability_Opportunities",
                columns: new[] { "TenantId", "ManagerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_Opportunities_TenantId_Status",
                schema: "__tenant__",
                table: "Capability_Opportunities",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_Opportunities_TenantId_Type",
                schema: "__tenant__",
                table: "Capability_Opportunities",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_OpportunityParticipants_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Capability_OpportunityParticipants",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_OpportunityParticipants_TenantId_OpportunityId_EmployeeId",
                schema: "__tenant__",
                table: "Capability_OpportunityParticipants",
                columns: new[] { "TenantId", "OpportunityId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capability_OpportunityRequirements_OpportunityId_SkillId",
                schema: "__tenant__",
                table: "Capability_OpportunityRequirements",
                columns: new[] { "OpportunityId", "SkillId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillEvidence_EmployeeId",
                schema: "__tenant__",
                table: "Capability_SkillEvidences",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillEvidence_SkillId",
                schema: "__tenant__",
                table: "Capability_SkillEvidences",
                columns: new[] { "TenantId", "SkillId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_SkillGraphRelationships_TenantId_SourceSkillId",
                schema: "__tenant__",
                table: "Capability_SkillGraphRelationships",
                columns: new[] { "TenantId", "SourceSkillId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_SkillGraphRelationships_TenantId_TargetSkillId",
                schema: "__tenant__",
                table: "Capability_SkillGraphRelationships",
                columns: new[] { "TenantId", "TargetSkillId" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_SkillNodes_TenantId_SkillCluster",
                schema: "__tenant__",
                table: "Capability_SkillNodes",
                columns: new[] { "TenantId", "SkillCluster" });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_SkillNodes_TenantId_SkillName",
                schema: "__tenant__",
                table: "Capability_SkillNodes",
                columns: new[] { "TenantId", "SkillName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectionDeadLetter_Queue",
                schema: "__tenant__",
                table: "ProjectionDeadLetters",
                columns: new[] { "TenantId", "ProjectionName", "FailedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_AggregateId_Version",
                schema: "__tenant__",
                table: "StoredEvents",
                columns: new[] { "TenantId", "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_CorrelationId",
                schema: "__tenant__",
                table: "StoredEvents",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_Position",
                schema: "__tenant__",
                table: "StoredEvents",
                column: "Position",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_TenantId_AggregateId",
                schema: "__tenant__",
                table: "StoredEvents",
                columns: new[] { "TenantId", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_TenantId_AggregateId_EffectiveUtc",
                schema: "__tenant__",
                table: "StoredEvents",
                columns: new[] { "TenantId", "AggregateId", "EffectiveUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDecisions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ApprovalDelegations",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ApprovalStages",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_EmployeeCapabilityProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_EmployeeSkillCoverageProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_OpportunityParticipants",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_OpportunityRequirements",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_SkillEvidences",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_SkillGraphRelationships",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_SkillNodes",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ProjectionCheckpoints",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ProjectionDeadLetters",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "StoredEvents",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ApprovalInstances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ApprovalDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_Opportunities",
                schema: "__tenant__");
        }
    }
}
