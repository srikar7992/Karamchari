using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Recruitment.Migrations
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
                name: "Recruitment_CandidateApplications",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferrerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_CandidateApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_CandidateProfiles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalEmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastInteractionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsAnonymized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_CandidateProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_InterviewSessions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeetingLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_InterviewSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_OfferLetters",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquityGrant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SignOnBonus = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_OfferLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_PipelineDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_PipelineDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceDemandSignal",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceDemandSignal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_InterviewFeedback",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScorecardJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallScore = table.Column<int>(type: "int", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_InterviewFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recruitment_InterviewFeedback_Recruitment_InterviewSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "__tenant__",
                        principalTable: "Recruitment_InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_PipelineStages",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RequiresInterview = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_PipelineStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recruitment_PipelineStages_Recruitment_PipelineDefinitions_PipelineId",
                        column: x => x.PipelineId,
                        principalSchema: "__tenant__",
                        principalTable: "Recruitment_PipelineDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_JobRequisitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeadcountTarget = table.Column<int>(type: "int", nullable: false),
                    Budget_Min = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Budget_Max = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Budget_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    HiringManagerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DemandSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_JobRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recruitment_JobRequisitions_WorkforceDemandSignal_DemandSignalId",
                        column: x => x.DemandSignalId,
                        principalSchema: "__tenant__",
                        principalTable: "WorkforceDemandSignal",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_CandidateApplications_TenantId_RequisitionId_CandidateId",
                schema: "__tenant__",
                table: "Recruitment_CandidateApplications",
                columns: new[] { "TenantId", "RequisitionId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_CandidateProfiles_TenantId_Email",
                schema: "__tenant__",
                table: "Recruitment_CandidateProfiles",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_InterviewFeedback_SessionId_InterviewerId",
                schema: "__tenant__",
                table: "Recruitment_InterviewFeedback",
                columns: new[] { "SessionId", "InterviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_JobRequisitions_DemandSignalId",
                schema: "__tenant__",
                table: "Recruitment_JobRequisitions",
                column: "DemandSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_JobRequisitions_TenantId_Status",
                schema: "__tenant__",
                table: "Recruitment_JobRequisitions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_PipelineDefinitions_TenantId_Name",
                schema: "__tenant__",
                table: "Recruitment_PipelineDefinitions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_PipelineStages_PipelineId",
                schema: "__tenant__",
                table: "Recruitment_PipelineStages",
                column: "PipelineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recruitment_CandidateApplications",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_CandidateProfiles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_InterviewFeedback",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_JobRequisitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_OfferLetters",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_PipelineStages",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_InterviewSessions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "WorkforceDemandSignal",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_PipelineDefinitions",
                schema: "__tenant__");
        }
    }
}
