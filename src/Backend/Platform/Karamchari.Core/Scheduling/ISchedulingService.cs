using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Karamchari.Core.Scheduling;

/// <summary>
/// Reusable platform service interface for intersection-based scheduling and booking of sessions.
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// Finds slots where all participants are available.
    /// </summary>
    Task<List<TimeSlot>> FindCommonAvailabilityAsync(
        string tenantId,
        List<Guid> participantEmployeeIds,
        DateTimeOffset start,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Books a scheduled session.
    /// </summary>
    Task BookSessionAsync(
        string tenantId,
        string purpose,
        List<Guid> participantEmployeeIds,
        DateTimeOffset start,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents a start and end date time slot boundary.
/// </summary>
public record TimeSlot(DateTimeOffset Start, DateTimeOffset End);
