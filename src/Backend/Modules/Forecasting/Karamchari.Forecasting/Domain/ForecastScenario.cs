using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Forecasting.Domain.Events;

namespace Karamchari.Forecasting.Domain;

public sealed class ForecastScenario : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ScenarioAssumption> _assumptions = [];
    private readonly List<HeadcountPlan> _headcountPlans = [];
    private readonly List<MonthlyWorkforceProjection> _projections = [];
    private ForecastScenario() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ScenarioType Type { get; private set; }
    public ScenarioStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastProjectedAtUtc { get; private set; }

    public IReadOnlyList<ScenarioAssumption> Assumptions => _assumptions.AsReadOnly();
    public IReadOnlyList<HeadcountPlan> HeadcountPlans => _headcountPlans.AsReadOnly();
    public IReadOnlyList<MonthlyWorkforceProjection> Projections => _projections.AsReadOnly();

    public static ForecastScenario Create(string tenantId, string name, ScenarioType type)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Type = type,
            Status = ScenarioStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    public void AddAssumption(ScenarioAssumption assumption) => _assumptions.Add(assumption);

    public void SetHeadcountPlan(HeadcountPlan plan)
    {
        _headcountPlans.RemoveAll(p => p.DepartmentId == plan.DepartmentId && p.FiscalYear == plan.FiscalYear);
        _headcountPlans.Add(plan);
    }

    public void Activate() => Status = ScenarioStatus.Active;
    public void Archive() => Status = ScenarioStatus.Archived;

    public void ApplyProjections(IEnumerable<MonthlyWorkforceProjection> projections)
    {
        _projections.Clear();
        _projections.AddRange(projections);
        LastProjectedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ScenarioProjectionComputed(Id, TenantId, Name, _projections.Count));
    }
}
