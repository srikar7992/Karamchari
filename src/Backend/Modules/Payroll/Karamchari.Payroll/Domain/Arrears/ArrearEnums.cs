namespace Karamchari.Payroll.Domain.Arrears;

/// <summary>
/// Authoritative business status for arrear calculations.
/// </summary>
public enum ArrearStatus
{
    /// <summary>Arrear calculation is pending review.</summary>
    Pending = 1,

    /// <summary>Arrear has been approved and is authoritative truth.</summary>
    Approved = 2,

    /// <summary>Arrear has been processed in a payroll run.</summary>
    Processed = 3,

    /// <summary>Arrear was rejected or cancelled.</summary>
    Rejected = 4,

    /// <summary>Arrear was reversed after being processed.</summary>
    Reversed = 5
}

public enum ArrearTriggerType
{
    SalaryRevision,
    Correction,
    Manual
}
