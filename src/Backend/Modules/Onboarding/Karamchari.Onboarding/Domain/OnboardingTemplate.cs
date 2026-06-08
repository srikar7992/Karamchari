using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Onboarding.Domain;

/// <summary>
/// Reusable checklist template for a given case type and role/department.
/// When a case is initiated from a template, its tasks are cloned from here.
/// </summary>
public sealed class OnboardingTemplate : AggregateRoot<OnboardingTemplateId>, ITenantOwned
{
    private readonly List<TemplateTask> _tasks = [];

    private OnboardingTemplate() { TenantId = string.Empty; Name = string.Empty; }

    private OnboardingTemplate(
        OnboardingTemplateId id,
        string tenantId,
        string name,
        OnboardingCaseType caseType,
        string? department,
        string? role) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        CaseType = caseType;
        Department = department;
        Role = role;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public OnboardingCaseType CaseType { get; private set; }
    public string? Department { get; private set; }
    public string? Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<TemplateTask> Tasks => _tasks.AsReadOnly();

    public static OnboardingTemplate Create(
        string tenantId,
        string name,
        OnboardingCaseType caseType,
        string? department = null,
        string? role = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new OnboardingTemplate(new OnboardingTemplateId(Guid.NewGuid()),
            tenantId, name, caseType, department, role);
    }

    public void AddTask(
        string title,
        TaskCategory category,
        int daysFromStartDue,
        string? description = null)
    {
        _tasks.Add(new TemplateTask(Guid.NewGuid(), Id, title, category, daysFromStartDue, description));
    }

    public void Deactivate() => IsActive = false;

    /// <summary>Instantiate tasks into an OnboardingCase based on this template.</summary>
    public void ApplyTo(OnboardingCase @case)
    {
        foreach (var t in _tasks)
        {
            var due = @case.ExpectedStartDate.AddDays(t.DaysFromStartDue);
            @case.AddTask(t.Title, t.Category, null, due, t.Description);
        }
    }
}

public sealed class TemplateTask : Entity<Guid>
{
    private TemplateTask() { Title = string.Empty; }

    internal TemplateTask(
        Guid id,
        OnboardingTemplateId templateId,
        string title,
        TaskCategory category,
        int daysFromStartDue,
        string? description) : base(id)
    {
        TemplateId = templateId;
        Title = title;
        Category = category;
        DaysFromStartDue = daysFromStartDue;
        Description = description;
    }

    public OnboardingTemplateId TemplateId { get; private set; } = null!;
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskCategory Category { get; private set; }
    public int DaysFromStartDue { get; private set; }
}
