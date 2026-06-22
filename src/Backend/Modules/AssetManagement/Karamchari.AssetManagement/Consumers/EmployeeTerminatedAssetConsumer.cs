// -----------------------------------------------------------------------
// <copyright file="EmployeeTerminatedAssetConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.AssetManagement.Domain;
using Karamchari.AssetManagement.Persistence;
using Karamchari.Core.Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.AssetManagement.Consumers;

/// <summary>
/// When an employee is terminated, auto-return all assets assigned to them.
/// Records the return at the termination date with the asset's current condition.
/// This closes the onboarding↔offboarding asset lifecycle loop.
/// </summary>
public sealed class EmployeeTerminatedAssetConsumer(AssetManagementDbContext db)
    : IConsumer<EmployeeTerminatedIntegrationEventV1>
{
    public async Task Consume(ConsumeContext<EmployeeTerminatedIntegrationEventV1> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        // Load all assets assigned to this employee (tenant-scoped by RLS).
        var assignedAssets = await db.Assets
            .Where(a => a.TenantId == ev.TenantId && a.Status == AssetStatus.Assigned)
            .ToListAsync(ct);

        var terminatedAssets = assignedAssets
            .Where(a => a.CurrentAssignment?.EmployeeId == ev.EmployeeId)
            .ToList();

        if (terminatedAssets.Count == 0) return;

        foreach (var asset in terminatedAssets)
        {
            asset.RecordReturn(
                ev.TerminatedOn,
                asset.Condition,
                $"Auto-returned on employee termination. Reason: {ev.TerminationReason}");
        }

        await db.SaveChangesAsync(ct);
    }
}
