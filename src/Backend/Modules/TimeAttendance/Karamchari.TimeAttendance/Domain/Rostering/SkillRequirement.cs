// -----------------------------------------------------------------------
// <copyright file="SkillRequirement.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering;

public sealed record SkillRequirement(
    Guid SkillId,
    string SkillName,
    int MinimumLevel,
    int Count);
