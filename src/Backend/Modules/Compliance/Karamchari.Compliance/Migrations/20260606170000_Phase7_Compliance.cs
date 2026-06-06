using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Compliance.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_Compliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Compliance_RetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetentionPeriodDays = table.Column<int>(type: "int", nullable: false),
                    ArchiveAfterDays = table.Column<int>(type: "int", nullable: false),
                    DeleteAfterDays = table.Column<int>(type: "int", nullable: false),
                    AnonymizeAfterDays = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_RetentionPolicies", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_RetentionPolicies_TenantId_RecordType",
                table: "Compliance_RetentionPolicies",
                columns: new[] { "TenantId", "RecordType" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "Compliance_RetentionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_RetentionActions", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_RetentionActions_TenantId_Status",
                table: "Compliance_RetentionActions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateTable(
                name: "Compliance_LegalHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CaseReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_LegalHolds", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_LegalHolds_TenantId_Status",
                table: "Compliance_LegalHolds",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateTable(
                name: "Compliance_LegalHoldScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalHoldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ScopeValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compliance_LegalHoldScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compliance_LegalHoldScopes_LegalHolds_LegalHoldId",
                        column: x => x.LegalHoldId,
                        principalTable: "Compliance_LegalHolds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Compliance_RegulatoryRegisters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegulationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_RegulatoryRegisters", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_RegulatoryRegisters_TenantId_Country_RegulationCode",
                table: "Compliance_RegulatoryRegisters",
                columns: new[] { "TenantId", "Country", "RegulationCode" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "Compliance_RegulatoryRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ViolationThreshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_RegulatoryRules", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_RegulatoryRules_RegisterId_RuleCode",
                table: "Compliance_RegulatoryRules",
                columns: new[] { "RegisterId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "Compliance_PolicyViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PolicyCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_PolicyViolations", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_PolicyViolations_TenantId_EmployeeId_Status",
                table: "Compliance_PolicyViolations",
                columns: new[] { "TenantId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_PolicyViolations_TenantId_Status_DetectedAt",
                table: "Compliance_PolicyViolations",
                columns: new[] { "TenantId", "Status", "DetectedAt" });

            migrationBuilder.CreateTable(
                name: "Compliance_ComplianceScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ViolationPenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OvertimePenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AuditFindingPenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PolicyExceptionPenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OpenViolationCount = table.Column<int>(type: "int", nullable: false),
                    CriticalViolationCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_ComplianceScores", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_ComplianceScores_TenantId_ScopeKey",
                table: "Compliance_ComplianceScores",
                columns: new[] { "TenantId", "ScopeKey" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "Compliance_ComplianceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SnapshotType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_ComplianceSnapshots", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Compliance_AuditPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PackageUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Compliance_AuditPackages", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_AuditPackages_TenantId_Status",
                table: "Compliance_AuditPackages",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Compliance_AuditPackages");
            migrationBuilder.DropTable(name: "Compliance_ComplianceSnapshots");
            migrationBuilder.DropTable(name: "Compliance_ComplianceScores");
            migrationBuilder.DropTable(name: "Compliance_PolicyViolations");
            migrationBuilder.DropTable(name: "Compliance_RegulatoryRules");
            migrationBuilder.DropTable(name: "Compliance_RegulatoryRegisters");
            migrationBuilder.DropTable(name: "Compliance_LegalHoldScopes");
            migrationBuilder.DropTable(name: "Compliance_LegalHolds");
            migrationBuilder.DropTable(name: "Compliance_RetentionActions");
            migrationBuilder.DropTable(name: "Compliance_RetentionPolicies");
        }
    }
}
