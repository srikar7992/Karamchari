using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// Serializes and deserializes tenant execution envelopes for background job processing.
/// Handles conversion between TenantExecutionEnvelope and a serializable format with versioning and validation.
/// </summary>
public sealed class TenantJobContextSerializer : ITenantJobContextSerializer
{
    /// <summary>
    /// Regular expression for validating tenant identifiers based on the tenant ID pattern.
    /// </summary>
    private static readonly Regex TenantIdRegex = new(TenantConstants.TenantIdPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    /// <summary>
    /// Logger instance for logging serialization and deserialization operations.
    /// </summary>
    private readonly ILogger<TenantJobContextSerializer> _logger;

    /// <summary>
    /// Current schema version for the serialized tenant context format.
    /// </summary>
    private const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Initializes a new instance of the TenantJobContextSerializer class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public TenantJobContextSerializer(ILogger<TenantJobContextSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Serializes a tenant execution envelope to a Base64-encoded JSON string.
    /// </summary>
    /// <param name="envelope">The tenant execution envelope to serialize.</param>
    /// <returns>A Base64-encoded JSON string representing the serialized tenant context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when envelope is null.</exception>
    public string Serialize(TenantExecutionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var wrapper = TenantAwareJobEnvelope.FromEnvelope(envelope);
        var payload = new JobContextPayload
        {
            SchemaVersion = CurrentSchemaVersion,
            Envelope = wrapper,
            SerializedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Serialized tenant context for {TenantId}, schema v{SchemaVersion}",
                envelope.TenantId,
                CurrentSchemaVersion);
        }

        return base64;
    }

    /// <summary>
    /// Deserializes a Base64-encoded JSON string to a tenant execution envelope.
    /// </summary>
    /// <param name="serialized">The Base64-encoded JSON string representing the serialized tenant context.</param>
    /// <returns>A tenant execution envelope reconstructed from the serialized data.</returns>
    /// <exception cref="StaleTenantRestoreException">Thrown when the serialized context is null, empty, or invalid.</exception>
    public TenantExecutionEnvelope Deserialize(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            throw new StaleTenantRestoreException(
                "Cannot deserialize: serialized context is null or empty.");
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(serialized));
            var payload = JsonSerializer.Deserialize<JobContextPayload>(json);

            if (payload == null)
            {
                throw new StaleTenantRestoreException("Deserialized payload is null.");
            }

            ValidateSchemaVersion(payload.SchemaVersion);
            ValidateEnvelope(payload.Envelope);

            var envelope = payload.Envelope.ToEnvelope();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Deserialized tenant context for {TenantId}",
                    envelope.TenantId);
            }

            return envelope;
        }
        catch (FormatException ex)
        {
            throw new StaleTenantRestoreException(
                "Invalid Base64 encoding in serialized context.", ex);
        }
        catch (JsonException ex)
        {
            throw new StaleTenantRestoreException(
                "Invalid JSON format in serialized context.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize the serialized tenant context without throwing exceptions.
    /// </summary>
    /// <param name="serialized">The Base64-encoded JSON string representing the serialized tenant context.</param>
    /// <param name="envelope">When this method returns, contains the deserialized tenant execution envelope if the operation succeeded, or null if it failed.</param>
    /// <returns>true if the serialized context was successfully deserialized; otherwise, false.</returns>
    public bool TryDeserialize(string serialized, out TenantExecutionEnvelope? envelope)
    {
        envelope = null;

        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        try
        {
            envelope = Deserialize(serialized);
            return true;
        }
        catch (StaleTenantRestoreException)
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the specified serialized tenant context is stale based on the maximum age.
    /// </summary>
    /// <param name="serializedContext">The Base64-encoded JSON string representing the serialized tenant context.</param>
    /// <param name="maxAge">The maximum age after which the context is considered stale.</param>
    /// <returns>true if the context is stale or invalid; otherwise, false.</returns>
    public bool IsStale(string serializedContext, TimeSpan maxAge)
    {
        if (string.IsNullOrWhiteSpace(serializedContext))
        {
            return true;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(serializedContext));
            var payload = JsonSerializer.Deserialize<JobContextPayload>(json);

            if (payload == null || payload.SerializedAt == default)
            {
                return true;
            }

            return DateTime.UtcNow - payload.SerializedAt > maxAge;
        }
        catch
        {
            return true;
        }
    }

    private void ValidateSchemaVersion(int version)
    {
        if (version < 1 || version > CurrentSchemaVersion)
        {
            throw new StaleTenantRestoreException(
                string.Create(CultureInfo.InvariantCulture,
                    $"Unsupported schema version {version}. Supported range: 1-{CurrentSchemaVersion}."));
        }
    }

    private void ValidateEnvelope(TenantAwareJobEnvelope envelope)
    {
        if (envelope == null)
        {
            throw new StaleTenantRestoreException("Envelope in payload is null.");
        }

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

        if (envelope.CorrelationId == Guid.Empty)
        {
            throw new StaleTenantRestoreException("CorrelationId in envelope is empty.");
        }

        if (envelope.RequestId == Guid.Empty)
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
