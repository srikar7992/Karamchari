// -----------------------------------------------------------------------
// <copyright file="ICapacityProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Returns the maximum billable hours an employee may log in a given week.
/// Null means no cap is configured for that employee/week combination.
/// </summary>
public interface ICapacityProvider
{
    Task<decimal?> GetBillableCapacityAsync(
        Guid employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
}
