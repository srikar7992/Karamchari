using System.Globalization;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public sealed partial record TenantAwareJobEnvelope
{
    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    public string TenantId { get; init; } = string.Empty;
    public Guid CorrelationId { get; init; }
    public Guid RequestId { get; init; }
    public ExecutionSource Source { get; init; }
    public int RetryAttempt { get; init; }
    public DateTime Timestamp { get; init; }
    public string? UserIdentity { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public string SerializedContext { get; init; } = string.Empty;

    public TenantExecutionEnvelope ToEnvelope()
    {
        if (!TenantIdRegex().IsMatch(TenantId))
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture,
                    $"Cannot restore envelope: TenantId '{TenantId}' is invalid."));
        }

        return new TenantExecutionEnvelope(
            TenantId,
            CorrelationId,
            RequestId,
            Source)
        {
            RetryAttempt = RetryAttempt,
            Timestamp = Timestamp,
            UserIdentity = UserIdentity,
            ReplayMetadata = Metadata
        };
    }

    public static TenantAwareJobEnvelope FromEnvelope(TenantExecutionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new TenantAwareJobEnvelope
        {
            TenantId = envelope.TenantId,
            CorrelationId = envelope.CorrelationId,
            RequestId = envelope.RequestId,
            Source = envelope.ExecutionSource,
            RetryAttempt = envelope.RetryAttempt,
            Timestamp = envelope.Timestamp,
            UserIdentity = envelope.UserIdentity,
            Metadata = envelope.ReplayMetadata
        };
    }
}
