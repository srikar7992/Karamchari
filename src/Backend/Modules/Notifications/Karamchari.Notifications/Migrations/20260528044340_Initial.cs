using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Notifications.Migrations
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
                name: "DigestBatches",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    DigestType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WindowKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SourceMessageIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RenderedSubject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RenderedBodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RenderedBodyText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScheduleDeliverAfter = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeliveredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMessages",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreferredChannel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RenderedSubject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RenderedBodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RenderedBodyText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScheduleDeliverAfter = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeliveredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviewText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PushEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DigestEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DigestFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuietHoursStart = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    QuietHoursEnd = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryAttempts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    AttemptedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveryAttempts_NotificationMessages_NotificationMessageId",
                        column: x => x.NotificationMessageId,
                        principalSchema: "__tenant__",
                        principalTable: "NotificationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DigestBatches_Status_ScheduledAt",
                schema: "__tenant__",
                table: "DigestBatches",
                columns: new[] { "TenantId", "Status", "ScheduleDeliverAfter" });

            migrationBuilder.CreateIndex(
                name: "UIX_DigestBatches_IdempotencyKey",
                schema: "__tenant__",
                table: "DigestBatches",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryAttempts_Message_Attempt",
                schema: "__tenant__",
                table: "NotificationDeliveryAttempts",
                columns: new[] { "NotificationMessageId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_Recipient_Read",
                schema: "__tenant__",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "RecipientEmployeeId", "IsRead", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_Status_ScheduledAt",
                schema: "__tenant__",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "Status", "ScheduleDeliverAfter" });

            migrationBuilder.CreateIndex(
                name: "UIX_NotificationMessages_IdempotencyKey",
                schema: "__tenant__",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_NotificationTemplates_Code_Channel_Locale",
                schema: "__tenant__",
                table: "NotificationTemplates",
                columns: new[] { "TenantId", "Code", "Channel", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_UserNotificationPreferences_Employee_Category",
                schema: "__tenant__",
                table: "UserNotificationPreferences",
                columns: new[] { "TenantId", "EmployeeId", "Category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigestBatches",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "NotificationDeliveryAttempts",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "NotificationTemplates",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "NotificationMessages",
                schema: "__tenant__");
        }
    }
}
