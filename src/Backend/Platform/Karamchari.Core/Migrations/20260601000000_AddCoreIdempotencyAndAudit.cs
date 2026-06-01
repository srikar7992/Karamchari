using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Core.Migrations;

/// <inheritdoc />
public partial class AddCoreIdempotencyAndAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.CreateTable(
            name: "IdempotentRequests",
            schema: "dbo",
            columns: table => new
            {
                IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                StatusCode = table.Column<int>(type: "int", nullable: false),
                ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotentRequests", x => new { x.IdempotencyKey, x.TenantId });
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdempotentRequests_ExpiresAtUtc",
            schema: "dbo",
            table: "IdempotentRequests",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                TableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PrimaryKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TenantId",
            schema: "dbo",
            table: "AuditLogs",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TimestampUtc",
            schema: "dbo",
            table: "AuditLogs",
            column: "TimestampUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(name: "IdempotentRequests", schema: "dbo");
        migrationBuilder.DropTable(name: "AuditLogs", schema: "dbo");
    }
}
