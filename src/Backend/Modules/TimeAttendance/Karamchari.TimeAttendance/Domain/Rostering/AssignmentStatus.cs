// -----------------------------------------------------------------------
// <copyright file="AssignmentStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering;

public enum AssignmentStatus
{
    Assigned = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4,
    Completed = 5,
}
