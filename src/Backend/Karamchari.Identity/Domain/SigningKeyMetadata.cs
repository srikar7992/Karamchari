namespace Karamchari.Identity.Domain;

/// <summary>
/// Metadata for a cryptographic signing key used for JWT issuance and validation.
/// Supports rotation by tracking activation and expiration timestamps.
/// </summary>
public sealed class SigningKeyMetadata
{
    private SigningKeyMetadata()
    {
        // EF Core
        KeyId = string.Empty;
        Secret = string.Empty;
    }

    private SigningKeyMetadata(
        string keyId,
        string secret,
        DateTimeOffset activatedAt,
        DateTimeOffset? expiresAt)
    {
        KeyId = keyId;
        Secret = secret;
        ActivatedAt = activatedAt;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the unique key identifier (kid).</summary>
    public string KeyId { get; private set; }

    /// <summary>Gets the base64-encoded secret or public key material.</summary>
    public string Secret { get; private set; }

    /// <summary>Gets the timestamp when the key becomes valid for signing.</summary>
    public DateTimeOffset ActivatedAt { get; private set; }

    /// <summary>Gets the optional timestamp when the key should no longer be used for signing.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets a value indicating whether the key is currently active for signing.</summary>
    public bool IsActiveForSigning => ActivatedAt <= DateTimeOffset.UtcNow && (ExpiresAt == null || ExpiresAt > DateTimeOffset.UtcNow);

    /// <summary>Creates a new signing key metadata record.</summary>
    public static SigningKeyMetadata Create(string keyId, string secret, DateTimeOffset activatedAt, DateTimeOffset? expiresAt = null)
    {
        return new SigningKeyMetadata(keyId, secret, activatedAt, expiresAt);
    }
}
