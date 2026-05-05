namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Types of detected fraud or anomalies in time punches.
/// </summary>
public enum FraudType
{
    BuddyPunching = 1,
    GpsSpoofing = 2,
    DeviceSharing = 3,
    OutsideGeoFence = 4,
    ImpossibleTravelSpeed = 5
}

public enum FraudStatus
{
    Detected = 1,
    UnderReview = 2,
    Confirmed = 3,
    Dismissed = 4
}

/// <summary>
/// A flag indicating suspicious activity related to an employee's attendance.
/// </summary>
public sealed class FraudFlag : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>The ID of the raw punch that triggered this flag, if applicable.</summary>
    public Guid? RelatedPunchId { get; private set; }

    public FraudType Type { get; private set; }

    public string Description { get; private set; } = string.Empty;

    /// <summary>Severity score out of 100.</summary>
    public int SeverityScore { get; private set; }

    public DateTime DetectedAtUtc { get; private set; }

    public FraudStatus Status { get; private set; }

    // Audit Context
    public string? ReviewedBy { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    private FraudFlag() { }

    public static FraudFlag Create(
        string tenantId,
        Guid employeeId,
        Guid? relatedPunchId,
        FraudType type,
        string description,
        int severityScore)
    {
        return new FraudFlag
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            RelatedPunchId = relatedPunchId,
            Type = type,
            Description = description,
            SeverityScore = severityScore,
            DetectedAtUtc = DateTime.UtcNow,
            Status = FraudStatus.Detected
        };
    }

    public void Review(FraudStatus newStatus, string reviewedBy, string notes)
    {
        Status = newStatus;
        ReviewedBy = reviewedBy;
        ReviewNotes = notes;
        ReviewedAtUtc = DateTime.UtcNow;
    }
}
