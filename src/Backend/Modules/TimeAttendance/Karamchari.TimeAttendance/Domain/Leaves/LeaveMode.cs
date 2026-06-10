// -----------------------------------------------------------------------
// <copyright file="LeaveMode.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

public enum LeaveMode
{
    FullDay = 1,
    HalfDay = 2,
    Hourly = 3,
    MultiDay = 4,
    Recurring = 5
}

public enum HalfDayPeriod
{
    FirstHalf = 1,
    SecondHalf = 2
}
