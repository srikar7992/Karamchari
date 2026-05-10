using System.Globalization;
using System.Text.RegularExpressions;

namespace Karamchari.Core.Multitenancy.Execution;

/// <summary>
/// An <see cref="IDisposable"/> scope that manages tenant execution context nesting,
/// saving the current context on entry and restoring it on disposal.
/// </summary>
public sealed partial class TenantExecutionScope : IDisposable
{
    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    private readonly TenantExecutionContextAccessor _accessor;
    private readonly TenantExecutionEnvelope? _savedContext;
    private readonly int _savedDepth;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantExecutionScope"/> class.
    /// </summary>
    /// <param name="accessor">The accessor to manage.</param>
    /// <param name="childContext">An optional child context to use within this scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when accessor is null.</exception>
    public TenantExecutionScope(
        TenantExecutionContextAccessor accessor,
        TenantExecutionEnvelope? childContext = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        _accessor = accessor;
        _savedContext = accessor.Current;
        _savedDepth = accessor.NestingDepth;

        if (childContext is not null)
        {
            ValidateTenantId(childContext.TenantId);
            accessor.SetCurrent(childContext);
        }
    }

    /// <summary>
    /// Gets the current nesting depth within this scope.
    /// </summary>
    public int Depth => _accessor.NestingDepth - _savedDepth;

    /// <summary>
    /// Gets the saved parent context that will be restored on disposal.
    /// </summary>
    public TenantExecutionEnvelope? SavedContext => _savedContext;

    private static void ValidateTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id cannot be null or whitespace.",
                nameof(tenantId));
        }

        if (!TenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                $"Tenant id '{tenantId}' is invalid. Must match {TenantConstants.TenantIdPattern}.",
                nameof(tenantId));
        }
    }

    /// <summary>
    /// Restores the parent execution context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_savedContext is null)
        {
            _accessor.Clear();
        }
        else
        {
            _accessor.SetCurrent(_savedContext);
        }
    }
}
