using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Attendance;

public enum AnomalyType
{
    LateArrival,
    EarlyDeparture,
    MissingCheckOut,
    BuddyPunchingSuspicion,
    GeoFenceBreach,
    ImpossibleTravel,
    SystemManipulation
}

public enum AnomalyStatus { Open, Investigating, Resolved, FalsePositive }

/// <summary>
/// Aggregate root for tracking workforce anomalies.
/// Created automatically by detection engine or manually by managers.
/// </summary>
public sealed class AttendanceAnomaly : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid SessionId { get; private set; }
    
    public AnomalyType Type { get; private set; }
    public AnomalyStatus Status { get; private set; }
    public decimal ConfidenceScore { get; private set; } // 0 to 1

    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; private set; }
    
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNote { get; private set; }

    private AttendanceAnomaly() { }

    public static AttendanceAnomaly Detect(
        string tenantId, 
        Guid employeeId, 
        Guid sessionId, 
        AnomalyType type, 
        decimal confidence,
        string description)
    {
        return new AttendanceAnomaly
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            SessionId = sessionId,
            Type = type,
            ConfidenceScore = confidence,
            Description = description,
            Status = AnomalyStatus.Open,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Resolve(string userId, string note, bool isFalsePositive = false)
    {
        Status = isFalsePositive ? AnomalyStatus.FalsePositive : AnomalyStatus.Resolved;
        ResolvedBy = userId;
        ResolutionNote = note;
    }
}
