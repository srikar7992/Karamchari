using Karamchari.Governance.Domain.Controls;
using Karamchari.Governance.Domain.Events;
using Karamchari.Governance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Governance.Services;

public sealed class SodEnforcementService(GovernanceDbContext db, IPublishEndpoint publish)
{
    public async Task<bool> ValidateRoleAssignmentAsync(
        string tenantId, Guid employeeId,
        string roleBeingAssigned, IEnumerable<string> currentRoles,
        CancellationToken ct)
    {
        var matrix = await db.SodMatrices.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId, ct);

        if (matrix is null) return true;

        var existing = currentRoles.ToList();
        if (!matrix.Conflicts(roleBeingAssigned, existing)) return true;

        var conflictingRole = existing.FirstOrDefault(r => matrix.Conflicts(roleBeingAssigned, [r])) ?? "";
        db.SodViolations.Add(new SodViolation
        {
            TenantId = tenantId, EmployeeId = employeeId,
            Role1 = roleBeingAssigned, Role2 = conflictingRole
        });
        await publish.Publish(new SodConflictDetected(
            employeeId, tenantId, roleBeingAssigned, conflictingRole, DateTimeOffset.UtcNow), ct);
        await db.SaveChangesAsync(ct);
        return false;
    }
}
