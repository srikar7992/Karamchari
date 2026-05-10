namespace Karamchari.TenantIsolationCertification.Infrastructure;

public sealed class TenantTestContext : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly Dictionary<string, string> _tenantSchemas = new();
    private readonly List<string> _createdTenants = new();

    public string ActiveTenantId { get; private set; } = string.Empty;
    public string ActiveSchema => $"tenant_{ActiveTenantId}";
    public Karamchari.Core.Caching.Tenant.TenantSource ActiveSource { get; private set; }

    public static TenantTestContext Create(string tenantId, Karamchari.Core.Caching.Tenant.TenantSource source = Karamchari.Core.Caching.Tenant.TenantSource.JwtClaim)
    {
        var ctx = new TenantTestContext
        {
            ActiveTenantId = tenantId.ToLowerInvariant(),
            ActiveSource = source
        };
        ctx._tenantSchemas[tenantId.ToLowerInvariant()] = $"tenant_{tenantId.ToLowerInvariant()}";
        return ctx;
    }

    public void AddCreatedTenant(string tenantId)
    {
        _createdTenants.Add(tenantId.ToLowerInvariant());
        _tenantSchemas[tenantId.ToLowerInvariant()] = $"tenant_{tenantId.ToLowerInvariant()}";
    }

    public IReadOnlyList<string> GetCreatedTenants() => _createdTenants.AsReadOnly();

    public string GetSchemaForTenant(string tenantId)
    {
        var normalized = tenantId.ToLowerInvariant();
        if (_tenantSchemas.TryGetValue(normalized, out var schema))
            return schema;

        schema = $"tenant_{normalized}";
        _tenantSchemas[normalized] = schema;
        return schema;
    }

    public void RegisterDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            try { disposable.Dispose(); }
            catch { }
        }
        _disposables.Clear();
    }

    public override string ToString() => $"TenantContext(TenantId={ActiveTenantId}, Schema={ActiveSchema}, Source={ActiveSource})";
}
