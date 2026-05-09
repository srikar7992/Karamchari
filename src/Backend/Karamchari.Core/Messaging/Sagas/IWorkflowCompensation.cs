namespace Karamchari.Core.Messaging.Sagas;

/// <summary>
/// Marker interface for commands that represent a compensating action
/// for a failed long-running process step.
/// </summary>
public interface IWorkflowCompensation
{
    /// <summary>Correlation identifier for the saga.</summary>
    Guid CorrelationId { get; }

    /// <summary>Reason for compensation.</summary>
    string Reason { get; }
}
