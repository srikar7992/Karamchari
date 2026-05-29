using System;

namespace Karamchari.HR.Contracts.Employees;

/// <summary>
/// Data transfer object for employee details.
/// </summary>
public record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn,
    string Status,
    string TimeZoneId
);
