// -----------------------------------------------------------------------
// <copyright file="TemporalPolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Temporal;

/// <summary>
/// Defines the temporal governance rules for a specific business process.
/// Implements Priority 3 Step 14.
/// </summary>
public sealed class TemporalPolicy : ITenantOwned
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty; // e.g. "Payroll", "Attendance"

    /// <summary>The number of days after a period ends that mutations are still allowed.</summary>
    public int CorrectionWindowDays { get; init; }

    /// <summary>Whether retroactive changes are allowed after the period is finalized.</summary>
    public bool AllowRetroactiveAfterFinalization { get; init; }

    /// <summary>The maximum duration into the past that a retroactive change can be made.</summary>
    public TimeSpan MaxRetroactiveLookback { get; init; }

    public static TemporalPolicy CreateDefault(string tenantId, string processName)
    {
        return new TemporalPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProcessName = processName,
            CorrectionWindowDays = 7,
            AllowRetroactiveAfterFinalization = false,
            MaxRetroactiveLookback = TimeSpan.FromDays(90)
        };
    }
}
