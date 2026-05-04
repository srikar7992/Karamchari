namespace Karamchari.HR.Contracts.Organization;

/// <summary>
/// Service for managing organizational units like departments.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Creates a new department.
    /// </summary>
    /// <param name="command">The create department command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unique identifier of the newly created department.</returns>
    Task<Guid> CreateDepartmentAsync(CreateDepartmentCommand command, CancellationToken cancellationToken = default);
}
