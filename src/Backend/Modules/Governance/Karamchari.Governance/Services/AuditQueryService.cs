using Karamchari.Governance.Domain.AuditTrail;
using Karamchari.Governance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Governance.Services;

public sealed class AuditQueryService(GovernanceDbContext db)
{
    public async Task<IReadOnlyList<FinancialAuditEntry>> QueryAsync(
        string tenantId, string? entityType, string? entityId,
        Guid? actorId, DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken ct)
    {
        var q = db.FinancialAuditEntries.AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (entityType != null) q = q.Where(e => e.EntityType == entityType);
        if (entityId != null) q = q.Where(e => e.EntityId == entityId);
        if (actorId.HasValue) q = q.Where(e => e.ActorId == actorId.Value);
        if (from.HasValue) q = q.Where(e => e.TimestampUtc >= from.Value);
        if (to.HasValue) q = q.Where(e => e.TimestampUtc <= to.Value);

        return await q.OrderByDescending(e => e.TimestampUtc).Take(500).ToListAsync(ct);
    }
}
