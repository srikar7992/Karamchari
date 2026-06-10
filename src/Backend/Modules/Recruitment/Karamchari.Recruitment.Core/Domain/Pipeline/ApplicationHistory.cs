// -----------------------------------------------------------------------
// <copyright file="ApplicationHistory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain.Pipeline;

/// <summary>
/// Auditable timeline of candidate progress and state transitions.
/// </summary>
public sealed class ApplicationHistory : Entity<Guid>, ITenantOwned
{
    private ApplicationHistory()
    {
        TenantId = string.Empty;
        FromStatus = string.Empty;
        ToStatus = string.Empty;
        TransitionedByUserId = string.Empty;
        Notes = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationHistory"/> class.
    /// </summary>
    public ApplicationHistory(
        Guid id,
        string tenantId,
        Guid applicationId,
        string fromStatus,
        string toStatus,
        string transitionedByUserId,
        string notes) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(toStatus);

        TenantId = tenantId;
        ApplicationId = applicationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        TransitionedByUserId = transitionedByUserId ?? string.Empty;
        Notes = notes ?? string.Empty;
        TransitionedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the application identifier.
    /// </summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>
    /// Gets the status before transition.
    /// </summary>
    public string FromStatus { get; private set; }

    /// <summary>
    /// Gets the status after transition.
    /// </summary>
    public string ToStatus { get; private set; }

    /// <summary>
    /// Gets the timestamp when transition occurred.
    /// </summary>
    public DateTimeOffset TransitionedAtUtc { get; private set; }

    /// <summary>
    /// Gets the operator identifier who executed the state change.
    /// </summary>
    public string TransitionedByUserId { get; private set; }

    /// <summary>
    /// Gets any notes associated with the stage change.
    /// </summary>
    public string Notes { get; private set; }
}
