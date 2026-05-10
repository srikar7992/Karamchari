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
    private readonly ILogger<TenantJobContextSerializer> _logger;

    public TenantJobContextSerializer(ILogger<TenantJobContextSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Serialize(TenantExecutionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var json = envelope.ToJson();
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public TenantExecutionEnvelope Deserialize(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            throw new StaleTenantRestoreException("Cannot deserialize empty tenant context.");
        }

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(serialized));
            var envelope = TenantExecutionEnvelope.FromJson(json);
            if (envelope == null)
            {
                throw new StaleTenantRestoreException("Failed to deserialize tenant execution envelope.");
            }

            return envelope;
        }
        catch (Exception ex) when (ex is not StaleTenantRestoreException)
        {
            throw new StaleTenantRestoreException("Invalid serialized tenant context format.", ex);
        }
    }

    public bool TryDeserialize(string serialized, out TenantExecutionEnvelope? envelope)
    {
        envelope = null;
        if (string.IsNullOrWhiteSpace(serialized)) return false;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(serialized));
            envelope = TenantExecutionEnvelope.FromJson(json);
            return envelope != null;
        }
        catch
        {
            return false;
        }
    }

    public bool IsStale(string serializedContext, TimeSpan maxAge)
    {
        if (!TryDeserialize(serializedContext, out var envelope)) return true;

        var age = DateTime.UtcNow - envelope!.Timestamp;
        return age > maxAge;
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
