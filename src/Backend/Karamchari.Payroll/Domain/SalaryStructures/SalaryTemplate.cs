namespace Karamchari.Payroll.Domain.SalaryStructures;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines a specific configuration of salary components that can be applied to an employee's CTC.
/// </summary>
public sealed class SalaryTemplate : ITenantOwned
{
    private readonly List<SalaryTemplateComponent> _components = new();

    private SalaryTemplate(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    private SalaryTemplate()
    {
        Name = string.Empty;
        TenantId = string.Empty;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier for the template.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the name of the template (e.g. "Software Engineer - Standard").</summary>
    public string Name { get; private set; }

    /// <summary>Gets the collection of components and their calculation rules.</summary>
    public IReadOnlyCollection<SalaryTemplateComponent> Components => _components.AsReadOnly();

    /// <summary>
    /// Creates a new salary template.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <returns>A new <see cref="SalaryTemplate"/> instance.</returns>
    public static SalaryTemplate Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SalaryTemplate(Guid.NewGuid(), name);
    }

    /// <summary>
    /// Adds or updates a component within the template.
    /// </summary>
    public void AddComponent(SalaryTemplateComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        
        // Remove if exists and add fresh
        var existing = _components.FirstOrDefault(c => c.ComponentId == component.ComponentId);
        if (existing != null) _components.Remove(existing);
        
        _components.Add(component);
    }
}

/// <summary>
/// Represents the configuration of a specific component within a template.
/// </summary>
public sealed record SalaryTemplateComponent
{
    /// <summary>Gets the ID of the master SalaryComponent.</summary>
    public Guid ComponentId { get; init; }

    /// <summary>Gets how the value is calculated.</summary>
    public ComponentCalculationType CalculationType { get; init; }

    /// <summary>Gets the literal value or percentage (e.g. 40.0 for 40%).</summary>
    public decimal? Value { get; init; }

    /// <summary>Gets the execution priority for components with the same dependency level.</summary>
    public int ExecutionPriority { get; init; }

    /// <summary>Gets the list of component IDs this calculation depends on.</summary>
    public List<Guid> DependsOnComponentIds { get; init; } = new();

    /// <summary>Gets the strategy for aggregating multiple dependencies.</summary>
    public DependencyAggregationType AggregationType { get; init; } = DependencyAggregationType.Sum;
}
