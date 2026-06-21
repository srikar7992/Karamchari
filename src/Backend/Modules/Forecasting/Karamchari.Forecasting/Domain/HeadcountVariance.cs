using Karamchari.Core.Multitenancy;
namespace Karamchari.Forecasting.Domain;

public sealed class HeadcountVariance : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid ScenarioId { get; init; }
    public Guid DepartmentId { get; init; }
    public int FiscalYear { get; init; }
    public int Month { get; init; }
    public int PlannedHeadcount { get; init; }
    public int ActualHeadcount { get; init; }
    public int Variance => ActualHeadcount - PlannedHeadcount;
    public DateTimeOffset ComputedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
