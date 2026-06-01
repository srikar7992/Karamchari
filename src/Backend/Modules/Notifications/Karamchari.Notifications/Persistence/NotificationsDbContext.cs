using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Notifications.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class NotificationsDbContext : KaramchariDbContext
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<DigestBatch> DigestBatches => Set<DigestBatch>();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new NotificationTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new UserNotificationPreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DigestBatchConfiguration());
    }
}
