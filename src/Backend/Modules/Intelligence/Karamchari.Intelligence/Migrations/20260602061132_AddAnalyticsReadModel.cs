using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Intelligence_AnalyticsReadModels",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MetricType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intelligence_AnalyticsReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intelligence_AnalyticsReadModels_TenantId_MetricType_EntityId_Stage",
                schema: "__tenant__",
                table: "Intelligence_AnalyticsReadModels",
                columns: new[] { "TenantId", "MetricType", "EntityId", "Stage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Intelligence_AnalyticsReadModels",
                schema: "__tenant__");
        }
    }
}
