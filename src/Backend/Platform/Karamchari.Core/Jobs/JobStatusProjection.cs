// -----------------------------------------------------------------------
// <copyright file="JobStatusProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Jobs;

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Stuck
}

/// <summary>
/// Projection for monitoring the health of operational batch jobs.
/// Implements Priority 4 Step 21.
/// </summary>
public sealed record JobStatusProjection(
    Guid JobId,
    string JobName,
    string OwningCapability,
    JobStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int RetryCount,
    string? LastError);
