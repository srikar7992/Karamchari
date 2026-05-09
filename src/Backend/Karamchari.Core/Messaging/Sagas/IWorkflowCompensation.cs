namespace Karamchari.Core.Messaging.Sagas;

/// <summary>
/// Marker interface for commands that represent a compensating action
/// for a failed long-running process step.
/// </summary>
public interface IWorkflowCompensation
{
    Guid CorrelationId { get; }
    string Reason { get; }
}
