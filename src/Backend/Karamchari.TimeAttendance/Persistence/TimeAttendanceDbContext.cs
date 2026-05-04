using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Database context for the Time and Attendance bounded context.
/// </summary>
public class TimeAttendanceDbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeAttendanceDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public TimeAttendanceDbContext(DbContextOptions<TimeAttendanceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Gets the holiday calendars set.
    /// </summary>
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();

    /// <summary>
    /// Gets the leave policies set.
    /// </summary>
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();

    /// <summary>
    /// Gets the leave requests set.
    /// </summary>
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    /// <summary>
    /// Gets the leave balances set.
    /// </summary>
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();

    /// <summary>
    /// Configures the domain model for the TimeAttendance context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<HolidayCalendar>(b =>
        {
            b.ToTable("HolidayCalendars");
            b.HasKey(x => x.Id);
            b.OwnsMany(x => x.Holidays, h =>
            {
                h.ToTable("Holidays");
                h.WithOwner().HasForeignKey("CalendarId");
                h.HasKey(x => x.Id);
            });
        });

        modelBuilder.Entity<LeavePolicy>(b =>
        {
            b.ToTable("LeavePolicies");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<LeaveRequest>(b =>
        {
            b.ToTable("LeaveRequests");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<LeaveBalance>(b =>
        {
            b.ToTable("LeaveBalances");
            b.HasKey(x => x.Id);
        });
    }
}
