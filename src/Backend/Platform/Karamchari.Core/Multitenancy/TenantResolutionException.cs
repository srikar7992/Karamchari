// -----------------------------------------------------------------------
// <copyright file="TenantResolutionException.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Thrown when the active request cannot be associated with a tenant Ã¢â‚¬â€
/// missing JWT claim, malformed tenant id, or disagreement between sources
/// (JWT vs. subdomain vs. trusted header).
///
/// The API exception handler maps this to <c>401 Unauthorized</c> when the
/// caller is unauthenticated and <c>403 Forbidden</c> when authenticated but
/// targeting a tenant they don't belong to.
/// </summary>
public sealed class TenantResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public TenantResolutionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TenantResolutionException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="reason">The failure reason.</param>
    public TenantResolutionException(string message, TenantResolutionFailureReason reason)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason for the tenant resolution failure.
    /// </summary>
    public TenantResolutionFailureReason Reason { get; init; } = TenantResolutionFailureReason.Unspecified;
}

/// <summary>Why tenant resolution failed. Exposed for telemetry and exception mapping; not for business logic branching.</summary>
public enum TenantResolutionFailureReason
{
    /// <summary>The failure reason is unknown.</summary>
    Unspecified = 0,
    /// <summary>The required JWT claim is missing.</summary>
    MissingJwtClaim = 1,
    /// <summary>The tenant identifier format is invalid.</summary>
    InvalidTenantIdFormat = 2,
    /// <summary>Multiple tenant resolution sources disagree on the identity.</summary>
    SourcesDisagree = 3,
    /// <summary>The tenant is currently disabled.</summary>
    TenantDisabled = 4,
    /// <summary>The specified tenant was not found.</summary>
    TenantNotFound = 5,
    /// <summary>The tenant header was provided without a valid gateway proof.</summary>
    UntrustedHeaderSource = 6,
}
