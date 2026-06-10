// -----------------------------------------------------------------------
// <copyright file="LeaveType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Metadata-driven leave type definition. Never hardcode leave rules — configure here.
/// </summary>
public sealed class LeaveType : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveType()
    {
        TenantId = string.Empty;
        Code = string.Empty;
        Name = string.Empty;
    }

    private LeaveType(Guid id, string code, string name, LeaveKind kind) : base(id)
    {
        Code = code;
        Name = name;
        Kind = kind;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Code { get; private set; }
    public string Name { get; private set; }
    public LeaveKind Kind { get; private set; }
    public bool IsPaid { get; private set; }
    public bool RequiresApproval { get; private set; }
    public bool RequiresDocument { get; private set; }
    public int? DocumentRequiredAfterDays { get; private set; }
    public bool AllowHalfDay { get; private set; }
    public bool AllowHourly { get; private set; }
    public bool AllowNegativeBalance { get; private set; }
    public bool AllowCarryForward { get; private set; }
    public bool AllowEncashment { get; private set; }
    public bool AllowPrefixSuffix { get; private set; }
    public bool AllowHolidaySandwich { get; private set; }
    public int? MaxConsecutiveDays { get; private set; }
    public bool IsActive { get; private set; }

    public static LeaveType Create(string code, string name, LeaveKind kind, LeaveTypeOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        var lt = new LeaveType(Guid.NewGuid(), code.ToUpperInvariant().Trim(), name.Trim(), kind)
        {
            IsPaid = options.IsPaid,
            RequiresApproval = options.RequiresApproval,
            RequiresDocument = options.RequiresDocument,
            DocumentRequiredAfterDays = options.DocumentRequiredAfterDays,
            AllowHalfDay = options.AllowHalfDay,
            AllowHourly = options.AllowHourly,
            AllowNegativeBalance = options.AllowNegativeBalance,
            AllowCarryForward = options.AllowCarryForward,
            AllowEncashment = options.AllowEncashment,
            AllowPrefixSuffix = options.AllowPrefixSuffix,
            AllowHolidaySandwich = options.AllowHolidaySandwich,
            MaxConsecutiveDays = options.MaxConsecutiveDays
        };

        return lt;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

public enum LeaveKind
{
    Earned = 1,
    Sick = 2,
    Casual = 3,
    CompOff = 4,
    Maternity = 5,
    Paternity = 6,
    Bereavement = 7,
    Sabbatical = 8,
    Marriage = 9,
    Wellness = 10,
    OptionalHoliday = 11,
    WFH = 12,
    LOP = 13,
    LWP = 14
}

public sealed class LeaveTypeOptions
{
    public bool IsPaid { get; init; } = true;
    public bool RequiresApproval { get; init; } = true;
    public bool RequiresDocument { get; init; }
    public int? DocumentRequiredAfterDays { get; init; }
    public bool AllowHalfDay { get; init; }
    public bool AllowHourly { get; init; }
    public bool AllowNegativeBalance { get; init; }
    public bool AllowCarryForward { get; init; }
    public bool AllowEncashment { get; init; }
    public bool AllowPrefixSuffix { get; init; }
    public bool AllowHolidaySandwich { get; init; }
    public int? MaxConsecutiveDays { get; init; }
}
