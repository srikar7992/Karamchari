// -----------------------------------------------------------------------
// <copyright file="OvertimePolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.WorkRules;

/// <summary>
/// Tenant-level overtime policy. Contains ordered tiers — evaluated lowest-priority first.
/// Example: [0-2 OT hrs @1.5x, 2+ OT hrs @2x, Holiday @3x, Weekend @2x].
/// </summary>
public sealed class OvertimePolicy : ITenantOwned
{
    private readonly List<OvertimeRule> _rules = [];

    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<OvertimeRule> Rules => _rules.AsReadOnly();

    private OvertimePolicy() { }

    public static OvertimePolicy Create(string tenantId, string name)
    {
        return new OvertimePolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddRule(decimal fromHours, decimal? toHours, decimal multiplier, OvertimeContext context, int priority)
    {
        _rules.Add(OvertimeRule.Create(fromHours, toHours, multiplier, context, priority));
        _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// Calculates total overtime pay given regular hours threshold and actual hours worked.
    /// Returns itemised breakdown per rule tier.
    /// </summary>
    public IReadOnlyList<OvertimeTierResult> CalculateOvertimePay(
        decimal hourlyRate,
        decimal totalHours,
        decimal regularThreshold,
        OvertimeContext context)
    {
        var results = new List<OvertimeTierResult>();
        var overtimeHours = Math.Max(0m, totalHours - regularThreshold);

        if (overtimeHours == 0m)
            return results;

        var applicableRules = _rules
            .Where(r => r.Context == context || r.Context == OvertimeContext.Regular)
            .OrderBy(r => r.Priority)
            .ToList();

        var remaining = overtimeHours;

        foreach (var rule in applicableRules)
        {
            if (remaining <= 0m) break;

            var tierCapacity = rule.ToHours.HasValue
                ? Math.Min(rule.ToHours.Value - rule.FromHours, remaining)
                : remaining;

            var hoursInTier = Math.Min(tierCapacity, remaining);
            var pay = rule.Calculate(hourlyRate, hoursInTier);
            results.Add(new OvertimeTierResult(rule.Context, hoursInTier, rule.Multiplier, pay));
            remaining -= hoursInTier;
        }

        return results;
    }
}

public sealed record OvertimeTierResult(OvertimeContext Context, decimal Hours, decimal Multiplier, decimal Pay);
