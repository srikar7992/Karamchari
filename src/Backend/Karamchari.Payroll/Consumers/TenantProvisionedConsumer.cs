namespace Karamchari.Payroll.Consumers;

using MassTransit;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;

/// <summary>
/// Provisions default Payroll settings (Standard Monthly Schedule) for a new tenant.
/// </summary>
public sealed class TenantProvisionedConsumer : IConsumer<TenantProvisionedIntegrationEvent>
{
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisionedConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The payroll database context.</param>
    public TenantProvisionedConsumer(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Consumes the tenant provisioned event and seeds payroll defaults.
    /// </summary>
    /// <param name="context">The consumer context.</param>
    public async Task Consume(ConsumeContext<TenantProvisionedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Create Default Monthly Payroll Schedule (Payday on the 1st)
        var schedule = PayrollSchedule.Create("Standard Monthly", 1);
        
        _dbContext.PayrollSchedules.Add(schedule);

        await _dbContext.SaveChangesAsync();
    }
}
