using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Succession.Domain;

public sealed class TalentReview : AggregateRoot<Guid>, ITenantOwned
{
    private TalentReview() { }
    private TalentReview(Guid id, string tenantId, Guid employeeId, string reviewCycleName,
        int performanceScore, int potentialScore, string? strengths, string? developmentAreas) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        ReviewCycleName = reviewCycleName;
        PerformanceScore = performanceScore;
        PotentialScore = potentialScore;
        Strengths = strengths;
        DevelopmentAreas = developmentAreas;
        NineBoxPosition = ComputeNineBox(performanceScore, potentialScore);
        ReviewedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string ReviewCycleName { get; private set; } = string.Empty;
    public int PerformanceScore { get; private set; }  // 1-3
    public int PotentialScore { get; private set; }    // 1-3
    public string NineBoxPosition { get; private set; } = string.Empty;
    public string? Strengths { get; private set; }
    public string? DevelopmentAreas { get; private set; }
    public bool IsSuccessionCandidate { get; private set; }
    public bool IsFlightRisk { get; private set; }
    public DateTimeOffset ReviewedAtUtc { get; private set; }

    public static TalentReview Create(string tenantId, Guid employeeId, string reviewCycleName,
        int performanceScore, int potentialScore, string? strengths = null, string? developmentAreas = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (performanceScore < 1 || performanceScore > 3) throw new ArgumentOutOfRangeException(nameof(performanceScore), "Must be 1-3.");
        if (potentialScore < 1 || potentialScore > 3) throw new ArgumentOutOfRangeException(nameof(potentialScore), "Must be 1-3.");
        return new TalentReview(Guid.NewGuid(), tenantId, employeeId, reviewCycleName, performanceScore, potentialScore, strengths, developmentAreas);
    }

    public void FlagSuccessionCandidate(bool value) => IsSuccessionCandidate = value;
    public void FlagFlightRisk(bool value) => IsFlightRisk = value;

    private static string ComputeNineBox(int perf, int pot)
    {
        var p = perf switch { 1 => "Low", 2 => "Medium", _ => "High" };
        var q = pot switch { 1 => "Low", 2 => "Medium", _ => "High" };
        return $"{p}_{q}";
    }
}
