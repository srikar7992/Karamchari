using System.Globalization;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public sealed class StaleTenantRestoreException : Exception
{
    public string? TenantId { get; }
    public bool IsSecurityRelevant { get; }

    public StaleTenantRestoreException(string message) : base(message)
    {
        IsSecurityRelevant = true;
    }

    public StaleTenantRestoreException(string message, Exception innerException)
        : base(message, innerException)
    {
        IsSecurityRelevant = true;
    }

    public StaleTenantRestoreException(string message, string tenantId)
        : base(message)
    {
        TenantId = tenantId;
        IsSecurityRelevant = true;
    }

    public StaleTenantRestoreException(string message, string tenantId, Exception innerException)
        : base(message, innerException)
    {
        TenantId = tenantId;
        IsSecurityRelevant = true;
    }
}
