using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Domain.IoT;
using Karamchari.TimeAttendance.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;

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
    public DbSet<Karamchari.TimeAttendance.Domain.Geofencing.LocationBoundary> LocationBoundaries => Set<Karamchari.TimeAttendance.Domain.Geofencing.LocationBoundary>();
    public DbSet<Karamchari.TimeAttendance.Domain.Geofencing.EmployeeLocationAssignment> EmployeeLocationAssignments => Set<Karamchari.TimeAttendance.Domain.Geofencing.EmployeeLocationAssignment>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.FraudFlag> FraudFlags => Set<Karamchari.TimeAttendance.Domain.IoT.FraudFlag>();
    public DbSet<Karamchari.TimeAttendance.Domain.Fraud.FraudSignal> FraudSignals => Set<Karamchari.TimeAttendance.Domain.Fraud.FraudSignal>();
    public DbSet<Karamchari.TimeAttendance.Domain.Fraud.FraudFlag> DetailedFraudFlags => Set<Karamchari.TimeAttendance.Domain.Fraud.FraudFlag>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.LiveAttendance> LiveAttendance => Set<Karamchari.TimeAttendance.Domain.IoT.LiveAttendance>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.AttendanceResult> AttendanceResults => Set<Karamchari.TimeAttendance.Domain.IoT.AttendanceResult>();
    public DbSet<Karamchari.TimeAttendance.Domain.IoT.AttendanceAudit> AttendanceAudits => Set<Karamchari.TimeAttendance.Domain.IoT.AttendanceAudit>();

    // ── Analytics ───────────────────────────────────────────────────────────

    public DbSet<ProjectMetrics> ProjectMetrics => Set<ProjectMetrics>();
    public DbSet<EmployeeMetrics> EmployeeMetrics => Set<EmployeeMetrics>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
    public DbSet<ProcessedEventLog> ProcessedEventLogs => Set<ProcessedEventLog>();

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

        const string MessagingSchema = "dbo";
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));

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

        modelBuilder.Entity<BackgroundJob>(b =>
        {
            b.ToTable("IoT_BackgroundJobs");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
        });

        modelBuilder.Entity<ProcessedEventLog>(b =>
        {
            b.ToTable("Analytics_ProcessedEventLog");
            b.HasKey(x => new { x.EventId, x.ConsumerName });
            b.HasIndex(x => new { x.EventId, x.ConsumerName }).IsUnique();
            b.HasIndex(x => x.ProcessedAt);
        });

        modelBuilder.Entity<Timesheet>(b =>
        {
            b.ToTable("Timesheets");
            b.HasKey(x => x.Id);

            b.Property(x => x.EmployeeTimeZoneId).HasMaxLength(64).IsRequired();
            b.Property(x => x.ApprovedAt);
            b.Property(x => x.IsRetroactive);

            // TotalHours is computed in-memory from the JSON entries — no DB column needed.
            b.Ignore(x => x.TotalHours);

            // Entries and audit log are stored as JSON columns for flexible,
            // week-bounded access without a separate join table.
            b.OwnsMany(x => x.Entries, e => e.ToJson());
            b.OwnsMany(x => x.AuditLog, a => a.ToJson());
        });

        modelBuilder.Entity<AttendanceResult>(b =>
        {
            b.ToTable("IoT_AttendanceResults");
            b.HasKey(x => x.Id);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Date }).IsUnique();
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

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Geofencing.LocationBoundary>(b =>
        {
            b.ToTable("IoT_LocationBoundaries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Location).HasColumnType("geography");
            b.HasIndex(x => x.Location);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Geofencing.EmployeeLocationAssignment>(b =>
        {
            b.ToTable("IoT_EmployeeLocationAssignments");
            b.HasKey(x => new { x.EmployeeId, x.LocationBoundaryId });
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.IoT.FraudFlag>(b =>
        {
            b.ToTable("IoT_FraudFlags");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Fraud.FraudSignal>(b =>
        {
            b.ToTable("IoT_FraudSignals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Fraud.FraudFlag>(b =>
        {
            b.ToTable("IoT_DetailedFraudFlags");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RawPunchId);
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

        // ── Analytics ────────────────────────────────────────────────────────
        // Columnstore indexes are added via raw migration SQL (see Migrations).

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Analytics.ProjectMetrics>(b =>
        {
            b.ToTable("Analytics_ProjectMetrics");
            b.HasKey(x => new { x.TenantId, x.ProjectId, x.Date });
            b.HasIndex(x => new { x.TenantId, x.Date }); // for tenant-wide date range queries
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Analytics.EmployeeMetrics>(b =>
        {
            b.ToTable("Analytics_EmployeeMetrics");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.Date });
            b.HasIndex(x => new { x.TenantId, x.Date });
        });

        modelBuilder.Entity<Karamchari.TimeAttendance.Domain.Analytics.ProcessedEventLog>(b =>
        {
            b.ToTable("Analytics_ProcessedEventLog");
            b.HasKey(x => new { x.EventId, x.ConsumerName });
            // Unique DB constraint enforces idempotency even under concurrent retries
            b.HasIndex(x => new { x.EventId, x.ConsumerName }).IsUnique();
            b.HasIndex(x => x.ProcessedAt); // for cleanup jobs
        });
    }
}
