// -----------------------------------------------------------------------
// <copyright file="TemporalGovernanceService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Temporal;

/// <summary>
/// Service for enforcing temporal operational policies.
/// Implements Priority 3 Step 13.
/// </summary>
public sealed class TemporalGovernanceService
{
    // In a real system, this would query a database of policies.
    // For now, we use a simple in-memory default.

    public bool IsMutationAllowed(
        string tenantId,
        string processName,
        DateOnly businessDate,
        DateTimeOffset currentTimestamp,
        out string? reason)
    {
        var policy = TemporalPolicy.CreateDefault(tenantId, processName);
        var businessDateTime = businessDate.ToDateTime(TimeOnly.MinValue);

        // 1. Check lookback limit
        if (currentTimestamp - businessDateTime > policy.MaxRetroactiveLookback)
        {
            reason = $"Mutation date {businessDate} exceeds the maximum lookback of {policy.MaxRetroactiveLookback.TotalDays} days.";
            return false;
        }

        // 2. Check correction window for closed periods
        // (Simplified: assuming any date older than today is "closed" for demo)
        var today = DateOnly.FromDateTime(currentTimestamp.DateTime);
        if (businessDate < today)
        {
            var daysPast = (today.ToDateTime(TimeOnly.MinValue) - businessDateTime).TotalDays;
            if (daysPast > policy.CorrectionWindowDays && !policy.AllowRetroactiveAfterFinalization)
            {
                reason = $"Correction window of {policy.CorrectionWindowDays} days has closed for date {businessDate}.";
                return false;
            }
        }

        reason = null;
        return true;
    }
}
