using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Core.Migrations;

/// <inheritdoc />
public partial class AddOutboxInfrastructure : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.EnsureSchema(
            name: "dbo");

        migrationBuilder.CreateTable(
            name: "OutboxDeadLetter",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Headers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DestinationAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                RetryCount = table.Column<int>(type: "int", nullable: false),
                FailedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxDeadLetter", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OutboxProcessingState",
            schema: "dbo",
            columns: table => new
            {
                OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LastAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                NextRetryUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                LockedByInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LockAcquiredUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxProcessingState", x => x.OutboxId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxDeadLetter_FailedAt",
            schema: "dbo",
            table: "OutboxDeadLetter",
            column: "FailedAt");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxDeadLetter_MessageId",
            schema: "dbo",
            table: "OutboxDeadLetter",
            column: "MessageId");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxProcessingState_LockAcquiredUtc",
            schema: "dbo",
            table: "OutboxProcessingState",
            column: "LockAcquiredUtc");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxProcessingState_NextRetryUtc",
            schema: "dbo",
            table: "OutboxProcessingState",
            column: "NextRetryUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(
            name: "OutboxDeadLetter",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "OutboxProcessingState",
            schema: "dbo");
    }
}
