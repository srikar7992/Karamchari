using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Recruitment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "Recruitment_Applications",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CandidateSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HiredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HiredBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_AuditStream",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_AuditStream", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_Candidates",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProfileVersion = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_Candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_InterviewFeedback",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_InterviewFeedback", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_Interviews",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InterviewerIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_Interviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_Offers",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_Offers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment_Requisitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HiringManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetHireDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_Requisitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_Applications_TenantId",
                schema: "__tenant__",
                table: "Recruitment_Applications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_Applications_TenantId_CandidateId_RequisitionId",
                schema: "__tenant__",
                table: "Recruitment_Applications",
                columns: new[] { "TenantId", "CandidateId", "RequisitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_AuditStream_EntityId",
                schema: "__tenant__",
                table: "Recruitment_AuditStream",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_AuditStream_TenantId",
                schema: "__tenant__",
                table: "Recruitment_AuditStream",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_Candidates_TenantId",
                schema: "__tenant__",
                table: "Recruitment_Candidates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_InterviewFeedback_TenantId",
                schema: "__tenant__",
                table: "Recruitment_InterviewFeedback",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_Interviews_TenantId",
                schema: "__tenant__",
                table: "Recruitment_Interviews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_Offers_TenantId",
                schema: "__tenant__",
                table: "Recruitment_Offers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_Requisitions_TenantId",
                schema: "__tenant__",
                table: "Recruitment_Requisitions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recruitment_Applications",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_AuditStream",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_Candidates",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_InterviewFeedback",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_Interviews",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_Offers",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Recruitment_Requisitions",
                schema: "__tenant__");
        }
    }
}
