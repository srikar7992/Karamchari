// -----------------------------------------------------------------------
// <copyright file="AttendancePolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Compliance;

/// <summary>
/// Aggregate root for attendance policies.
/// A policy is a collection of compliance rules assigned to a group of employees.
/// </summary>
public sealed class AttendancePolicy : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ComplianceRule> _rules = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<ComplianceRule> Rules => _rules.AsReadOnly();

    private AttendancePolicy() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static AttendancePolicy Create(string tenantId, string name, string description)
    {
        return new AttendancePolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddRule(ComplianceRuleType type, decimal threshold, ComplianceSeverity severity)
    {
        _rules.Add(ComplianceRule.Create(Id, type, threshold, severity));
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ComplianceRule : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid PolicyId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ComplianceRuleType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Threshold { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ComplianceSeverity Severity { get; private set; }

    private ComplianceRule() { }

    internal static ComplianceRule Create(Guid policyId, ComplianceRuleType type, decimal threshold, ComplianceSeverity severity)
    {
        return new ComplianceRule
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Type = type,
            Threshold = threshold,
            Severity = severity
        };
    }
}
