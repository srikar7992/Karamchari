namespace Karamchari.TimeAttendance.Domain.Approval;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Reusable workflow definition decoupled from any specific aggregate.
/// Resolves the approval chain dynamically based on context rules.
///
/// Supports: Leave, Overtime, ShiftSwap, Travel, Expense (future).
/// Rule examples:
///   - if ActualDays > 5 → add HR step
///   - if EmployeeGrade >= Director → add SkipLevel step
///   - always → add Manager step
/// </summary>
public sealed class ApprovalWorkflowDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ApprovalStepDefinition> _steps = [];

    private ApprovalWorkflowDefinition(Guid id, string workflowName, string entityType) : base(id)
    {
        WorkflowName = workflowName;
        EntityType = entityType;
        IsActive = true;
    }

    private ApprovalWorkflowDefinition()
    {
        TenantId = string.Empty;
        WorkflowName = string.Empty;
        EntityType = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string WorkflowName { get; private set; }

    /// <summary>"Leave", "Overtime", "Expense", "ShiftSwap"</summary>
    public string EntityType { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ApprovalStepDefinition> Steps => _steps.AsReadOnly();

    public static ApprovalWorkflowDefinition Create(string workflowName, string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        return new ApprovalWorkflowDefinition(Guid.NewGuid(), workflowName.Trim(), entityType.Trim());
    }

    public void AddStep(int sequence, ApproverRole role, bool isRequired = true,
        string? conditionExpression = null)
    {
        if (_steps.Any(s => s.Sequence == sequence))
            throw new InvalidOperationException($"Sequence {sequence} already defined.");

        _steps.Add(ApprovalStepDefinition.Create(Id, sequence, role, isRequired, conditionExpression));
        _steps.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// One step in the workflow. Optionally conditional (e.g. only execute if days > 5).
/// Condition is stored as a simple expression string to be evaluated by the resolver.
/// </summary>
public sealed class ApprovalStepDefinition : Entity<Guid>
{
    private ApprovalStepDefinition() { }

    private ApprovalStepDefinition(Guid id, Guid workflowId, int sequence,
        ApproverRole role, bool isRequired, string? conditionExpression) : base(id)
    {
        WorkflowId = workflowId;
        Sequence = sequence;
        Role = role;
        IsRequired = isRequired;
        ConditionExpression = conditionExpression;
    }

    public Guid WorkflowId { get; private set; }
    public int Sequence { get; private set; }
    public ApproverRole Role { get; private set; }
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Simple condition evaluated by resolver. Examples:
    ///   "ActualDays > 5"
    ///   "EmployeeGrade >= Director"
    ///   null → always include this step
    /// </summary>
    public string? ConditionExpression { get; private set; }

    internal static ApprovalStepDefinition Create(Guid workflowId, int sequence,
        ApproverRole role, bool isRequired, string? conditionExpression)
        => new(Guid.NewGuid(), workflowId, sequence, role, isRequired, conditionExpression);
}
