// -----------------------------------------------------------------------
// <copyright file="PayrollAuditEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Audit;

/// <summary>
/// Immutable audit trail entry for every significant payroll action.
/// Never delete, never update. Append-only.
/// </summary>
public sealed class PayrollAuditEvent : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; }
    public string Payload { get; private set; } = string.Empty;

    private PayrollAuditEvent() { }

    public static PayrollAuditEvent Record(
        string tenantId,
        Guid payrollRunId,
        string action,
        string actor,
        string payload = "")
    {
        return new PayrollAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            Action = action,
            Actor = actor,
            Payload = payload,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
