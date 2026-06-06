using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Insights;

public sealed class ExecutiveDigest : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public DigestPeriod Period { get; private set; }
    public DateTime PeriodDate { get; private set; }
    public string HeadlineJson { get; private set; } = string.Empty;
    public string MetricSummaryJson { get; private set; } = string.Empty;
    public string TopRisksJson { get; private set; } = string.Empty;
    public string TopRecommendationsJson { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }

    private ExecutiveDigest() { }

    public static ExecutiveDigest Create(
        string tenantId,
        DigestPeriod period,
        DateTime periodDate,
        string headlineJson,
        string metricSummaryJson,
        string topRisksJson,
        string topRecommendationsJson)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Period = period,
            PeriodDate = periodDate,
            HeadlineJson = headlineJson,
            MetricSummaryJson = metricSummaryJson,
            TopRisksJson = topRisksJson,
            TopRecommendationsJson = topRecommendationsJson,
            GeneratedAt = DateTime.UtcNow
        };
}
