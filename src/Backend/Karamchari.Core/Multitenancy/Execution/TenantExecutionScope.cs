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
    private readonly IDisposable? _scope;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantExecutionScope"/> class.
    /// </summary>
    /// <param name="accessor">The accessor to manage.</param>
    /// <param name="childEnvelope">An optional child execution envelope to use within this scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when accessor is null.</exception>
    public TenantExecutionScope(
        TenantExecutionContextAccessor accessor,
        TenantExecutionEnvelope? childEnvelope = null)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

        if (childEnvelope is not null)
        {
            ValidateTenantId(childEnvelope.TenantId);
            var context = new TenantExecutionContext(childEnvelope);
            _scope = _accessor.Establish(context);
        }
    }

    /// <summary>
    /// Gets the saved parent context that will be restored on disposal.
    /// </summary>
    public TenantExecutionContext? ParentContext => _accessor.Current;

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
        _scope?.Dispose();
    }
}
