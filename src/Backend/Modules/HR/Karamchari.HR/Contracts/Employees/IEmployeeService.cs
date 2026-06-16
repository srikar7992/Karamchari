// -----------------------------------------------------------------------
// <copyright file="IEmployeeService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Contracts.Employees;

/// <summary>
/// Application service boundary for employee lifecycle operations.
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Onboards a new employee.
    /// </summary>
    /// <param name="command">The onboard employee command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unique identifier of the newly onboarded employee.</returns>
    Task<Guid> OnboardEmployeeAsync(OnboardEmployeeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an employee by ID.
    /// </summary>
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active employees for the current tenant.
    /// </summary>
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an enriched employee profile (identity + resolved organizational names).
    /// </summary>
    Task<EmployeeProfileDto?> GetEmployeeProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates employee details.
    /// </summary>
    Task UpdateEmployeeAsync(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates/deletes an employee.
    /// </summary>
    Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default);
}
