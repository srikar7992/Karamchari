// -----------------------------------------------------------------------
// <copyright file="EmployeeAvailabilityPreference.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Rostering;

/// <summary>
/// Stores an employee's declared availability for a specific day of week within a time range.
/// One row per (employee, day-of-week) entry. Multiple entries allowed per day to express
/// split availability (e.g. morning + evening slots).
/// Consumed by AvailabilityConstraint via AssignmentContext.AvailabilityWindows.
/// </summary>
public sealed class EmployeeAvailabilityPreference : Entity<Guid>, ITenantOwned
{
    private EmployeeAvailabilityPreference() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly From { get; private set; }
    public TimeOnly To { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static EmployeeAvailabilityPreference Declare(
        string tenantId,
        Guid employeeId,
        DayOfWeek dayOfWeek,
        TimeOnly from,
        TimeOnly to)
    {
        if (to <= from)
            throw new ArgumentException($"Availability window end ({to}) must be after start ({from}).");

        return new EmployeeAvailabilityPreference
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            DayOfWeek = dayOfWeek,
            From = from,
            To = to,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Revoke() => IsActive = false;
}
