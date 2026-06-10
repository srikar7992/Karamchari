// -----------------------------------------------------------------------
// <copyright file="OrganizationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.HR.Contracts.Organization;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Persistence;

namespace Karamchari.HR.Services;

/// <summary>
/// Service for managing organizational units within HR.
/// </summary>
public sealed class OrganizationService : IOrganizationService
{
    private readonly HRDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationService"/> class.
    /// </summary>
    /// <param name="dbContext">The HR database context.</param>
    public OrganizationService(HRDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new department asynchronously.
    /// </summary>
    /// <param name="command">The create department command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unique identifier of the newly created department.</returns>
    public async Task<Guid> CreateDepartmentAsync(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var department = Department.Create(command.Name, command.Description);

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return department.Id;
    }
}
