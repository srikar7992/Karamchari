namespace Karamchari.Intelligence.Domain.Signals;

public enum EvidenceType
{
    SystemCalculated,
    VerifiedCertificate,
    ManagerAssessment,
    PeerEndorsement,
    HistoricalPerformance,
    SelfReported
}

public sealed record ContributingFactor(string Name, decimal ImpactWeight, EvidenceType Evidence, string? Description);
public sealed record PenalizingFactor(string Name, decimal ImpactWeight, string Reason);
public sealed record MissingEvidence(string Name, string Description);

/// <summary>
/// Structure required by the UI to render the "Why?" behind an intelligence score.
/// Prevents black-box algorithmic ranking.
/// </summary>
public sealed record ScoreExplanation
{
    public IReadOnlyCollection<ContributingFactor> Contributors { get; }
    public IReadOnlyCollection<PenalizingFactor> Penalties { get; }
    public IReadOnlyCollection<MissingEvidence> MissingInputs { get; }
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
