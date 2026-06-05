namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Defines the types of entries in the leave balance ledger (immutable, append-only).
/// </summary>
public enum LeaveBalanceEntryType
{
    Accrual = 1,
    Consumption = 2,
    CarryForward = 3,
    Expiry = 4,
    Encashment = 5,
    CancellationRestore = 6,
    ManualAdjustment = 7,
    CompOffCredit = 8,
    CompOffConsumption = 9,

    /// <summary>Balance imported from a legacy system during data migration.</summary>
    Migration = 10,

    /// <summary>HR-initiated correction to fix a ledger error. Requires audit justification.</summary>
    Correction = 11,

    /// <summary>Balance deducted for Loss Of Pay. Authorised to go negative — payroll deducts salary.</summary>
    LOPConversion = 12,

    /// <summary>Payroll-system correction (e.g. salary change retroactive). Authorised to go negative.</summary>
    PayrollCorrection = 13,
}

/// <summary>
/// Reason codes for LeaveBalance.Adjust(). Replaces magic ForceAdjust bypass.
/// Auditors can filter ledger by reason; LOPConversion and PayrollCorrection bypass balance guard.
/// </summary>
public enum AdjustmentReason
{
    ManualCorrection = 1,
    PayrollCorrection = 2,
    Migration = 3,
    LOPConversion = 4,
}
