using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Workflow.Migrations
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
                name: "Workflow_Definitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow_Definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflow_Instances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow_Instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflow_Steps",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false),
                    QuorumRule = table.Column<int>(type: "int", nullable: false),
                    QuorumThreshold = table.Column<int>(type: "int", nullable: false),
                    ApproverRolesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow_Steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflow_Steps_Workflow_Definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalSchema: "__tenant__",
                        principalTable: "Workflow_Definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workflow_AuditLogs",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflow_AuditLogs_Workflow_Instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "__tenant__",
                        principalTable: "Workflow_Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workflow_StepInstances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    ApproverRole = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow_StepInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflow_StepInstances_Workflow_Instances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "__tenant__",
                        principalTable: "Workflow_Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_AuditLogs_WorkflowInstanceId",
                schema: "__tenant__",
                table: "Workflow_AuditLogs",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_Definitions_TenantId_EntityType_IsActive",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                columns: new[] { "TenantId", "EntityType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_Instances_TenantId_EntityType_EntityId",
                schema: "__tenant__",
                table: "Workflow_Instances",
                columns: new[] { "TenantId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_Instances_TenantId_Status",
                schema: "__tenant__",
                table: "Workflow_Instances",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_StepInstances_Status_ApproverRole",
                schema: "__tenant__",
                table: "Workflow_StepInstances",
                columns: new[] { "Status", "ApproverRole" });

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_StepInstances_WorkflowInstanceId",
                schema: "__tenant__",
                table: "Workflow_StepInstances",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_Steps_DefinitionId",
                schema: "__tenant__",
                table: "Workflow_Steps",
                column: "DefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workflow_AuditLogs",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workflow_StepInstances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workflow_Steps",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workflow_Instances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workflow_Definitions",
                schema: "__tenant__");
        }
    }
}
