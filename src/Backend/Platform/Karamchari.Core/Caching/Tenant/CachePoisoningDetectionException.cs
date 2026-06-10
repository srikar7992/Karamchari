// -----------------------------------------------------------------------
// <copyright file="CachePoisoningDetectionException.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// Exception thrown when a potential cache poisoning attempt is detected,
/// such as a tenant attempting to access or modify a cache key that does not belong to them.
/// </summary>
public sealed class CachePoisoningDetectionException : Exception
{
    /// <summary>Gets the cache key that was involved in the detected attempt.</summary>
    public string? AttackKey { get; }

    /// <summary>Gets the timestamp when the attempt was detected.</summary>
    public DateTime DetectionTimeUtc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachePoisoningDetectionException"/> class.
    /// </summary>
    public CachePoisoningDetectionException(string message)
        : base(message)
    {
        AttackKey = null;
        DetectionTimeUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachePoisoningDetectionException"/> class with the suspicious key.
    /// </summary>
    public CachePoisoningDetectionException(string message, string attackKey)
        : base(message)
    {
        AttackKey = attackKey;
        DetectionTimeUtc = DateTime.UtcNow;
    }
}
