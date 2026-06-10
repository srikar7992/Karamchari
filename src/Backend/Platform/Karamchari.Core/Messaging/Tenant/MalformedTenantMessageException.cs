// -----------------------------------------------------------------------
// <copyright file="MalformedTenantMessageException.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// Exception thrown when a message has malformed or missing tenant context.
/// </summary>
public sealed class MalformedTenantMessageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedTenantMessageException"/> class.
    /// </summary>
    public MalformedTenantMessageException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedTenantMessageException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public MalformedTenantMessageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedTenantMessageException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MalformedTenantMessageException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedTenantMessageException"/> class
    /// with specific failure details.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="tenantId">The tenant identifier that was found (if any).</param>
    /// <param name="failureReason">The reason for the failure.</param>
    public MalformedTenantMessageException(string message, string? tenantId, TenantMessageFailureReason failureReason)
        : base(message)
    {
        TenantId = tenantId;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Gets the tenant identifier that was found (if any) when the failure occurred.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the reason for the tenant message failure.
    /// </summary>
    public TenantMessageFailureReason FailureReason { get; init; } = TenantMessageFailureReason.Unspecified;
}

/// <summary>
/// Reason for tenant message failure. Used for telemetry and exception handling.
/// </summary>
public enum TenantMessageFailureReason
{
    /// <summary>The failure reason is unknown.</summary>
    Unspecified = 0,
    /// <summary>The required tenant identifier is missing from the message headers.</summary>
    MissingTenantId = 1,
    /// <summary>The tenant identifier format is invalid.</summary>
    InvalidTenantIdFormat = 2,
    /// <summary>The message timestamp is stale (older than 5 minutes).</summary>
    StaleMessage = 3,
    /// <summary>The message was detected as a replay attack.</summary>
    ReplayAttack = 4,
    /// <summary>The content hash validation failed.</summary>
    ContentHashMismatch = 5,
    /// <summary>The execution envelope is malformed or missing.</summary>
    MalformedEnvelope = 6,
    /// <summary>The tenant identifier in the message does not match expected tenant.</summary>
    TenantMismatch = 7,
    /// <summary>The message is missing required headers.</summary>
    MissingRequiredHeaders = 8,
    /// <summary>The cryptographic signature is invalid or missing.</summary>
    InvalidSignature = 9
}
