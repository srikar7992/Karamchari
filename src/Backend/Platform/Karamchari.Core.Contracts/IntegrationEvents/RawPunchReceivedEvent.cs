// -----------------------------------------------------------------------
// <copyright file="RawPunchReceivedEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Contracts.IntegrationEvents;

/// <summary>
/// Immutable event published by the Edge Ingestion API.
/// This decouples the high-throughput, unreliable device streams from the deterministic TimeAttendance context.
/// </summary>
public sealed record RawPunchReceivedEvent
{
    /// <summary>The raw JSON or URL-encoded payload from the device.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>The internal ID of the authenticated device.</summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>The tenant identifier mapped from the authenticated device.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>The exact UTC time the edge API received this payload.</summary>
    public DateTime ReceivedAtUtc { get; init; }

    /// <summary>Indicates if the punch originated from a mobile device vs physical biometric hardware.</summary>
    public bool IsMobileSource { get; init; }
}
