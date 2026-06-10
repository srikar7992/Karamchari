// -----------------------------------------------------------------------
// <copyright file="InboxDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace Karamchari.Core.Messaging.Inbox;

/// <summary>
/// Minimal DbContext for consumer-side inbox deduplication infrastructure.
///
/// Does NOT inherit <c>KaramchariDbContext</c> — inbox records are shared
/// infrastructure, not tenant-owned, so no schema rewriting or RLS interceptors apply.
///
/// A unique index on <see cref="InboxMessage.Id"/> (the broker MessageId) provides
/// a database-level guard against concurrent duplicate inserts.
/// </summary>
public sealed class InboxDbContext : DbContext
{
    /// <summary>Initializes a new instance of <see cref="InboxDbContext"/>.</summary>
    public InboxDbContext(DbContextOptions<InboxDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the inbox message deduplication store.</summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("InboxMessages", "dbo");
            b.HasKey(x => x.Id);

            // Id is the broker-assigned MessageId — caller provides it; no DB generation.
            b.Property(x => x.Id).ValueGeneratedNever();

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.MessageType).IsRequired().HasMaxLength(500);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.ReceivedAt).IsRequired();
            b.Property(x => x.ProcessedAt);
            b.Property(x => x.IsProcessed).IsRequired().HasDefaultValue(false);

            // Fast lookup by MessageId for IsProcessedAsync hot path.
            // PK already provides this; the index here is for explicit documentation.
            b.HasIndex(x => x.Id).IsUnique();

            // Supports cleanup of old processed records by age.
            b.HasIndex(x => x.ReceivedAt);

            // Supports retry-monitor queries: WHERE IsProcessed = 0.
            b.HasIndex(x => x.IsProcessed);
        });
    }
}
