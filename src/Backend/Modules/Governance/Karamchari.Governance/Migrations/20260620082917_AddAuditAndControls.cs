using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Governance.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "financial_audit_entries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Governance_SodMatrix",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governance_SodMatrix", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Governance_SodViolations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role1 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Role2 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governance_SodViolations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Governance_SodRules",
                schema: "__tenant__",
                columns: table => new
                {
                    Role1 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Role2 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SodMatrixId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governance_SodRules", x => new { x.SodMatrixId, x.Role1, x.Role2 });
                    table.ForeignKey(
                        name: "FK_Governance_SodRules_Governance_SodMatrix_SodMatrixId",
                        column: x => x.SodMatrixId,
                        principalSchema: "__tenant__",
                        principalTable: "Governance_SodMatrix",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_audit_entries_TenantId_ActorId_TimestampUtc",
                schema: "audit",
                table: "financial_audit_entries",
                columns: new[] { "TenantId", "ActorId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_audit_entries_TenantId_EntityType_EntityId",
                schema: "audit",
                table: "financial_audit_entries",
                columns: new[] { "TenantId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Governance_SodMatrix_TenantId",
                schema: "__tenant__",
                table: "Governance_SodMatrix",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Governance_SodViolations_TenantId_EmployeeId_DetectedAt",
                schema: "__tenant__",
                table: "Governance_SodViolations",
                columns: new[] { "TenantId", "EmployeeId", "DetectedAt" });

            migrationBuilder.EnsureSchema("audit");
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION audit.prevent_audit_modification()
RETURNS TRIGGER AS $$ BEGIN RAISE EXCEPTION ''Audit entries are immutable''; END; $$ LANGUAGE plpgsql;
CREATE TRIGGER trg_audit_immutable BEFORE UPDATE OR DELETE ON audit.financial_audit_entries
FOR EACH ROW EXECUTE FUNCTION audit.prevent_audit_modification();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_audit_entries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "Governance_SodRules",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Governance_SodViolations",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Governance_SodMatrix",
                schema: "__tenant__");
        }
    }
}
