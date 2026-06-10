// -----------------------------------------------------------------------
// <copyright file="ProjectionCheckpoint.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.Core.Projections;

/// <summary>
/// Tracks the processed sequence offset for an individual read model projection, preventing double-processing on replays.
/// </summary>
public sealed class ProjectionCheckpoint
{
    private ProjectionCheckpoint()
    {
        ProjectionName = string.Empty;
        PartitionKey = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionCheckpoint"/> class.
    /// </summary>
    public ProjectionCheckpoint(string projectionName, string partitionKey, Guid lastProcessedEventId, long lastProcessedPosition, int lastProcessedVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        ProjectionName = projectionName;
        PartitionKey = partitionKey;
        LastProcessedEventId = lastProcessedEventId;
        LastProcessedPosition = lastProcessedPosition;
        LastProcessedVersion = lastProcessedVersion;
        ProcessedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique projection name.
    /// </summary>
    public string ProjectionName { get; private set; }

    /// <summary>
    /// Gets the tenant partition key or database shard boundary identifier.
    /// </summary>
    public string PartitionKey { get; private set; }

    /// <summary>
    /// Gets the event identifier of the last successfully processed event.
    /// </summary>
    public Guid LastProcessedEventId { get; private set; }

    /// <summary>
    /// Gets the monotonically increasing sequence offset representing the event store offset.
    /// </summary>
    public long LastProcessedPosition { get; private set; }

    /// <summary>
    /// Gets the version sequence index of the last processed event.
    /// </summary>
    public int LastProcessedVersion { get; private set; }

    /// <summary>
    /// Gets the timestamp when the checkpoint was updated.
    /// </summary>
    public DateTimeOffset ProcessedUtc { get; private set; }

    /// <summary>
    /// Updates the checkpoint location.
    /// </summary>
    public void UpdateCheckpoint(Guid eventId, long position, int version)
    {
        LastProcessedEventId = eventId;
        LastProcessedPosition = position;
        LastProcessedVersion = version;
        ProcessedUtc = DateTimeOffset.UtcNow;
    }
}
