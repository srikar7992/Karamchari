using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain;

public sealed class WorkflowStep : Entity<Guid>
{
    private WorkflowStep(
        Guid id,
        Guid definitionId,
        int order,
        string name,
        bool isParallel,
        QuorumRule quorumRule,
        int quorumThreshold,
        string approverRolesJson,
        string? conditionJson)
        : base(id)
    {
        DefinitionId = definitionId;
        Order = order;
        Name = name;
        IsParallel = isParallel;
        QuorumRule = quorumRule;
        QuorumThreshold = quorumThreshold;
        ApproverRolesJson = approverRolesJson;
        ConditionJson = conditionJson;
    }

    private WorkflowStep() { Name = string.Empty; ApproverRolesJson = "[]"; }

    public Guid DefinitionId { get; private set; }
    public int Order { get; private set; }
    public string Name { get; private set; }
    public bool IsParallel { get; private set; }
    public QuorumRule QuorumRule { get; private set; }
    public int QuorumThreshold { get; private set; }
    public string? ConditionJson { get; private set; }

    // Stored as a JSON array in the DB column; deserialized by the service layer when needed.
    public string ApproverRolesJson { get; private set; }

    public static WorkflowStep Create(
        Guid definitionId,
        int order,
        string name,
        bool isParallel,
        QuorumRule quorum,
        int quorumThreshold,
        IEnumerable<string> approverRoles,
        string? conditionJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(approverRoles);

        var roles = approverRoles.ToList();
        if (roles.Count == 0)
            throw new ArgumentException("At least one approver role is required.", nameof(approverRoles));

        var rolesJson = System.Text.Json.JsonSerializer.Serialize(roles);

        return new WorkflowStep(Guid.NewGuid(), definitionId, order, name.Trim(),
            isParallel, quorum, quorumThreshold, rolesJson, conditionJson);
    }

    public IReadOnlyList<string> GetApproverRoles() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(ApproverRolesJson) ?? [];
}
