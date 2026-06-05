namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Immutable snapshot of the policy rules at the moment of leave submission.
/// Stored as owned JSON on LeaveRequest so historical approvals are never affected
/// by future policy changes.
/// </summary>
public sealed class LeavePolicySnapshot
{
    public Guid PolicyId { get; init; }
    public int PolicyVersion { get; init; }
    public decimal EntitlementAtSnapshot { get; init; }

    // Flatten critical rules (avoid deep JSON nesting with TenureBrackets)
    public double AnnualAllowance { get; init; }
    public bool RequiresManagerApproval { get; init; }
    public bool AllowHalfDays { get; init; }
    public bool AllowHourly { get; init; }
    public bool AllowNegativeBalance { get; init; }
    public double MaximumCarryForward { get; init; }
    public int NoticeRequiredDays { get; init; }
    public int? MaxConsecutiveDays { get; init; }
    public bool AllowHolidaySandwich { get; init; }
    public bool AllowPrefixSuffix { get; init; }
    public int MaxRetroactiveDays { get; init; }
    public NoticePeriodLeaveMode NoticePeriodLeaveMode { get; init; }
    public DateTimeOffset TakenAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public static LeavePolicySnapshot From(LeavePolicy policy, decimal entitlement)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return new LeavePolicySnapshot
        {
            PolicyId = policy.Id,
            PolicyVersion = policy.CurrentVersion,
            EntitlementAtSnapshot = entitlement,
            AnnualAllowance = policy.Rules.AnnualAllowance,
            RequiresManagerApproval = policy.Rules.RequiresManagerApproval,
            AllowHalfDays = policy.Rules.AllowHalfDays,
            AllowHourly = policy.Rules.AllowHourly,
            AllowNegativeBalance = policy.Rules.AllowNegativeBalance,
            MaximumCarryForward = policy.Rules.MaximumCarryForward,
            NoticeRequiredDays = policy.Rules.NoticeRequiredDays,
            MaxConsecutiveDays = policy.Rules.MaxConsecutiveDays,
            AllowHolidaySandwich = policy.Rules.AllowHolidaySandwich,
            AllowPrefixSuffix = policy.Rules.AllowPrefixSuffix,
            MaxRetroactiveDays = policy.Rules.MaxRetroactiveDays,
            NoticePeriodLeaveMode = policy.Rules.NoticePeriodLeaveMode
        };
    }
}

public enum NoticePeriodLeaveMode
{
    /// <summary>Leave not permitted during notice period.</summary>
    NotAllowed = 1,

    /// <summary>Leave permitted; notice period extended by leave days taken.</summary>
    AllowedExtendNotice = 2,

    /// <summary>Leave permitted; notice period not extended.</summary>
    AllowedNoExtension = 3
}
