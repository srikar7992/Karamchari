// -----------------------------------------------------------------------
// <copyright file="EmploymentType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Employees;

/// <summary>
/// Defines the various forms of employment within the workforce.
/// </summary>
public enum EmploymentType
{
    /// <inheritdoc/>
    FullTime = 1,
    /// <inheritdoc/>
    PartTime = 2,
    /// <inheritdoc/>
    Contractor = 3,
    /// <inheritdoc/>
    Intern = 4,
    /// <inheritdoc/>
    Consultant = 5,
    /// <inheritdoc/>
    Trainee = 6
}
