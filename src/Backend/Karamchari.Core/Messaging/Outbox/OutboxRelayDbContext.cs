using Microsoft.EntityFrameworkCore;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Minimal DbContext for outbox relay infrastructure only.
/// Does NOT inherit KaramchariDbContext — these tables are shared infrastructure,
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
    }
}
