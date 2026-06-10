// -----------------------------------------------------------------------
// <copyright file="ProjectionMetadata.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Projections;

/// <summary>
/// Defines how a projection is synchronized with the source of truth.
/// </summary>
public enum ProjectionConsistencyType
{
    /// <summary>Projection is updated within the same transaction as the source.</summary>
    Strong,

    /// <summary>Projection is updated via domain events or background jobs.</summary>
    Eventual,

    /// <summary>Projection is calculated on-the-fly during read operations.</summary>
    OnDemand
}

/// <summary>
/// Defines the strategy for reconstructing a projection from its source ledger/events.
/// </summary>
public enum ProjectionRebuildStrategy
{
    /// <summary>Projection cannot be rebuilt; it is a point-in-time snapshot.</summary>
    None,

    /// <summary>Projection can be fully reconstructed from the beginning of time.</summary>
    FullReplay,

    /// <summary>Projection is rebuilt in chunks or within specific time/tenant windows.</summary>
    Incremental
}

/// <summary>
/// Defines the operational classification of a projection.
/// </summary>
public enum ProjectionCategory
{
    /// <summary>Critical for real-time operations and user experience.</summary>
    RealTimeOperational,

    /// <summary>Secondary views updated periodically or via events.</summary>
    NearRealTime,

    /// <summary>Heavy summaries calculated in batches for reporting or payroll.</summary>
    BatchOriented
}

/// <summary>
/// Metadata describing the lifecycle and governance of a projection.
/// Implements Priority 1 Step 2.
/// </summary>
public sealed record ProjectionMetadata
{
    public string ProjectionName { get; init; } = string.Empty;

    public string OwningCapability { get; init; } = string.Empty;

    public ProjectionConsistencyType ConsistencyType { get; init; }

    public ProjectionRebuildStrategy RebuildStrategy { get; init; }

    public ProjectionCategory Category { get; init; }

    /// <summary>The maximum allowed staleness for this projection.</summary>
    public TimeSpan RefreshTarget { get; init; }

    /// <summary>The version of the projection schema. Used for migration/rebuild logic.</summary>
    public string Version { get; init; } = "1.0";
}
