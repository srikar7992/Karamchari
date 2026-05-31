namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum EvidenceType
{
    /// <inheritdoc/>
    SystemCalculated,
    /// <inheritdoc/>
    VerifiedCertificate,
    /// <inheritdoc/>
    ManagerAssessment,
    /// <inheritdoc/>
    PeerEndorsement,
    /// <inheritdoc/>
    HistoricalPerformance,
    /// <inheritdoc/>
    SelfReported
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ContributingFactor(string Name, decimal ImpactWeight, EvidenceType Evidence, string? Description);
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record PenalizingFactor(string Name, decimal ImpactWeight, string Reason);
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MissingEvidence(string Name, string Description);

/// <summary>
/// Structure required by the UI to render the "Why?" behind an intelligence score.
/// Prevents black-box algorithmic ranking.
/// </summary>
public sealed record ScoreExplanation
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<ContributingFactor> Contributors { get; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<PenalizingFactor> Penalties { get; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<MissingEvidence> MissingInputs { get; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Summary { get; }

    private ScoreExplanation(
        List<ContributingFactor> contributors,
        List<PenalizingFactor> penalties,
        List<MissingEvidence> missing,
        string summary)
    {
        Contributors = contributors.AsReadOnly();
        Penalties = penalties.AsReadOnly();
        MissingInputs = missing.AsReadOnly();
        Summary = summary;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ScoreExplanation Compile(
        IEnumerable<ContributingFactor> contributors,
        IEnumerable<PenalizingFactor> penalties,
        IEnumerable<MissingEvidence> missing,
        string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("A plain-text summary is required for executive explainability.");

        return new ScoreExplanation(
            contributors.ToList(),
            penalties.ToList(),
            missing.ToList(),
            summary);
    }
}
