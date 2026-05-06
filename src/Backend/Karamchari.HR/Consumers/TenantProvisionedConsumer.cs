namespace Karamchari.HR.Consumers;

using MassTransit;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.HR.Persistence;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Employees;

/// <summary>
/// Provisions default HR structure (HQ department, Super Admin employee) for a new tenant.
/// </summary>
public sealed class TenantProvisionedConsumer : IConsumer<TenantProvisionedIntegrationEvent>
{
    private readonly HRDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisionedConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The HR database context.</param>
    public TenantProvisionedConsumer(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Consumes the tenant provisioned event and seeds HR defaults.
    /// </summary>
    /// <param name="context">The consumer context.</param>
    public async Task Consume(ConsumeContext<TenantProvisionedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Create Default Headquarters Department
        var hq = Department.Create("Headquarters", "Primary organizational unit for administrative functions.");
        _dbContext.Departments.Add(hq);

        // 2. Create the initial Super Admin Employee
        // In a real onboarding flow, this would be the person who signed up for the account.
        var admin = Employee.Hire(
            "EMP-001",
            "Super Administrator",
            context.Message.AdminEmail,
            DateOnly.FromDateTime(DateTime.UtcNow));

        _dbContext.Employees.Add(admin);

        await _dbContext.SaveChangesAsync();
    }
}
