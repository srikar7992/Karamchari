using System.Globalization;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// Represents a tenant-aware job envelope that encapsulates tenant context for background job processing.
/// This record is used to serialize and deserialize tenant context for reliable job execution.
/// </summary>
public sealed partial record TenantAwareJobEnvelope
{
    /// <summary>
    /// Regular expression for validating tenant identifiers based on the tenant ID pattern.
    /// </summary>
    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    /// <summary>
    /// Gets or sets the tenant identifier associated with the job.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation identifier for tracking related operations across services.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets or sets the request identifier that originated the job.
    /// </summary>
    public Guid RequestId { get; init; }

    /// <summary>
    /// Gets or sets the execution source that indicates how the job was initiated.
    /// </summary>
    public ExecutionSource Source { get; init; }

    /// <summary>
    /// Gets or sets the number of retry attempts made for this job.
    /// </summary>
    public int RetryAttempt { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the job context was created or last updated.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the user identity associated with the job context, if available.
    /// </summary>
    public string? UserIdentity { get; init; }

    /// <summary>
    /// Gets or sets additional metadata associated with the tenant context.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Gets or sets the serialized context string used for persistence or transmission.
    /// </summary>
    public string SerializedContext { get; init; } = string.Empty;

    /// <summary>
    /// Converts the tenant-aware job envelope to a tenant execution envelope.
    /// Validates the tenant ID before conversion to ensure data integrity.
    /// </summary>
    /// <returns>A tenant execution envelope representing the same tenant context.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tenant ID is invalid.</exception>
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

    /// <summary>
    /// Creates a tenant-aware job envelope from a tenant execution envelope.
    /// </summary>
    /// <param name="envelope">The tenant execution envelope to convert from.</param>
    /// <returns>A new tenant-aware job envelope with the same context data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when envelope is null.</exception>
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
