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
}
