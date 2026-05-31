namespace Karamchari.Core.Domain.Workflows;

/// <summary>
/// Canonical workflow statuses for platform-wide convergence.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Workflow is active and waiting for action.</summary>
    Pending = 1,
    /// <summary>Workflow has been fully approved.</summary>
    Approved = 2,
    /// <summary>Workflow has been rejected.</summary>
    Rejected = 3,
    /// <summary>Workflow has been escalated to a higher authority.</summary>
    Escalated = 4,
    /// <summary>Workflow has been cancelled by the initiator or system.</summary>
    Cancelled = 5,
    /// <summary>Workflow has reached its final state (either Approved or Rejected or processed).</summary>
    Completed = 6
}
