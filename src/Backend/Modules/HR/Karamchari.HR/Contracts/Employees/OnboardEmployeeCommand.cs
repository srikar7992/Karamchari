// -----------------------------------------------------------------------
// <copyright file="OnboardEmployeeCommand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Contracts.Employees;

/// <summary>
/// Command to onboard a new employee into the HR system.
/// </summary>
public record OnboardEmployeeCommand(
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn);
