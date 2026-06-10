// -----------------------------------------------------------------------
// <copyright file="LeavePolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Policy for a specific leave type at a specific hierarchy level.
/// Hierarchy: Global → Country → BusinessUnit → Department → Grade → Employee.
/// Resolution: most specific non-null value wins.
/// </summary>
public sealed class LeavePolicy : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LeavePolicyVersion> _versions = [];

    private LeavePolicy(Guid id, Guid leaveTypeId, PolicyLevel level, string name,
        string description, LeavePolicyRules rules) : base(id)
    {
        LeaveTypeId = leaveTypeId;
        Level = level;
        Name = name;
        Description = description;
        Rules = rules;
        CurrentVersion = 1;
        IsActive = true;
    }

    private LeavePolicy()
    {
        Name = string.Empty;
        Description = string.Empty;
        TenantId = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid LeaveTypeId { get; private set; }
    public PolicyLevel Level { get; private set; }

    /// <summary>
    /// Scope this policy applies to. Null = applies to all at this level.
    /// For Country = CountryCode, for BU/Dept/Grade/Employee = their respective ID.
    /// </summary>
    public string? ScopeKey { get; private set; }

    /// <summary>Parent policy this inherits from (null for Global).</summary>
    public Guid? ParentPolicyId { get; private set; }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public int CurrentVersion { get; private set; }
    public LeavePolicyRules Rules { get; private set; } = null!;

    public IReadOnlyCollection<LeavePolicyVersion> Versions => _versions.AsReadOnly();

    public static LeavePolicy Create(Guid leaveTypeId, PolicyLevel level, string name,
        string description, LeavePolicyRules rules, string? scopeKey = null,
        Guid? parentPolicyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rules);

        return new LeavePolicy(Guid.NewGuid(), leaveTypeId, level, name.Trim(),
            description.Trim(), rules)
        {
            ScopeKey = scopeKey,
            ParentPolicyId = parentPolicyId
        };
    }

    /// <summary>
    /// Updates rules and creates a new immutable version snapshot.
    /// Never overwrites — appends to version history.
    /// </summary>
    public void UpdateRules(LeavePolicyRules newRules, DateOnly effectiveFrom,
        string changedBy, string? changeReason = null)
    {
        ArgumentNullException.ThrowIfNull(newRules);

        // Close current version
        LeavePolicyVersion? current = _versions.FirstOrDefault(v => v.EffectiveTo is null);
        current?.Close(effectiveFrom.AddDays(-1));

        CurrentVersion++;
        var version = LeavePolicyVersion.Create(Id, CurrentVersion, newRules,
            effectiveFrom, changedBy, changeReason);
        _versions.Add(version);

        Rules = newRules;
    }

    public void Deactivate() => IsActive = false;
}

public enum PolicyLevel
{
    Global = 1,
    Country = 2,
    BusinessUnit = 3,
    Department = 4,
    Grade = 5,
    Employee = 6
}

/// <summary>
/// Defines the category of leave. Kept for EF backward compatibility.
/// Prefer <see cref="LeaveKind"/> on <see cref="LeaveType"/> for new code.
/// </summary>
public enum LeaveCategory
{
    Paid = 1,
    Unpaid = 2,
    Statutory = 3
}

public enum AccrualFrequency
{
    Upfront = 1,
    Monthly = 2,
    Biweekly = 3,
    HoursWorked = 4,
    TenureBased = 5,
    EventBased = 6
}

/// <summary>
/// Configurable rules for a leave policy. Stored as JSONB.
/// </summary>
public sealed class LeavePolicyRules
{
    public LeaveCategory Category { get; set; }
    public AccrualFrequency AccrualFrequency { get; set; }

    /// <summary>Annual entitlement in days (for Upfront accrual).</summary>
    public double AnnualAllowance { get; set; }

    /// <summary>Days accrued per month (for Monthly accrual).</summary>
    public double MonthlyAccrualDays { get; set; }

    /// <summary>Tenure-based entitlement brackets. Used when AccrualFrequency = TenureBased.</summary>
    public List<TenureEntitlementBracket> TenureBrackets { get; set; } = [];

    public bool RequiresManagerApproval { get; set; } = true;
    public bool AllowHalfDays { get; set; }
    public bool AllowHourly { get; set; }
    public bool AllowNegativeBalance { get; set; }

    /// <summary>Max days that carry forward to next year. 0 = no carry forward.</summary>
    public double MaximumCarryForward { get; set; }

    /// <summary>Carry-forward balance expires after this many months. Null = never.</summary>
    public int? CarryForwardExpiryMonths { get; set; }

    public bool AllowEncashment { get; set; }
    public double MaxEncashmentDays { get; set; }

    /// <summary>Minimum advance notice in calendar days before leave starts.</summary>
    public int NoticeRequiredDays { get; set; }

    /// <summary>Maximum days allowed in a single continuous application.</summary>
    public int? MaxConsecutiveDays { get; set; }

    /// <summary>Minimum gap (working days) required between two applications of same type.</summary>
    public int? MinGapBetweenApplicationsDays { get; set; }

    /// <summary>Whether adjoining holidays/weekends are absorbed into leave (sandwich policy).</summary>
    public bool AllowHolidaySandwich { get; set; }

    /// <summary>Whether leave can sandwich a public holiday between two leave days.</summary>
    public bool AllowPrefixSuffix { get; set; }

    /// <summary>Require medical document after N consecutive sick days.</summary>
    public bool RequiresDocument { get; set; }
    public int? DocumentRequiredAfterDays { get; set; }

    /// <summary>Continue accrual during LOP month. Policy-configurable.</summary>
    public bool AccrualContinuesDuringLop { get; set; }

    /// <summary>Continue accrual during maternity/paternity. Policy-configurable.</summary>
    public bool AccrualContinuesDuringStatutory { get; set; } = true;

    /// <summary>
    /// Maximum days in the past an employee can apply leave (retroactive window).
    /// 0 = no backdating. 2 = sick leave can be applied up to 2 days after return.
    /// </summary>
    public int MaxRetroactiveDays { get; set; }

    /// <summary>Behavior when leave is applied during notice period.</summary>
    public NoticePeriodLeaveMode NoticePeriodLeaveMode { get; set; } = NoticePeriodLeaveMode.AllowedNoExtension;
}

public sealed class TenureEntitlementBracket
{
    public int FromYears { get; set; }
    public int? ToYears { get; set; }
    public double AnnualDays { get; set; }
}
