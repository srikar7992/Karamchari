// -----------------------------------------------------------------------
// <copyright file="UpdateEmployeeCommand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Contracts.Employees;

/// <summary>
/// Command to update employee details.
/// </summary>
public record UpdateEmployeeCommand(
    string LegalName,
    string? WorkEmail
);
