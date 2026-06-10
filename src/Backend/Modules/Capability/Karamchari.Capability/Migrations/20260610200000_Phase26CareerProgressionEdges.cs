using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations;

/// <inheritdoc />
public partial class Phase26CareerProgressionEdges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Capability_CareerProgressionEdges",
            columns: table => new
            {
                TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                FromRoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ToRoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Weight = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Capability_CareerProgressionEdges", x => new { x.TenantId, x.FromRoleRequirementId, x.ToRoleRequirementId });
            });

        migrationBuilder.CreateIndex(
            name: "IX_CareerPath_FromRole",
            table: "Capability_CareerProgressionEdges",
            columns: new[] { "TenantId", "FromRoleRequirementId" });

        migrationBuilder.CreateIndex(
            name: "IX_CareerPath_ToRole",
            table: "Capability_CareerProgressionEdges",
            columns: new[] { "TenantId", "ToRoleRequirementId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Capability_CareerProgressionEdges");
    }
}
