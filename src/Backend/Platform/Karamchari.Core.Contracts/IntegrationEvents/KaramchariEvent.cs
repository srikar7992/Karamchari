// -----------------------------------------------------------------------
// <copyright file="KaramchariEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Contracts.IntegrationEvents;

/// <summary>
/// Identifies the originating subsystem that produced an integration event.
/// </summary>
public enum EventSource
{
    /// <summary>Triggered by a user action via the REST API (human actor, authenticated).</summary>
    Api,

    /// <summary>Triggered by a background job or scheduled task (system actor).</summary>
    Batch,

    /// <summary>Triggered internally by the platform (e.g. auto-approval, escalation).</summary>
    System,
}

/// <summary>
/// Mandatory envelope fields carried by every Karamchari integration event.
/// Bounded contexts embed this by including ActorId, TenantId, OccurredAt, Source
/// directly on their event records (composition, not inheritance, because records
/// in C# don't support base-record positional parameters ergonomically).
///
/// Validation rule: ActorId must never be Guid.Empty on events that originate from
/// an authenticated user action. Consumers dead-letter events that violate this.
/// </summary>
public static class KaramchariEventContract
{
    /// <summary>Well-known actor ID used for system-generated events (batch jobs, escalations).</summary>
    public static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Throws if <paramref name="actorId"/> is empty and <paramref name="source"/> is <see cref="EventSource.Api"/>.
    /// Call this at the point of event construction to enforce the quality gate.
    /// </summary>
    public static void ValidateActorId(Guid actorId, EventSource source)
    {
        if (source == EventSource.Api && actorId == Guid.Empty)
            throw new InvalidOperationException(
                "ActorId must not be Guid.Empty for API-originated events. " +
                "Extract the actor from the authenticated ClaimsPrincipal before publishing.");
    }
}
