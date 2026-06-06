using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Compliance.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_1_ScoreHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Compliance_ScoreHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenViolationCount = table.Column<int>(type: "int", nullable: false),
                    CriticalViolationCount = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compliance_ScoreHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_ScoreHistory_TenantId_ScopeKey_RecordedAt",
                table: "Compliance_ScoreHistory",
                columns: new[] { "TenantId", "ScopeKey", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Compliance_ScoreHistory");
        }
    }
}
