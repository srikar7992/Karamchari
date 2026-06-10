// -----------------------------------------------------------------------
// <copyright file="StaleTenantRestoreException.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// Exception thrown when attempting to restore a stale or invalid tenant context in background job processing.
/// This exception indicates a potential security concern when tenant context cannot be safely restored.
/// </summary>
public sealed class StaleTenantRestoreException : Exception
{
    /// <summary>
    /// Gets the tenant identifier associated with the stale context, if available.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets a value indicating whether the exception is security-relevant.
    /// </summary>
    public bool IsSecurityRelevant { get; }

    /// <summary>
    /// Initializes a new instance of the StaleTenantRestoreException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public StaleTenantRestoreException(string message) : base(message)
    {
        IsSecurityRelevant = true;
    }

    /// <summary>
    /// Initializes a new instance of the StaleTenantRestoreException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StaleTenantRestoreException(string message, Exception innerException)
        : base(message, innerException)
    {
        IsSecurityRelevant = true;
    }

    /// <summary>
    /// Initializes a new instance of the StaleTenantRestoreException class with a specified error message and tenant identifier.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="tenantId">The tenant identifier associated with the stale context.</param>
    public StaleTenantRestoreException(string message, string tenantId)
        : base(message)
    {
        TenantId = tenantId;
        IsSecurityRelevant = true;
    }

    /// <summary>
    /// Initializes a new instance of the StaleTenantRestoreException class with a specified error message, tenant identifier, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="tenantId">The tenant identifier associated with the stale context.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StaleTenantRestoreException(string message, string tenantId, Exception innerException)
        : base(message, innerException)
    {
        TenantId = tenantId;
        IsSecurityRelevant = true;
    }
}
