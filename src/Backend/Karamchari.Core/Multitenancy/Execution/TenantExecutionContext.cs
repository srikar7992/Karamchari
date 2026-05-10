using System.Diagnostics;

namespace Karamchari.Core.Multitenancy.Execution;

/// <summary>
/// Runtime container for tenant execution metadata, preserved across async boundaries.
/// Acts as the unified access point for TenantId, CorrelationId, and security context
/// for the current thread of execution.
/// </summary>
public sealed class TenantExecutionContext
{
    private static readonly AsyncLocal<TenantExecutionContext?> _current = new();

    /// <summary>
    /// Gets the current <see cref="TenantExecutionContext"/> for this async flow.
    /// </summary>
    public static TenantExecutionContext? Current => _current.Value;

    /// <summary>
    /// Gets the current tenant identifier, if established.
    /// </summary>
    public static string? CurrentTenantId => _current.Value?.TenantId;

    /// <summary>
    /// The immutable envelope containing all metadata for this execution.
    /// </summary>
    public TenantExecutionEnvelope Envelope { get; }

    /// <summary>
    /// The tenant identifier for this execution.
    /// </summary>
    public string TenantId => Envelope.TenantId;

    /// <summary>
    /// The correlation ID for cross-service request tracking.
    /// </summary>
    public string CorrelationId => Envelope.CorrelationId;

    /// <summary>
    /// The source of this execution (API, Consumer, Job, etc.).
    /// </summary>
    public ExecutionSource Source => Envelope.ExecutionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantExecutionContext"/> class.
    /// </summary>
    /// <param name="envelope">The execution envelope.</param>
    public TenantExecutionContext(TenantExecutionEnvelope envelope)
    {
        Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
    }

    /// <summary>
    /// Establishes this context as the current one for the async flow.
    /// </summary>
    /// <returns>A scope that restores the previous context when disposed.</returns>
    public IDisposable Establish()
    {
        var previous = _current.Value;
        _current.Value = this;
        return new ScopeRemover(previous);
    }

    /// <summary>
    /// Clears the current tenant execution context.
    /// </summary>
    public static void Clear()
    {
        _current.Value = null;
    }

    private sealed class ScopeRemover : IDisposable
    {
        private readonly TenantExecutionContext? _previous;

        public ScopeRemover(TenantExecutionContext? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            _current.Value = _previous;
        }
    }
}
