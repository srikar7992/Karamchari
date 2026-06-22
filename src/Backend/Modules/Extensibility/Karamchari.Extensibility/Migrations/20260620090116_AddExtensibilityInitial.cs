using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Extensibility.Migrations
{
    /// <inheritdoc />
    public partial class AddExtensibilityInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "CustomEntityDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomEntityDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomEntityRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomEntityRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomEntityDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldDefinitions_CustomEntityDefinitions_CustomEntityDefinitionId",
                        column: x => x.CustomEntityDefinitionId,
                        principalSchema: "__tenant__",
                        principalTable: "CustomEntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UIX_CustomEntityDefs_Tenant_Name",
                schema: "__tenant__",
                table: "CustomEntityDefinitions",
                columns: new[] { "TenantId", "EntityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomEntityRecords_Tenant_Def",
                schema: "__tenant__",
                table: "CustomEntityRecords",
                columns: new[] { "TenantId", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomEntityRecords_Tenant_Owner",
                schema: "__tenant__",
                table: "CustomEntityRecords",
                columns: new[] { "TenantId", "OwnerEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_CustomEntityDefinitionId",
                schema: "__tenant__",
                table: "CustomFieldDefinitions",
                column: "CustomEntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "UIX_CustomFieldDefs_Def_Name",
                schema: "__tenant__",
                table: "CustomFieldDefinitions",
                columns: new[] { "DefinitionId", "FieldName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomEntityRecords",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CustomEntityDefinitions",
                schema: "__tenant__");
        }
    }
}
