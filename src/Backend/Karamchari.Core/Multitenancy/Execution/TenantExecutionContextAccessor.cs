using System.Globalization;

namespace Karamchari.Core.Multitenancy.Execution;

/// <summary>
/// AsyncLocal-based accessor that manages the current <see cref="TenantExecutionContext"/>
/// across async boundaries, providing automatic propagation of tenant context.
/// </summary>
public sealed class TenantExecutionContextAccessor
{
    /// <summary>
    /// Gets the current execution context, or null if none is set.
    /// </summary>
    public TenantExecutionContext? Current => TenantExecutionContext.Current;

    /// <summary>
    /// Gets the current execution context, throwing if none is set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no execution context is available.</exception>
    public TenantExecutionContext GetCurrent()
    {
        return Current
            ?? throw new InvalidOperationException(
                "No tenant execution context is available. Ensure the operation is scoped within a tenant execution.");
    }

    /// <summary>
    /// Establishes the provided context as current.
    /// </summary>
    /// <param name="context">The context to establish.</param>
    /// <returns>A scope that restores the previous context when disposed.</returns>
    public IDisposable Establish(TenantExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Establish();
    }

    /// <summary>
    /// Clears the current execution context.
    /// </summary>
    public void Clear()
    {
        TenantExecutionContext.Clear();
    }
}
