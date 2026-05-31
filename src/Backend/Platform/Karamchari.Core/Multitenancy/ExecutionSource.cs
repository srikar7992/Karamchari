namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Indicates the origin of a tenant execution, used for diagnostics, auditing,
/// and source-agreement validation across the platform.
/// </summary>
public enum ExecutionSource : byte
{
    /// <summary>Originated from an HTTP request.</summary>
    HttpRequest = 0,

    /// <summary>Originated from a message consumer (MassTransit or similar).</summary>
    MessageConsumer = 1,

    /// <summary>Originated from a background job (Hangfire or similar).</summary>
    BackgroundJob = 2,

    /// <summary>Originated from a saga orchestration.</summary>
    SagaOrchestration = 3,

    /// <summary>Originated from a database migration.</summary>
    Migration = 4,

    /// <summary>Manually established (e.g., in tests or admin scenarios).</summary>
    Manual = 5,

    /// <summary>Execution within a unit/integration test.</summary>
    Test = 6,
}
