// -----------------------------------------------------------------------
// <copyright file="LeaveBalanceProjectionRebuilder.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Projections;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Rebuilds the LeaveBalanceReadModel from the authoritative LeaveBalance ledger.
/// Implements Priority 1 Step 4.
/// </summary>
public sealed class LeaveBalanceProjectionRebuilder(
    TimeAttendanceDbContext db,
    ILogger<LeaveBalanceProjectionRebuilder> logger) : IProjectionRebuilder
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly ILogger<LeaveBalanceProjectionRebuilder> _logger = logger;

    public ProjectionMetadata Metadata => new()
    {
        ProjectionName = "LeaveBalances",
        OwningCapability = "TimeAttendance",
        ConsistencyType = ProjectionConsistencyType.Eventual,
        RebuildStrategy = ProjectionRebuildStrategy.FullReplay,
        Category = ProjectionCategory.NearRealTime,
        RefreshTarget = TimeSpan.FromMinutes(5)
    };

    public async Task RebuildAsync(
        string tenantId,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding projection {ProjectionName} for tenant {TenantId}", Metadata.ProjectionName, tenantId);

        // 1. Get all active balances for the tenant
        List<LeaveBalance> balances = await _db.LeaveBalances
            .Include(b => b.Entries)
            .Where(b => b.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (LeaveBalance? balance in balances)
        {
            LeavePolicy? policy = await _db.LeavePolicies.FindAsync([balance.PolicyId], ct);
            string policyName = policy?.Name ?? "Unknown";

            LeaveBalanceReadModel? readModel = await _db.LeaveBalanceReadModels
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.EmployeeId == balance.EmployeeId && m.PolicyId == balance.PolicyId, ct);

            if (readModel == null)
            {
                readModel = new LeaveBalanceReadModel
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EmployeeId = balance.EmployeeId,
                    PolicyId = balance.PolicyId,
                    PolicyName = policyName
                };
                _db.LeaveBalanceReadModels.Add(readModel);
            }

            // 2. Re-calculate from ledger entries
            IReadOnlyCollection<LeaveBalanceEntry> entries = balance.Entries;
            if (startTime.HasValue) entries = [.. entries.Where(e => e.CreatedAt >= startTime.Value)];
            if (endTime.HasValue) entries = [.. entries.Where(e => e.CreatedAt <= endTime.Value)];

            readModel.AvailableBalance = entries.Sum(e => e.Quantity);
            readModel.ConsumedBalance = Math.Abs(entries.Where(e => e.Quantity < 0).Sum(e => e.Quantity));
            readModel.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
            readModel.Version = Metadata.Version;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Finished rebuilding projection {ProjectionName} for tenant {TenantId}", Metadata.ProjectionName, tenantId);
    }
}
