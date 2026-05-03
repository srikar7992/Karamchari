namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Thrown when the active request cannot be associated with a tenant —
/// missing JWT claim, malformed tenant id, or disagreement between sources
/// (JWT vs. subdomain vs. trusted header).
///
/// The API exception handler maps this to <c>401 Unauthorized</c> when the
/// caller is unauthenticated and <c>403 Forbidden</c> when authenticated but
/// targeting a tenant they don't belong to.
/// </summary>
public sealed class TenantResolutionException : Exception
{
    public TenantResolutionException(string message) : base(message) { }

    public TenantResolutionException(string message, Exception innerException)
        : base(message, innerException) { }

    public TenantResolutionException(string message, TenantResolutionFailureReason reason)
        : base(message)
    {
        Reason = reason;
    }

    public TenantResolutionFailureReason Reason { get; init; } = TenantResolutionFailureReason.Unspecified;
}

/// <summary>Why tenant resolution failed. Exposed for telemetry and exception mapping; not for business logic branching.</summary>
public enum TenantResolutionFailureReason
{
    Unspecified = 0,
    MissingJwtClaim = 1,
    InvalidTenantIdFormat = 2,
    SourcesDisagree = 3,
    TenantDisabled = 4,
    TenantNotFound = 5,
    UntrustedHeaderSource = 6,
}
