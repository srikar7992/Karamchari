using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Domain.Workflows;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// A single step in a workflow coordination template.
/// </summary>
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
        string approverRolesJson)
        : base(id)
    {
        DefinitionId = definitionId;
        Order = order;
        Name = name;
        IsParallel = isParallel;
        QuorumRule = quorumRule;
        QuorumThreshold = quorumThreshold;
        ApproverRolesJson = approverRolesJson;
    }

    private WorkflowStep() { Name = string.Empty; ApproverRolesJson = "[]"; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid DefinitionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int Order { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsParallel { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public QuorumRule QuorumRule { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int QuorumThreshold { get; private set; }

    // Stored as a JSON array in the DB column; deserialized by the service layer when needed.
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ApproverRolesJson { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkflowStep Create(
        Guid definitionId,
        int order,
        string name,
        bool isParallel,
        QuorumRule quorum,
        int quorumThreshold,
        IEnumerable<string> approverRoles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(approverRoles);

        var roles = approverRoles.ToList();
        if (roles.Count == 0)
            throw new ArgumentException("At least one approver role is required.", nameof(approverRoles));

        var rolesJson = System.Text.Json.JsonSerializer.Serialize(roles);

        return new WorkflowStep(Guid.NewGuid(), definitionId, order, name.Trim(),
            isParallel, quorum, quorumThreshold, rolesJson);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<string> GetApproverRoles() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(ApproverRolesJson) ?? [];
}
