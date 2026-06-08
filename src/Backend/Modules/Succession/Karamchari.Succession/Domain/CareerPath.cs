using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Succession.Domain;

public sealed class CareerPath : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<CareerStep> _steps = [];

    private CareerPath() { TenantId = string.Empty; Name = string.Empty; Description = string.Empty; }

    private CareerPath(Guid id, string tenantId, string name, string description) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyList<CareerStep> Steps => _steps.AsReadOnly();

    public static CareerPath Create(string tenantId, string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new CareerPath(Guid.NewGuid(), tenantId, name, description ?? string.Empty);
    }

    public CareerStep AddStep(int stepOrder, string roleTitle, Guid? roleSkillRequirementId, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle);
        if (_steps.Any(s => s.StepOrder == stepOrder))
            throw new InvalidOperationException($"Step order {stepOrder} already exists on this path.");

        var step = CareerStep.Create(Id, stepOrder, roleTitle, roleSkillRequirementId, notes);
        _steps.Add(step);
        return step;
    }

    public void RemoveStep(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException("Step not found.");
        _steps.Remove(step);
    }

    public void Update(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description ?? string.Empty;
    }

    public void Deactivate() => IsActive = false;
}

public sealed class CareerStep : Entity<Guid>
{
    private CareerStep() { RoleTitle = string.Empty; }

    private CareerStep(Guid id, Guid pathId, int stepOrder, string roleTitle,
        Guid? roleSkillRequirementId, string? notes) : base(id)
    {
        PathId = pathId;
        StepOrder = stepOrder;
        RoleTitle = roleTitle;
        RoleSkillRequirementId = roleSkillRequirementId;
        Notes = notes;
    }

    public Guid PathId { get; private set; }
    public int StepOrder { get; private set; }
    public string RoleTitle { get; private set; }
    public Guid? RoleSkillRequirementId { get; private set; }
    public string? Notes { get; private set; }

    internal static CareerStep Create(Guid pathId, int stepOrder, string roleTitle,
        Guid? roleSkillRequirementId, string? notes)
        => new(Guid.NewGuid(), pathId, stepOrder, roleTitle, roleSkillRequirementId, notes);
}
