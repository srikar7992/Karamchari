using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// Implements high-fidelity serialization and deserialization for tenant execution contexts
/// in background job processing, ensuring integrity across service restarts and distributed nodes.
/// </summary>
public sealed class TenantJobContextSerializer : ITenantJobContextSerializer
{
    private static readonly Regex TenantIdRegex = new(TenantConstants.TenantIdPattern, RegexOptions.Compiled);
    private readonly ILogger<TenantJobContextSerializer> _logger;
    private const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantJobContextSerializer"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic tracing.</param>
    public TenantJobContextSerializer(ILogger<TenantJobContextSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string Serialize(TenantExecutionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var jobEnvelope = TenantAwareJobEnvelope.FromEnvelope(envelope);
        var payload = new JobContextPayload
        {
            SchemaVersion = CurrentSchemaVersion,
            Envelope = jobEnvelope,
            SerializedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <inheritdoc/>
    public TenantExecutionEnvelope Deserialize(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            throw new StaleTenantRestoreException("Cannot deserialize empty tenant context.");
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(serialized));
            var payload = JsonSerializer.Deserialize<JobContextPayload>(json);

            if (payload == null || payload.Envelope == null)
            {
                throw new StaleTenantRestoreException("Deserialized tenant context payload is null.");
            }

            ValidatePayload(payload);

            return payload.Envelope.ToEnvelope();
        }
        catch (Exception ex) when (ex is not StaleTenantRestoreException)
        {
            _logger.LogError(ex, "Failed to deserialize tenant job context.");
            throw new StaleTenantRestoreException("Invalid serialized tenant context format.", ex);
        }
    }

    /// <inheritdoc/>
    public bool TryDeserialize(string serialized, out TenantExecutionEnvelope? envelope)
    {
        envelope = null;
        if (string.IsNullOrWhiteSpace(serialized)) return false;

        try
        {
            envelope = Deserialize(serialized);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool IsStale(string serializedContext, TimeSpan maxAge)
    {
        if (string.IsNullOrWhiteSpace(serializedContext)) return true;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(serializedContext));
            var payload = JsonSerializer.Deserialize<JobContextPayload>(json);

            if (payload == null) return true;

            var age = DateTime.UtcNow - payload.SerializedAt;
            return age > maxAge;
        }
        catch
        {
            return true;
        }
    }

    private static void ValidatePayload(JobContextPayload payload)
    {
        if (payload.SchemaVersion != CurrentSchemaVersion)
        {
            throw new StaleTenantRestoreException(
                $"Unsupported tenant job context schema version: {payload.SchemaVersion}");
        }

        var envelope = payload.Envelope;

        if (string.IsNullOrWhiteSpace(envelope.TenantId))
        {
            throw new StaleTenantRestoreException("TenantId in envelope is null or empty.");
        }

        if (!TenantIdRegex.IsMatch(envelope.TenantId))
        {
            throw new StaleTenantRestoreException(
                string.Create(CultureInfo.InvariantCulture,
                    $"TenantId '{envelope.TenantId}' does not match expected pattern."));
        }

        if (string.IsNullOrWhiteSpace(envelope.CorrelationId))
        {
            throw new StaleTenantRestoreException("CorrelationId in envelope is empty.");
        }

        if (string.IsNullOrWhiteSpace(envelope.RequestId))
        {
            throw new StaleTenantRestoreException("RequestId in envelope is empty.");
        }
    }

    private sealed class JobContextPayload
    {
        public int SchemaVersion { get; set; }
        public TenantAwareJobEnvelope Envelope { get; set; } = new();
        public DateTime SerializedAt { get; set; }
    }
}

/// <summary>
/// Defines the contract for serializing and deserializing tenant execution envelopes in background job processing.
/// </summary>
public interface ITenantJobContextSerializer
{
    /// <summary>
    /// Serializes a tenant execution envelope to a Base64-encoded JSON string.
    /// </summary>
    /// <param name="envelope">The tenant execution envelope to serialize.</param>
    /// <returns>A Base64-encoded JSON string representing the serialized tenant context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when envelope is null.</exception>
    string Serialize(TenantExecutionEnvelope envelope);

    /// <summary>
    /// Deserializes a Base64-encoded JSON string to a tenant execution envelope.
    /// </summary>
    /// <param name="serialized">The Base64-encoded JSON string representing the serialized tenant context.</param>
    /// <returns>A tenant execution envelope reconstructed from the serialized data.</returns>
    /// <exception cref="StaleTenantRestoreException">Thrown when the serialized context is null, empty, or invalid.</exception>
    TenantExecutionEnvelope Deserialize(string serialized);

    /// <summary>
    /// Attempts to deserialize the serialized tenant context without throwing exceptions.
    /// </summary>
    /// <param name="serialized">The Base64-encoded JSON string representing the serialized tenant context.</param>
    /// <param name="envelope">When this method returns, contains the deserialized tenant execution envelope if the operation succeeded, or null if it failed.</param>
    /// <returns>true if the serialized context was successfully deserialized; otherwise, false.</returns>
    bool TryDeserialize(string serialized, out TenantExecutionEnvelope? envelope);

    /// <summary>
    /// Determines whether the specified serialized tenant context is stale based on the maximum age.
    /// </summary>
    /// <param name="serializedContext">The Base64-encoded JSON string representing the serialized tenant context.</param>
    /// <param name="maxAge">The maximum age after which the context is considered stale.</param>
    /// <returns>true if the context is stale or invalid; otherwise, false.</returns>
    bool IsStale(string serializedContext, TimeSpan maxAge);
}
