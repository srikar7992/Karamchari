// -----------------------------------------------------------------------
// <copyright file="IdempotentRequest.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Domain.Idempotency;

/// <summary>
/// Represents a previously processed request for idempotency purposes.
/// </summary>
public sealed class IdempotentRequest
{
    /// <summary>Gets the idempotency key.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the HTTP status code of the response.</summary>
    public int StatusCode { get; private set; }

    /// <summary>Gets the response body.</summary>
    public string ResponseBody { get; private set; } = string.Empty;

    /// <summary>Gets the date and time the request was processed.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Gets the date and time the record expires.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the request record has expired.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;

    private IdempotentRequest() { }

    /// <summary>Creates a new idempotent request record.</summary>
    public static IdempotentRequest Create(string key, string tenantId, int statusCode, string body, TimeSpan ttl)
    {
        var now = DateTimeOffset.UtcNow;
        return new IdempotentRequest
        {
            IdempotencyKey = key,
            TenantId = tenantId,
            StatusCode = statusCode,
            ResponseBody = body,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(ttl)
        };
    }
}
