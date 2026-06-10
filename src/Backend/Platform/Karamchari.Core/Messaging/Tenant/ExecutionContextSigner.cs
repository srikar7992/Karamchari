// -----------------------------------------------------------------------
// <copyright file="ExecutionContextSigner.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// Provides cryptographic signing and validation for execution context metadata
/// to ensure integrity across asynchronous boundaries.
/// </summary>
public sealed class ExecutionContextSigner
{
    private readonly byte[][] _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextSigner"/> class.
    /// </summary>
    /// <param name="secrets">
    /// The secret keys used for HMAC generation. The first secret is the primary key used for signing new messages.
    /// Subsequent secrets are retained for validation during key rotation periods to ensure in-flight messages survive.
    /// </param>
    public ExecutionContextSigner(IEnumerable<string> secrets)
    {
        if (secrets == null || !secrets.Any())
            throw new ArgumentException("At least one signing secret must be provided", nameof(secrets));

        _keys = secrets
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => Encoding.UTF8.GetBytes(s))
            .ToArray();

        if (_keys.Length == 0)
            throw new ArgumentException("At least one valid signing secret must be provided", nameof(secrets));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextSigner"/> class with a single secret.
    /// </summary>
    /// <param name="secret">The secret key used for HMAC generation.</param>
    public ExecutionContextSigner(string secret) : this(new[] { secret })
    {
    }

    /// <summary>
    /// Generates a signature for the provided execution context metadata using the primary (newest) key.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="sourceService">The producing service identity.</param>
    /// <param name="timestamp">The creation timestamp.</param>
    /// <returns>A base64-encoded HMACSHA256 signature.</returns>
    public string Sign(string tenantId, string correlationId, string conversationId, string sourceService, string timestamp)
    {
        return GenerateSignature(_keys[0], tenantId, correlationId, conversationId, sourceService, timestamp);
    }

    /// <summary>
    /// Validates the signature for the provided execution context metadata against all configured keys.
    /// This ensures messages signed before a key rotation remain valid during the rotation window.
    /// </summary>
    public bool Validate(string signature, string tenantId, string correlationId, string conversationId, string sourceService, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;

        var signatureBytes = Convert.FromBase64String(signature);

        foreach (var key in _keys)
        {
            var computed = GenerateSignature(key, tenantId, correlationId, conversationId, sourceService, timestamp);
            if (CryptographicOperations.FixedTimeEquals(signatureBytes, Convert.FromBase64String(computed)))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateSignature(byte[] key, string tenantId, string correlationId, string conversationId, string sourceService, string timestamp)
    {
        var canonicalString = Canonicalize(tenantId, correlationId, conversationId, sourceService, timestamp);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalString));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Transforms metadata into a deterministic, alphabetically-sorted string for signing.
    /// </summary>
    private static string Canonicalize(string tenantId, string correlationId, string conversationId, string sourceService, string timestamp)
    {
        // Alphabetical order: ConversationId, CorrelationId, SourceService, TenantId, Timestamp
        return $"con={conversationId}|cor={correlationId}|src={sourceService}|tn={tenantId}|ts={timestamp}";
    }
}
