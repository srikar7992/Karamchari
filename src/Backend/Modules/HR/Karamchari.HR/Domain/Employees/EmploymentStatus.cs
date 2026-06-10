// -----------------------------------------------------------------------
// <copyright file="EmploymentStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Employees;

/// <summary>
/// Defines the possible employment statuses for an employee.
/// </summary>
public enum EmploymentStatus
{
    /// <summary>The employee is currently active.</summary>
    Active = 1,
    /// <summary>The employee has been terminated.</summary>
    Terminated = 2,
}
