using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Recruitment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recruitment_AnalyticsReadModels",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment_AnalyticsReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_AnalyticsReadModels_TenantId",
                schema: "__tenant__",
                table: "Recruitment_AnalyticsReadModels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_AnalyticsReadModels_EventType_RequisitionId_CandidateId",
                schema: "__tenant__",
                table: "Recruitment_AnalyticsReadModels",
                columns: new[] { "EventType", "RequisitionId", "CandidateId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recruitment_AnalyticsReadModels",
                schema: "__tenant__");
        }
    }
}
