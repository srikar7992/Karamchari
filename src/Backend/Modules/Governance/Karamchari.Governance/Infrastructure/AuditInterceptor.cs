using Karamchari.Core.Persistence;
using Karamchari.Governance.Domain.AuditTrail;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Karamchari.Governance.Infrastructure;

public sealed class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor, IFinancialAuditInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken)
    {
        if (eventData.Context is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var httpCtx = httpContextAccessor.HttpContext;
        var actorId = Guid.TryParse(httpCtx?.User.FindFirst("employee_id")?.Value, out var g) ? g : Guid.Empty;
        var actorName = httpCtx?.User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        var correlationId = httpCtx?.TraceIdentifier;

        var entries = new List<FinancialAuditEntry>();
        foreach (var entry in eventData.Context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableFinancial
                        && e.Entity is not FinancialAuditEntry
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var op = entry.State switch {
                EntityState.Added => AuditOperation.Create,
                EntityState.Modified => AuditOperation.Update,
                _ => AuditOperation.Delete
            };

            var serOpts = new JsonSerializerOptions { WriteIndented = false };
            entries.Add(new FinancialAuditEntry
            {
                TenantId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId")?.CurrentValue as string ?? "",
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString() ?? "",
                Operation = op,
                ActorId = actorId,
                ActorName = actorName,
                BeforeJson = op != AuditOperation.Create
                    ? JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()), serOpts)
                    : null,
                AfterJson = op != AuditOperation.Delete
                    ? JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()), serOpts)
                    : null,
                CorrelationId = correlationId
            });
        }

        if (entries.Count > 0)
            eventData.Context.AddRange(entries);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
