namespace Karamchari.Payroll.Domain.Corrections;

/// <summary>
/// Authoritative business status for payroll corrections.
/// </summary>
public enum CorrectionStatus
{
    /// <summary>Correction is being drafted.</summary>
    Draft = 1,

    /// <summary>Correction has been submitted and is awaiting coordination.</summary>
    Submitted = 2,

    /// <summary>Correction has been approved and is authoritative truth.</summary>
    Approved = 3,

    /// <summary>Correction has been applied to payroll.</summary>
    Applied = 4,

    /// <summary>Correction was rejected by coordination.</summary>
    Rejected = 5,

    /// <summary>Correction was cancelled by the initiator.</summary>
    Cancelled = 6,

    /// <summary>Correction is being recalculated.</summary>
    RecalculationInProgress = 7,

    /// <summary>Correction has been processed in a payroll run.</summary>
    Processed = 8
}

public enum CorrectionType
{
    Omission,
    Error,
    BackdatedAction
}

public enum CorrectionScope
{
    Earnings,
    Deductions,
    Statutory
}
