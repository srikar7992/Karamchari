using System.Text.Json;
using Karamchari.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Notifications.Persistence.Configurations;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> b)
    {
        b.ToTable("NotificationTemplates");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.Code).IsRequired().HasMaxLength(100);
        b.Property(x => x.Category).IsRequired().HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Channel).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Locale).IsRequired().HasMaxLength(10);
        b.Property(x => x.Subject).IsRequired().HasMaxLength(500);
        b.Property(x => x.BodyHtml).IsRequired();
        b.Property(x => x.BodyText).IsRequired();
        b.Property(x => x.PreviewText).HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();

        // One template per code + channel + locale per tenant
        b.HasIndex(x => new { x.TenantId, x.Code, x.Channel, x.Locale })
            .IsUnique()
            .HasDatabaseName("UIX_NotificationTemplates_Code_Channel_Locale");
    }
}

internal sealed class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> b)
    {
        b.ToTable("UserNotificationPreferences");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.EmployeeId).IsRequired();
        b.Property(x => x.Category).IsRequired().HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.DigestFrequency).IsRequired().HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.QuietHoursStart).HasMaxLength(5);
        b.Property(x => x.QuietHoursEnd).HasMaxLength(5);
        b.Property(x => x.Timezone).IsRequired().HasMaxLength(50);

        // One preference row per employee per category
        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Category })
            .IsUnique()
            .HasDatabaseName("UIX_UserNotificationPreferences_Employee_Category");
    }
}

internal sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> b)
    {
        b.ToTable("NotificationMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.RecipientEmployeeId).IsRequired();
        b.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(320);
        b.Property(x => x.Category).IsRequired().HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.PreferredChannel).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.RenderedSubject).IsRequired().HasMaxLength(500);
        b.Property(x => x.RenderedBodyHtml).IsRequired();
        b.Property(x => x.RenderedBodyText).IsRequired();
        b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CreatedOnUtc).IsRequired();

        // Idempotency: duplicate events must not create second message
        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UIX_NotificationMessages_IdempotencyKey");

        // Inbox query: unread in-app notifications for a recipient
        b.HasIndex(x => new { x.TenantId, x.RecipientEmployeeId, x.IsRead, x.CreatedOnUtc })
            .HasDatabaseName("IX_NotificationMessages_Recipient_Read");

        // Delivery queue: pending messages scheduled for delivery
        b.HasIndex(x => new { x.TenantId, x.Status, x.ScheduleDeliverAfter })
            .HasDatabaseName("IX_NotificationMessages_Status_ScheduledAt");

        b.OwnsMany(x => x.Attempts, a =>
        {
            a.ToTable("NotificationDeliveryAttempts");
            a.HasKey(x => x.Id);
            a.Property(x => x.NotificationMessageId).IsRequired();
            a.Property(x => x.Channel).IsRequired().HasConversion<string>().HasMaxLength(30);
            a.Property(x => x.Result).IsRequired().HasConversion<string>().HasMaxLength(30);
            a.Property(x => x.ProviderMessageId).HasMaxLength(200);
            a.Property(x => x.FailureReason).HasMaxLength(500);
            a.Property(x => x.AttemptedOnUtc).IsRequired();
            a.HasIndex(x => new { x.NotificationMessageId, x.AttemptNumber })
                .HasDatabaseName("IX_NotificationDeliveryAttempts_Message_Attempt");
        });
    }
}

internal sealed class DigestBatchConfiguration : IEntityTypeConfiguration<DigestBatch>
{
    public void Configure(EntityTypeBuilder<DigestBatch> b)
    {
        b.ToTable("DigestBatches");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.RecipientEmployeeId).IsRequired();
        b.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(320);
        b.Property(x => x.DigestType).IsRequired().HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.WindowKey).IsRequired().HasMaxLength(20);
        b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(300);
        b.Property(x => x.RenderedSubject).IsRequired().HasMaxLength(500);
        b.Property(x => x.RenderedBodyHtml).IsRequired();
        b.Property(x => x.RenderedBodyText).IsRequired();
        b.Property(x => x.MessageCount).IsRequired();
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CreatedOnUtc).IsRequired();

        // SourceMessageIds stored as JSON column (list of GUIDs, read-only after creation).
        b.Property(x => x.SourceMessageIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => (IReadOnlyList<Guid>)(JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)
                         ?? new List<Guid>()))
            .HasColumnType("nvarchar(max)");

        // One batch per recipient per digest type per window per tenant.
        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UIX_DigestBatches_IdempotencyKey");

        // Delivery worker query: pending batches ready to send.
        b.HasIndex(x => new { x.TenantId, x.Status, x.ScheduleDeliverAfter })
            .HasDatabaseName("IX_DigestBatches_Status_ScheduledAt");
    }
}
