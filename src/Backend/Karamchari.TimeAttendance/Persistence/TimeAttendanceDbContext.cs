using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
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
    /// Gets the weekly timesheets set.
    /// </summary>
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();

    // ── IoT DbSets ──────────────────────────────────────────────────────────

    public DbSet<Karamchari.TimeAttendance.Domain.IoT.Device> Devices => Set<Karamchari.TimeAttendance.Domain.IoT.Device>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.BiometricMapping> BiometricMappings => Set<Karamchari.TimeAttendance.Domain.IoT.BiometricMapping>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.RawPunch> RawPunches => Set<Karamchari.TimeAttendance.Domain.IoT.RawPunch>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.InvalidPunch> InvalidPunches => Set<Karamchari.TimeAttendance.Domain.IoT.InvalidPunch>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.GeoFence> GeoFences => Set<Karamchari.TimeAttendance.Domain.IoT.GeoFence>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.FraudFlag> FraudFlags => Set<Karamchari.TimeAttendance.Domain.IoT.FraudFlag>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.LiveAttendance> LiveAttendance => Set<Karamchari.TimeAttendance.Domain.IoT.LiveAttendance>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.AttendanceResult> AttendanceResults => Set<Karamchari.TimeAttendance.Domain.IoT.AttendanceResult>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.AttendanceAudit> AttendanceAudits => Set<Karamchari.TimeAttendance.Domain.IoT.AttendanceAudit>();

    // ── Shifts DbSets ────────────────────────────────────────────────────────

    public DbSet<Karamchari.TimeAttendance.Domain.Shifts.ShiftTemplate> ShiftTemplates => Set<Karamchari.TimeAttendance.Domain.Shifts.ShiftTemplate>();
    public DbSet<Karamchari.TimeAttendance.Domain.Shifts.ShiftAssignment> ShiftAssignments => Set<Karamchari.TimeAttendance.Domain.Shifts.ShiftAssignment>();
    public DbSet<Karamchari.TimeAttendance.Domain.Shifts.ShiftOverride> ShiftOverrides => Set<Karamchari.TimeAttendance.Domain.Shifts.ShiftOverride>();
    public DbSet<Karamchari.TimeAttendance.Domain.Shifts.WeeklyOffRule> WeeklyOffRules => Set<Karamchari.TimeAttendance.Domain.Shifts.WeeklyOffRule>();

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
            b.OwnsOne(x => x.Rules, r => r.ToJson());
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

        modelBuilder.Entity<Timesheet>(b =>
        {
            b.ToTable("Timesheets");
            b.HasKey(x => x.Id);
            
            // Mapped to a JSON column for flexible, week-bounded entries.
            b.OwnsMany(x => x.Entries, e => e.ToJson());
            
            b.Property(x => x.TotalHours).HasPrecision(18, 2);
        });

        // ── IoT & Edge Ingestion ────────────────────────────────────────────────

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.Device>(b =>
        {
            b.ToTable("IoT_Devices");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ApiKey).IsUnique();
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.BiometricMapping>(b =>
        {
            b.ToTable("IoT_BiometricMappings");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.BiometricId).IsUnique();
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.RawPunch>(b =>
        {
            b.ToTable("IoT_RawPunches");
            b.HasKey(x => x.Id);
            
            // Strict Idempotency: DeviceId + ExternalId must be unique.
            b.HasIndex(x => new { x.DeviceId, x.ExternalId }).IsUnique();
            
            // For querying chronological punches per employee.
            b.HasIndex(x => new { x.EmployeeId, x.TimestampUtc });
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.InvalidPunch>(b =>
        {
            b.ToTable("IoT_InvalidPunches");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.GeoFence>(b =>
        {
            b.ToTable("IoT_GeoFences");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.FraudFlag>(b =>
        {
            b.ToTable("IoT_FraudFlags");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.LiveAttendance>(b =>
        {
            b.ToTable("IoT_LiveAttendance");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId).IsUnique();
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.AttendanceResult>(b =>
        {
            b.ToTable("IoT_AttendanceResults");
            b.HasKey(x => x.Id);
            // One final result per employee per date.
            b.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.AttendanceAudit>(b =>
        {
            b.ToTable("IoT_AttendanceAudits");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.EmployeeId, x.Date });
        });

        // ── Shift Rostering ──────────────────────────────────────────────────────

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Shifts.ShiftTemplate>(b =>
        {
            b.ToTable("Shifts_Templates");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Shifts.ShiftAssignment>(b =>
        {
            b.ToTable("Shifts_Assignments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Shifts.ShiftOverride>(b =>
        {
            b.ToTable("Shifts_Overrides");
            b.HasKey(x => x.Id);
            // Only one override per employee per date.
            b.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Shifts.WeeklyOffRule>(b =>
        {
            b.ToTable("Shifts_WeeklyOffRules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
        });
    }
}
