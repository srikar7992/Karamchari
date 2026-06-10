// -----------------------------------------------------------------------
// <copyright file="PublishViolation.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering.Validation;

public sealed record PublishViolation(
    string Code,
    string Description,
    DateOnly? Date = null,
    Guid? ShiftId = null);

public sealed class PublishValidationResult
{
    private readonly List<PublishViolation> _violations = [];

    public IReadOnlyList<PublishViolation> Violations => _violations.AsReadOnly();
    public bool IsValid => _violations.Count == 0;

    internal void Add(PublishViolation violation) => _violations.Add(violation);
}
