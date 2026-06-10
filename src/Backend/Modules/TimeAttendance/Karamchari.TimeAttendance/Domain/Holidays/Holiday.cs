// -----------------------------------------------------------------------
// <copyright file="Holiday.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Holidays;

using Karamchari.Core.Domain.Primitives;

/// <summary>
/// Represents a specific holiday within a calendar.
/// </summary>
public sealed class Holiday : Entity<Guid>
{
    internal Holiday(Guid id, DateOnly date, string name) : base(id)
    {
        Date = date;
        Name = name;
    }

    private Holiday()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Gets the date of the holiday.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets the name of the holiday.
    /// </summary>
    public string Name { get; private set; }
}
