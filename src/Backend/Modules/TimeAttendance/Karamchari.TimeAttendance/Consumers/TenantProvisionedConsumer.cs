namespace Karamchari.TimeAttendance.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;

/// <summary>
/// Seeds default Time &amp; Attendance data (policies, calendars) for a newly provisioned tenant.
/// </summary>
public sealed class TenantProvisionedConsumer : IConsumer<TenantProvisionedIntegrationEvent>
{
    private readonly TimeAttendanceDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisionedConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The time attendance database context.</param>
    public TenantProvisionedConsumer(TimeAttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Consumes the tenant provisioned event and seeds defaults.
    /// </summary>
    /// <param name="context">The consumer context.</param>
    public async Task Consume(ConsumeContext<TenantProvisionedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Note: The MassTransit tenant filter or schema interceptor ensures 
        // these operations target the new tenant's schema based on the event's TenantId header.

        // 1. Seed Default Leave Policies
        var sickLeave = LeavePolicy.Create(
            "Standard Sick Leave",
            "10 days of paid sick leave per year.",
            new LeavePolicyRules
            {
                Category = LeaveCategory.Paid,
                AnnualAllowance = 10,
                AccrualFrequency = AccrualFrequency.Upfront,
                RequiresManagerApproval = true,
                AllowHalfDays = true
            });

        var casualLeave = LeavePolicy.Create(
            "Casual Leave",
            "Standard casual leave allowance for personal matters.",
            new LeavePolicyRules
            {
                Category = LeaveCategory.Paid,
                AnnualAllowance = 12,
                AccrualFrequency = AccrualFrequency.Upfront,
                RequiresManagerApproval = true,
                AllowHalfDays = true
            });

        _dbContext.LeavePolicies.AddRange(sickLeave, casualLeave);

        // 2. Seed Default Holiday Calendar
        var calendar = HolidayCalendar.Create("Default Calendar", "Standard Organizational Calendar");

        // Add some base global holidays
        var year = DateTime.UtcNow.Year;
        calendar.AddHoliday(new DateOnly(year, 1, 1), "New Year's Day");
        calendar.AddHoliday(new DateOnly(year, 12, 25), "Christmas Day");

        _dbContext.HolidayCalendars.Add(calendar);

        await _dbContext.SaveChangesAsync();
    }
}
