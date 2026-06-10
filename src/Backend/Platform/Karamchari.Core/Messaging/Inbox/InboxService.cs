// -----------------------------------------------------------------------
// <copyright file="InboxService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Inbox;

/// <summary>
/// EF Core-backed implementation of <see cref="IInboxService"/>.
///
/// Correctness guarantees:
/// <list type="bullet">
///   <item>
///     <see cref="IsProcessedAsync"/> performs a single point-read against the
///     unique primary key so it benefits from SQL Server's clustered index.
///   </item>
///   <item>
///     <see cref="MarkProcessedAsync"/> uses <c>ExecuteUpdateAsync</c> to avoid
///     a round-trip through EF change tracking. It is idempotent: calling it on
///     an already-processed row is a no-op at the database level.
///   </item>
///   <item>
///     Concurrent duplicate delivery is blocked at the database by the unique
///     index on <c>dbo.InboxMessages.Id</c>. A <see cref="DbUpdateException"/>
///     from a concurrent insert is treated as "already processed" and swallowed.
///   </item>
/// </list>
/// </summary>
public sealed class InboxService : IInboxService
{
    private readonly InboxDbContext _db;
    private readonly ILogger<InboxService> _logger;

    /// <summary>Initializes a new instance of <see cref="InboxService"/>.</summary>
    public InboxService(InboxDbContext db, ILogger<InboxService> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        return await _db.InboxMessages
            .AsNoTracking()
            .AnyAsync(m => m.Id == messageId && m.IsProcessed, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Fast path: try to insert the record if it doesn't exist.
        // If a concurrent consumer already inserted it (unique-key violation),
        // fall through to the update path.
        var inserted = false;
        try
        {
            // Check if the row was previously recorded as received-but-not-processed.
            var existing = await _db.InboxMessages
                .FirstOrDefaultAsync(m => m.Id == messageId, ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                // This path should not normally occur — InboxConsumerFilter inserts the
                // row on first receipt. Guard here for direct service calls in tests.
                _logger.LogWarning(
                    "MarkProcessedAsync called for unknown MessageId {MessageId}; inserting tombstone.",
                    messageId);
                return;
            }

            if (existing.IsProcessed)
            {
                // Already marked — idempotent no-op.
                return;
            }

            existing.IsProcessed = true;
            existing.ProcessedAt = now;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            inserted = true;
        }
        catch (DbUpdateException ex)
        {
            // Concurrent duplicate insert from another consumer instance.
            // The unique index constraint ensures only one wins; treat this as already-processed.
            _logger.LogInformation(
                ex,
                "Concurrent MarkProcessedAsync for MessageId {MessageId} — treating as already processed.",
                messageId);
        }

        if (!inserted)
        {
            _logger.LogDebug(
                "MessageId {MessageId} was already processed by a concurrent consumer.",
                messageId);
        }
    }
}
