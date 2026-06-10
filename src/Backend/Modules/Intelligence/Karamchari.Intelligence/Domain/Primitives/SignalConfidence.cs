// -----------------------------------------------------------------------
// <copyright file="SignalConfidence.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Domain.Primitives;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ConfidenceLevel
{
    /// <inheritdoc/>
    Low,
    /// <inheritdoc/>
    Medium,
    /// <inheritdoc/>
    High,
    /// <inheritdoc/>
    Verified
}

/// <summary>
/// Represents the trustworthiness and freshness of a signal.
/// Prevents "garbage in, garbage out" scenarios in executive dashboards.
/// </summary>
public sealed record SignalConfidence
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Score { get; } // 0 to 100
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ConfidenceLevel Level { get; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EvidenceType { get; } // e.g., "ManagerAssessment", "SystemCalculated", "ExternalCertification"
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    private SignalConfidence(decimal score, ConfidenceLevel level, string evidenceType, DateTimeOffset evaluatedAt)
    {
        Score = score;
        Level = level;
        EvidenceType = evidenceType;
        EvaluatedAtUtc = evaluatedAt;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
