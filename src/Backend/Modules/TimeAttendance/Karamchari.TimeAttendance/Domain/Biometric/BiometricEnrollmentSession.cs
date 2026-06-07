using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Biometric;

public enum EnrollmentSessionStatus
{
    InProgress,
    QualityCheckFailed,
    Completed,
    Cancelled
}

/// <summary>
/// Tracks the 6-step biometric enrollment process (identity verification through audit log).
/// When Completed, triggers BiometricTemplate creation and device sync.
/// </summary>
public sealed class BiometricEnrollmentSession : AggregateRoot<Guid>, ITenantOwned
{
    private BiometricEnrollmentSession() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public BiometricModality Modality { get; private set; }

    /// <summary>Operator who performed the enrollment (HR admin or device admin).</summary>
    public string OperatorId { get; private set; } = string.Empty;

    public Guid? DeviceId { get; private set; }
    public EnrollmentSessionStatus Status { get; private set; }

    /// <summary>Number of samples captured (target: 3-5 per spec).</summary>
    public int SamplesCaptured { get; private set; }

    public int RequiredSampleCount { get; private set; }

    /// <summary>Average quality score across all accepted samples (0-100).</summary>
    public int AverageQualityScore { get; private set; }

    public int MinimumQualityThreshold { get; private set; }

    /// <summary>True when employee provided explicit biometric consent (GDPR/BIPA/PDPA).</summary>
    public bool ConsentObtained { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>Reference to the BiometricTemplate created on completion. Null until completed.</summary>
    public Guid? ResultTemplateId { get; private set; }

    public static BiometricEnrollmentSession Start(
        string tenantId,
        Guid employeeId,
        BiometricModality modality,
        string operatorId,
        Guid? deviceId = null,
        int requiredSampleCount = 3,
        int minimumQualityThreshold = 70)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorId);
        ArgumentOutOfRangeException.ThrowIfLessThan(requiredSampleCount, 1);

        return new BiometricEnrollmentSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Modality = modality,
            OperatorId = operatorId,
            DeviceId = deviceId,
            Status = EnrollmentSessionStatus.InProgress,
            RequiredSampleCount = requiredSampleCount,
            MinimumQualityThreshold = minimumQualityThreshold,
            StartedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void RecordConsent() => ConsentObtained = true;

    /// <summary>
    /// Records one sample capture. If quality below threshold, sets status to QualityCheckFailed.
    /// Returns true when enough samples collected and quality passes.
    /// </summary>
    public bool RecordSample(int qualityScore)
    {
        if (Status != EnrollmentSessionStatus.InProgress)
            throw new InvalidOperationException($"Cannot capture sample in status {Status}.");

        SamplesCaptured++;
        AverageQualityScore = AverageQualityScore == 0
            ? qualityScore
            : (AverageQualityScore * (SamplesCaptured - 1) + qualityScore) / SamplesCaptured;

        if (qualityScore < MinimumQualityThreshold)
        {
            Status = EnrollmentSessionStatus.QualityCheckFailed;
            return false;
        }

        return SamplesCaptured >= RequiredSampleCount;
    }

    public void RetryAfterQualityFailure()
    {
        if (Status != EnrollmentSessionStatus.QualityCheckFailed)
            throw new InvalidOperationException("Can only retry from QualityCheckFailed state.");

        Status = EnrollmentSessionStatus.InProgress;
        SamplesCaptured = 0;
        AverageQualityScore = 0;
    }

    public void Complete(Guid templateId)
    {
        if (!ConsentObtained)
            throw new InvalidOperationException("Biometric consent must be obtained before enrollment can complete.");

        if (SamplesCaptured < RequiredSampleCount)
            throw new InvalidOperationException($"Insufficient samples. Required: {RequiredSampleCount}, captured: {SamplesCaptured}.");

        Status = EnrollmentSessionStatus.Completed;
        ResultTemplateId = templateId;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = EnrollmentSessionStatus.Cancelled;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }
}
