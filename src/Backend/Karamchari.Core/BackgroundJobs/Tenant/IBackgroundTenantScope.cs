using Karamchari.Core.Multitenancy.Execution;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public interface IBackgroundTenantScope
{
    TenantExecutionEnvelope GetCurrentEnvelope();
    Task<TenantExecutionEnvelope> GetEnvelopeAsync(CancellationToken ct);
    string CurrentTenantId { get; }
    bool IsValid { get; }
}
