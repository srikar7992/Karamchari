// -----------------------------------------------------------------------
// <copyright file="TenantTestContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.TenantIsolationCertification.Infrastructure;

/// <summary>
/// Infrastructure helper for managing tenant context during isolation testing.
/// </summary>
public sealed class TenantTestContext : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly Dictionary<string, string> _tenantSchemas = new();
    private readonly List<string> _createdTenants = new();

    /// <summary>Gets the currently active tenant identifier for the test.</summary>
    public string ActiveTenantId { get; private set; } = string.Empty;

    /// <summary>Gets the expected schema name for the active tenant.</summary>
    public string ActiveSchema => $"tenant_{ActiveTenantId}";

    /// <summary>Gets the source of the tenant context.</summary>
    public TenantSource ActiveSource { get; private set; }

    /// <summary>
    /// Creates a new test context for a specific tenant.
    /// </summary>
    public static TenantTestContext Create(string tenantId, TenantSource source = TenantSource.JwtClaim)
    {
        var ctx = new TenantTestContext
        {
            ActiveTenantId = tenantId.ToLowerInvariant(),
            ActiveSource = source
        };
        ctx._tenantSchemas[tenantId.ToLowerInvariant()] = $"tenant_{tenantId.ToLowerInvariant()}";
        return ctx;
    }

    /// <summary>
    /// Tracks a tenant created during the test for cleanup or verification.
    /// </summary>
    public void AddCreatedTenant(string tenantId)
    {
        _createdTenants.Add(tenantId.ToLowerInvariant());
        _tenantSchemas[tenantId.ToLowerInvariant()] = $"tenant_{tenantId.ToLowerInvariant()}";
    }

    /// <summary>
    /// Gets the collection of tenants created during the test.
    /// </summary>
    public IReadOnlyList<string> GetCreatedTenants() => _createdTenants.AsReadOnly();

    /// <summary>
    /// Resolves the schema name for a specific tenant.
    /// </summary>
    public string GetSchemaForTenant(string tenantId)
    {
        var normalized = tenantId.ToLowerInvariant();
        if (_tenantSchemas.TryGetValue(normalized, out var schema))
            return schema;

        schema = $"tenant_{normalized}";
        _tenantSchemas[normalized] = schema;
        return schema;
    }

    /// <summary>
    /// Registers a resource for disposal when the test context is disposed.
    /// </summary>
    public void RegisterDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            try { disposable.Dispose(); }
            catch { }
        }
        _disposables.Clear();
    }

    /// <inheritdoc/>
    public override string ToString() => $"TenantContext(TenantId={ActiveTenantId}, Schema={ActiveSchema}, Source={ActiveSource})";
}
