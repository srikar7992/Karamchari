using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public sealed class TenantJobContextSerializer : ITenantJobContextSerializer
{
    private static readonly Regex TenantIdRegex = new(TenantConstants.TenantIdPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private readonly ILogger<TenantJobContextSerializer> _logger;
    private const int CurrentSchemaVersion = 1;

    public TenantJobContextSerializer(ILogger<TenantJobContextSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

public interface ITenantJobContextSerializer
{
    string Serialize(TenantExecutionEnvelope envelope);
    TenantExecutionEnvelope Deserialize(string serialized);
    bool TryDeserialize(string serialized, out TenantExecutionEnvelope? envelope);
    bool IsStale(string serializedContext, TimeSpan maxAge);
}
