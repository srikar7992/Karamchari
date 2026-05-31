using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// One row per employee per review cycle for the 9-box / talent heatmap.
/// Populated from <see cref="PerformanceSnapshot"/> after calibration completes.
/// Used for succession planning, retention risk dashboards, and executive talent views.
/// </summary>
public sealed class TalentHeatmapEntry : Entity<Guid>, ITenantOwned
{
    private TalentHeatmapEntry() { /* EF materialization */ }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeDisplayName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Department { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CareerLevel { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CycleName { get; private set; } = string.Empty;

    /// <summary>0â€“100 weighted composite from calibrated review.</summary>
    public decimal CompositeScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PerformanceBucket { get; private set; } = string.Empty;

    /// <summary>1â€“3 potential rating from manager/calibration panel.</summary>
    public int PotentialRating { get; private set; }

    /// <summary>9-box grid position: "Performance_Potential" e.g. "High_High".</summary>
    public string NineBoxPosition { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsHighPerformer { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsAtRetentionRisk { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsPromotionReady { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsSuccessionCandidate { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset MaterializedOnUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static TalentHeatmapEntry Create(
        string tenantId,
        Guid employeeId,
        string employeeDisplayName,
        string department,
        string careerLevel,
        Guid reviewCycleId,
        string cycleName,
        decimal compositeScore,
        string performanceBucket,
        int potentialRating,
        bool isHighPerformer,
        bool isAtRetentionRisk,
        bool isPromotionReady,
        bool isSuccessionCandidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(performanceBucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(cycleName);

        if (potentialRating is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(potentialRating), "Potential rating must be 1, 2, or 3.");

        var performanceLabel = compositeScore switch
        {
            >= 85 => "High",
            >= 60 => "Medium",
            _ => "Low",
        };
        var potentialLabel = potentialRating switch
        {
            3 => "High",
            2 => "Medium",
            _ => "Low",
        };

        return new TalentHeatmapEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeDisplayName = employeeDisplayName,
            Department = department,
            CareerLevel = careerLevel,
            ReviewCycleId = reviewCycleId,
            CycleName = cycleName,
            CompositeScore = compositeScore,
            PerformanceBucket = performanceBucket,
            PotentialRating = potentialRating,
            NineBoxPosition = $"{performanceLabel}_{potentialLabel}",
            IsHighPerformer = isHighPerformer,
            IsAtRetentionRisk = isAtRetentionRisk,
            IsPromotionReady = isPromotionReady,
            IsSuccessionCandidate = isSuccessionCandidate,
            MaterializedOnUtc = DateTimeOffset.UtcNow,
            LastRefreshedUtc = DateTimeOffset.UtcNow,
        };
    }
}
