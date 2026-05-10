using System.Globalization;

namespace Karamchari.Core.Multitenancy.Execution;

/// <summary>
/// AsyncLocal-based accessor that manages the current <see cref="TenantExecutionEnvelope"/>
/// across async boundaries, providing automatic propagation of tenant context.
/// </summary>
public sealed class TenantExecutionContextAccessor
{
    private readonly AsyncLocal<TenantExecutionEnvelope?> _asyncLocal;
    private readonly ThreadLocal<int> _nestingDepth;
    private readonly object _syncLock = new();

    private int _currentNestingDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantExecutionContextAccessor"/> class.
    /// </summary>
    public TenantExecutionContextAccessor()
    {
        _asyncLocal = new AsyncLocal<TenantExecutionEnvelope?>(OnAsyncLocalChanged);
        _nestingDepth = new ThreadLocal<int>(() => 0);
    }

    /// <summary>
    /// Gets the current execution envelope, or null if none is set.
    /// </summary>
    public TenantExecutionEnvelope? Current => _asyncLocal.Value;

    /// <summary>
    /// Gets the current nesting depth for diagnostics.
    /// </summary>
    public int NestingDepth
    {
        get
        {
            lock (_syncLock)
            {
                return _currentNestingDepth;
            }
        }
    }

    private void OnAsyncLocalChanged(AsyncLocalValueChangedArgs<TenantExecutionEnvelope?> _)
    {
        lock (_syncLock)
        {
            _currentNestingDepth = _nestingDepth.IsValueCreated
                ? _nestingDepth.Value
                : 0;
        }
    }

    /// <summary>
    /// Gets the current execution envelope, throwing if none is set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no execution context is available.</exception>
    public TenantExecutionEnvelope GetCurrent()
    {
        return _asyncLocal.Value
            ?? throw new InvalidOperationException(
                "No tenant execution context is available. Ensure the request is scoped within a tenant execution.");
    }

    /// <summary>
    /// Sets the current execution envelope.
    /// </summary>
    /// <param name="envelope">The execution envelope to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when envelope is null.</exception>
    public void SetCurrent(TenantExecutionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        _asyncLocal.Value = envelope;
        IncrementNestingDepth();
    }

    /// <summary>
    /// Clears the current execution envelope.
    /// </summary>
    public void Clear()
    {
        _asyncLocal.Value = null;
        DecrementNestingDepth();
    }

    private void IncrementNestingDepth()
    {
        lock (_syncLock)
        {
            var depth = _nestingDepth.Value;
            _nestingDepth.Value = depth + 1;
            _currentNestingDepth = depth + 1;
        }
    }

    private void DecrementNestingDepth()
    {
        lock (_syncLock)
        {
            var depth = _nestingDepth.Value;
            _nestingDepth.Value = Math.Max(0, depth - 1);
            _currentNestingDepth = Math.Max(0, depth - 1);
        }
    }
}
