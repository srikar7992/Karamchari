// -----------------------------------------------------------------------
// <copyright file="EmployeeLeaveSummary.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Persistence;

using Karamchari.Core.Multitenancy;

/// <summary>
/// CQRS read model: denormalized summary of all leave balances for one employee.
/// Updated by consumers of LeaveAccrued, LeaveApproved, LeaveCancelled, BalanceChanged.
/// Dashboards query this — never hit the ledger for display.
/// </summary>
public sealed class EmployeeLeaveSummary : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public required string LeaveYearName { get; init; }
    public Guid LeaveYearId { get; init; }

    /// <summary>JSON: { policyId → { earned, consumed, pending, available, carryForward } }</summary>
    public string BalancesJson { get; set; } = "{}";

    public int TotalApprovedRequests { get; set; }
    public int TotalPendingRequests { get; set; }
    public decimal TotalDaysOnLeaveYtd { get; set; }
    public decimal TotalLopDaysYtd { get; set; }

    public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Version { get; set; } = "1.0";
}
