namespace Karamchari.Intelligence.Domain.Primitives;

public enum ConfidenceLevel
{
    Low,
    Medium,
    High,
    Verified
}

/// <summary>
/// Represents the trustworthiness and freshness of a signal.
/// Prevents "garbage in, garbage out" scenarios in executive dashboards.
/// </summary>
public sealed record SignalConfidence
{
    public decimal Score { get; } // 0 to 100
    public ConfidenceLevel Level { get; }
    public string EvidenceType { get; } // e.g., "ManagerAssessment", "SystemCalculated", "ExternalCertification"
    public DateTimeOffset EvaluatedAtUtc { get; }

    private SignalConfidence(decimal score, ConfidenceLevel level, string evidenceType, DateTimeOffset evaluatedAt)
    {
        Score = score;
        Level = level;
        EvidenceType = evidenceType;
        EvaluatedAtUtc = evaluatedAt;
    }

    public static SignalConfidence Create(decimal score, string evidenceType)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("Confidence score must be between 0 and 100.");

        var level = score switch
        {
            >= 90 => ConfidenceLevel.Verified,
            >= 70 => ConfidenceLevel.High,
            >= 40 => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };

        return new SignalConfidence(score, level, evidenceType, DateTimeOffset.UtcNow);
    }
}
