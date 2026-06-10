// -----------------------------------------------------------------------
// <copyright file="JobGovernanceService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Jobs;

/// <summary>
/// Service for governing and monitoring operational jobs.
/// Implements Priority 4 Step 19.
/// </summary>
public sealed partial class JobGovernanceService
{
    private readonly ILogger<JobGovernanceService> _logger;

    public JobGovernanceService(ILogger<JobGovernanceService> logger)
    {
        _logger = logger;
    }

    public void TrackJobStart(Guid jobId, string jobName, string capability)
    {
        LogJobStarted(_logger, jobName, jobId, capability);
        // In a real system, this would write to a JobStatus store
    }

    public void TrackJobCompletion(Guid jobId)
    {
        LogJobCompleted(_logger, jobId);
    }

    public void TrackJobFailure(Guid jobId, Exception ex)
    {
        LogJobFailed(_logger, jobId, ex);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Job {JobName} ({JobId}) started in capability {Capability}")]
    static partial void LogJobStarted(ILogger logger, string jobName, Guid jobId, string capability);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Job {JobId} completed successfully")]
    static partial void LogJobCompleted(ILogger logger, Guid jobId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Job {JobId} failed")]
    static partial void LogJobFailed(ILogger logger, Guid jobId, Exception ex);
}
