using Microsoft.EntityFrameworkCore;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Minimal DbContext for outbox relay infrastructure only.
/// Does NOT inherit KaramchariDbContext â€” these tables are shared infrastructure,
/// not tenant-owned, so no schema rewriting or RLS interceptors apply.
/// </summary>
public sealed class OutboxRelayDbContext : DbContext
{
    /// <summary>Initializes a new instance of <see cref="OutboxRelayDbContext"/>.</summary>
    public OutboxRelayDbContext(DbContextOptions<OutboxRelayDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the dead-letter message store.</summary>
    public DbSet<OutboxDeadLetterMessage> DeadLetterMessages => Set<OutboxDeadLetterMessage>();

    /// <summary>Gets the relay-owned processing state store.</summary>
    public DbSet<OutboxProcessingState> ProcessingStates => Set<OutboxProcessingState>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxDeadLetterMessage>(b =>
        {
            b.ToTable("OutboxDeadLetter", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).UseIdentityColumn();
            b.Property(x => x.MessageId).IsRequired();
            b.Property(x => x.OutboxId).IsRequired();
            b.Property(x => x.MessageType).IsRequired().HasMaxLength(500);
            b.Property(x => x.Body).IsRequired();
            b.Property(x => x.DestinationAddress).HasMaxLength(500);
            b.Property(x => x.FailureReason).HasMaxLength(2000);
            b.Property(x => x.RetryCount).IsRequired();
            b.Property(x => x.FailedAt).IsRequired();

            b.HasIndex(x => x.MessageId);
            b.HasIndex(x => x.FailedAt);
        });

        modelBuilder.Entity<OutboxProcessingState>(b =>
        {
            b.ToTable("OutboxProcessingState", "dbo");
            b.HasKey(x => x.OutboxId);
            b.Property(x => x.OutboxId).IsRequired().ValueGeneratedNever();
            b.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
            b.Property(x => x.LastAttemptUtc).HasColumnType("datetime2");
            b.Property(x => x.NextRetryUtc).HasColumnType("datetime2");
            b.Property(x => x.LastError).HasMaxLength(2000);
            b.Property(x => x.LockedByInstanceId);
            b.Property(x => x.LockAcquiredUtc).HasColumnType("datetime2");

            // Supports stale-lock cleanup: WHERE LockAcquiredUtc < threshold AND LockedByInstanceId IS NOT NULL
            b.HasIndex(x => x.LockAcquiredUtc);
            // Supports retry eligibility scan: WHERE NextRetryUtc <= NOW() AND RetryCount > 0
            b.HasIndex(x => x.NextRetryUtc);
        });
    }
}
