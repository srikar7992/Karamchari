using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Succession.Migrations
{
    /// <inheritdoc />
    public partial class Phase21SuccessionIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IncumbentAttritionRisk",
                schema: "__tenant__",
                table: "Succession_CriticalRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IncumbentRetirementRisk",
                schema: "__tenant__",
                table: "Succession_CriticalRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RequiredSuccessors",
                schema: "__tenant__",
                table: "Succession_CriticalRoles",
                type: "int",
                nullable: false,
                defaultValue: 1);

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
                name: "Succession_CoverageProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CurrentOccupantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsKeyPosition = table.Column<bool>(type: "bit", nullable: false),
                    SuccessorCount = table.Column<int>(type: "int", nullable: false),
                    ReadyNowCount = table.Column<int>(type: "int", nullable: false),
                    ReadyNearTermCount = table.Column<int>(type: "int", nullable: false),
                    VacancyRisk = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_CoverageProjections", x => new { x.TenantId, x.PositionId });
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
                name: "ProjectionCheckpoints",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ProjectionDeadLetters",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "StoredEvents",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_CoverageProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ApprovalInstances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ApprovalDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropColumn(
                name: "IncumbentAttritionRisk",
                schema: "__tenant__",
                table: "Succession_CriticalRoles");

            migrationBuilder.DropColumn(
                name: "IncumbentRetirementRisk",
                schema: "__tenant__",
                table: "Succession_CriticalRoles");

            migrationBuilder.DropColumn(
                name: "RequiredSuccessors",
                schema: "__tenant__",
                table: "Succession_CriticalRoles");
        }
    }
}
