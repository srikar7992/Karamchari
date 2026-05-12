namespace Karamchari.Performance.Domain.Calibration;

/// <summary>
/// Authoritative business status for calibration sessions.
/// </summary>
public enum CalibrationSessionStatus
{
    /// <summary>Session is being set up.</summary>
    Draft = 1,

    /// <summary>Session is active and scores are being adjusted.</summary>
    InProgress = 2,

    /// <summary>Session has been finalized and scores are authoritative.</summary>
    Finalized = 3,

    /// <summary>Session was archived.</summary>
    Archived = 4,

    /// <summary>Session was rejected during coordination.</summary>
    Rejected = 5
}
