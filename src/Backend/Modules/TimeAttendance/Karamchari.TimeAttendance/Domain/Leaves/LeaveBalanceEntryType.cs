namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Defines the types of entries that can be recorded in the leave balance ledger.
/// </summary>
public enum LeaveBalanceEntryType
{
    /// <summary>Automatic or manual leave accrual.</summary>
    Accrual = 1,

    /// <summary>Deduction due to leave consumption.</summary>
    Consumption = 2,

    /// <summary>Balance carried forward from previous period.</summary>
    CarryForward = 3,

    /// <summary>Deduction due to balance expiry.</summary>
    Expiry = 4,

    /// <summary>Deduction due to leave encashment.</summary>
    Encashment = 5,

    /// <summary>Balance restoration due to leave cancellation.</summary>
    CancellationRestore = 6,

    /// <summary>Manual adjustment by administrator.</summary>
    ManualAdjustment = 7,

    /// <summary>Credit given for working on a holiday/weekend.</summary>
    CompOffCredit = 8,

    /// <summary>Consumption of comp-off credit.</summary>
    CompOffConsumption = 9
}
