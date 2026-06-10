// -----------------------------------------------------------------------
// <copyright file="FinancialReplayMetadata.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.FinancialOps.Contracts.Primitives;

/// <summary>
/// Metadata for tracking financial operations during replay or recovery.
/// </summary>
public record FinancialReplayMetadata(
    string SourceSystem,
    DateTimeOffset OriginalTimestamp,
    int ReplayAttempt = 0,
    bool IsSimulated = false)
{
    /// <summary>Gets an empty replay metadata instance.</summary>
    public static FinancialReplayMetadata Empty => new("System", DateTimeOffset.UtcNow);
}
