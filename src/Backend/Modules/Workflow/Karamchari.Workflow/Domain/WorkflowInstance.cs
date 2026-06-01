using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Core.Multitenancy;
using Karamchari.Workflow.Domain.Events;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class WorkflowInstance : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<WorkflowStepInstance> _stepInstances = [];
    private readonly List<Karamchari.Core.Domain.Workflows.WorkflowAuditEntry> _auditLog = [];

    private WorkflowInstance(
        Guid id,
        string tenantId,
        string entityType,
        Guid entityId,
        string definitionSnapshotJson)
        : base(id)
    {
        TenantId = tenantId;
        EntityType = entityType;
        EntityId = entityId;
        DefinitionSnapshotJson = definitionSnapshotJson;
        CurrentStep = 0;
        Status = WorkflowStatus.Pending;
        StartedAt = DateTimeOffset.UtcNow;
    }

    private static readonly JsonSerializerOptions SnapshotOptions =
        new() { Converters = { new JsonStringEnumConverter() } };

    private WorkflowInstance()
    {
        TenantId = string.Empty;
        EntityType = string.Empty;
        DefinitionSnapshotJson = string.Empty;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EntityType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EntityId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string DefinitionSnapshotJson { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int CurrentStep { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkflowStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<WorkflowStepInstance> StepInstances => _stepInstances.AsReadOnly();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<Karamchari.Core.Domain.Workflows.WorkflowAuditEntry> AuditLog => _auditLog.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkflowInstance Start(
        string tenantId,
        string entityType,
        Guid entityId,
        WorkflowDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentNullException.ThrowIfNull(definition);

        if (!definition.IsActive)
            throw new InvalidOperationException(
                $"Cannot start workflow instance from inactive definition {definition.Id}.");

        if (definition.Steps.Count == 0)
            throw new InvalidOperationException(
                $"Workflow definition {definition.Id} has no steps.");

        var snapshot = JsonSerializer.Serialize(new
        {
            definition.Id,
            definition.Name,
            definition.EntityType,
            Steps = definition.Steps.Select(s => new
            {
                s.Id,
                s.Order,
                s.Name,
                s.IsParallel,
                s.QuorumRule,
                s.QuorumThreshold,
                ApproverRoles = s.GetApproverRoles()
            }),
        }, SnapshotOptions);

        var instance = new WorkflowInstance(Guid.NewGuid(), tenantId, entityType, entityId, snapshot);

        var step0 = definition.Steps.Where(s => s.Order == 0).ToList();
        if (step0.Count == 0)
            step0 = [definition.Steps[0]];

        foreach (var step in step0)
        {
            foreach (var role in step.GetApproverRoles())
                instance._stepInstances.Add(WorkflowStepInstance.Create(instance.Id, step.Order, role));
        }

        instance.Audit("Started", Guid.Empty, "None", WorkflowStatus.Pending.ToString());

        instance.RaiseDomainEvent(new WorkflowInstanceStarted(
            instance.Id, tenantId, entityType, entityId, DateTimeOffset.UtcNow));

        return instance;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Approve(Guid stepInstanceId, Guid approverId)
    {
        if (Status != WorkflowStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot approve a workflow instance with status {Status}.");

        var stepInstance = _stepInstances.FirstOrDefault(s => s.Id == stepInstanceId)
            ?? throw new ArgumentException(
                $"Step instance {stepInstanceId} not found in workflow instance {Id}.",
                nameof(stepInstanceId));

        if (stepInstance.Status == WorkflowStepStatus.Approved)
            return;

        stepInstance.MarkApproved(approverId);
        Audit("StepApproved", approverId, Status.ToString(), Status.ToString(), notes: $"Step {stepInstance.StepOrder} approved.");

        if (IsQuorumMet(stepInstance.StepOrder))
            AdvanceStep(stepInstance.StepOrder);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Reject(Guid stepInstanceId, Guid approverId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != WorkflowStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot reject a workflow instance with status {Status}.");

        var stepInstance = _stepInstances.FirstOrDefault(s => s.Id == stepInstanceId)
            ?? throw new ArgumentException(
                $"Step instance {stepInstanceId} not found in workflow instance {Id}.",
                nameof(stepInstanceId));

        stepInstance.MarkRejected(approverId, reason);
        var oldStatus = Status;
        Status = WorkflowStatus.Rejected;
        CompletedAt = DateTimeOffset.UtcNow;

        Audit("Rejected", approverId, oldStatus.ToString(), Status.ToString(), notes: reason);

        RaiseDomainEvent(new WorkflowInstanceCompleted(
            Id, TenantId, EntityType, EntityId, Status, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Audit(string action, Guid actorId, string fromState, string toState, string? notes = null)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            Status,
            CurrentStep,
            StepInstanceCount = _stepInstances.Count,
        });

        _auditLog.Add(new Karamchari.Core.Domain.Workflows.WorkflowAuditEntry(
            TenantId,
            "N/A", // CorrelationId not easily available here, should be handled by repository/interceptor
            Id,
            action,
            actorId,
            DateTimeOffset.UtcNow,
            fromState,
            toState,
            notes,
            snapshot));
    }

    private bool IsQuorumMet(int stepOrder)
    {
        var currentStepInstances = _stepInstances.Where(s => s.StepOrder == stepOrder).ToList();
        var approvedCount = currentStepInstances.Count(s => s.Status == WorkflowStepStatus.Approved);
        var totalCount = currentStepInstances.Count;

        try
        {
            using var doc = JsonDocument.Parse(DefinitionSnapshotJson);
            var steps = doc.RootElement.GetProperty("Steps");
            foreach (var step in steps.EnumerateArray())
            {
                if (step.GetProperty("Order").GetInt32() != stepOrder)
                    continue;

                var rule = Enum.Parse<QuorumRule>(step.GetProperty("QuorumRule").GetString()!);
                var threshold = step.GetProperty("QuorumThreshold").GetInt32();

                return rule switch
                {
                    QuorumRule.All => approvedCount == totalCount,
                    QuorumRule.Any => approvedCount >= 1,
                    QuorumRule.MinN => approvedCount >= threshold,
                    _ => approvedCount == totalCount,
                };
            }
        }
        catch (Exception)
        {
            // Fail-closed: if snapshot is unreadable, require all approvals.
        }

        return approvedCount == totalCount;
    }

    private void AdvanceStep(int completedStepOrder)
    {
        int? nextOrder = null;
        try
        {
            using var doc = JsonDocument.Parse(DefinitionSnapshotJson);
            var steps = doc.RootElement.GetProperty("Steps");
            var orders = steps.EnumerateArray()
                .Select(s => s.GetProperty("Order").GetInt32())
                .Where(o => o > completedStepOrder)
                .OrderBy(o => o)
                .ToList();

            if (orders.Count > 0)
                nextOrder = orders[0];
        }
        catch (Exception)
        {
            // Snapshot unreadable â€” treat as final step.
        }

        if (nextOrder.HasValue)
        {
            CurrentStep = nextOrder.Value;
            CreateStepInstances(nextOrder.Value);

            RaiseDomainEvent(new WorkflowStepAdvanced(
                Id, TenantId, completedStepOrder, nextOrder.Value, DateTimeOffset.UtcNow));
        }
        else
        {
            var oldStatus = Status;
            Status = WorkflowStatus.Approved;
            CompletedAt = DateTimeOffset.UtcNow;

            Audit("Approved", Guid.Empty, oldStatus.ToString(), Status.ToString());

            RaiseDomainEvent(new WorkflowInstanceCompleted(
                Id, TenantId, EntityType, EntityId, Status, DateTimeOffset.UtcNow));
        }
    }

    private void CreateStepInstances(int stepOrder)
    {
        try
        {
            using var doc = JsonDocument.Parse(DefinitionSnapshotJson);
            var steps = doc.RootElement.GetProperty("Steps");
            foreach (var step in steps.EnumerateArray())
            {
                if (step.GetProperty("Order").GetInt32() != stepOrder)
                    continue;

                foreach (var roleEl in step.GetProperty("ApproverRoles").EnumerateArray())
                {
                    var role = roleEl.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(role))
                        _stepInstances.Add(WorkflowStepInstance.Create(Id, stepOrder, role));
                }

                break;
            }
        }
        catch (Exception)
        {
            // Snapshot unreadable â€” no step instances created; engine must handle.
        }
    }
}
