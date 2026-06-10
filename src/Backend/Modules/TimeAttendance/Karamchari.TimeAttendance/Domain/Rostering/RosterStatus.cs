// -----------------------------------------------------------------------
// <copyright file="RosterStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering;

public enum RosterStatus
{
    Draft = 1,
    Review = 2,
    Published = 3,
    Archived = 4,
}
