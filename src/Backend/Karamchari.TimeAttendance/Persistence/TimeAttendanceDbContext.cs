using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Domain.Analytics;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Compliance;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Timesheets;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Database context for the Time and Attendance bounded context.
/// Phase 1C: Workforce Operational Intelligence Platform.
/// </summary>
public class TimeAttendanceDbContext : KaramchariDbContext
{
    public TimeAttendanceDbContext(DbContextOptions<TimeAttendanceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    // â”€â”€ Workforce Operational intelligence (Phase 1C) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<WorkforceSchedule> WorkforceSchedules => Set<WorkforceSchedule>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<AttendanceAnomaly> AttendanceAnomalies => Set<AttendanceAnomaly>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();

    // â”€â”€ Core Attendance/Leave sets â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveBalanceReadModel> LeaveBalanceReadModels => Set<LeaveBalanceReadModel>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();

    // â”€â”€ Analytics â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ProjectMetrics> ProjectMetrics => Set<ProjectMetrics>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ProcessedEventLog> ProcessedEventLogs => Set<ProcessedEventLog>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        const string MessagingSchema = "dbo";

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema, t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema, t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema, t => t.ExcludeFromMigrations()));

        // â”€â”€ Workforce Aggregates Mapping â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<ShiftDefinition>(b =>
        {
            b.ToTable("Workforce_ShiftDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Code).HasMaxLength(32).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<WorkforceSchedule>(b =>
        {
            b.ToTable("Workforce_Schedules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
            b.OwnsMany(x => x.Assignments, a =>
            {
                a.ToTable("Workforce_ShiftAssignments");
                a.WithOwner().HasForeignKey("ScheduleId");
                a.HasKey(x => x.Id);
                a.HasIndex(x => new { x.EmployeeId, x.Date });
            });
        });

        modelBuilder.Entity<AttendanceSession>(b =>
        {
            b.ToTable("Workforce_AttendanceSessions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.WorkDate }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.TotalWorkHours).HasPrecision(18, 2);
            b.Property(x => x.TotalBreakHours).HasPrecision(18, 2);

            b.OwnsOne(x => x.CheckInLocation, loc =>
            {
                loc.Property(p => p.Latitude).HasColumnName("CheckIn_Latitude");
                loc.Property(p => p.Longitude).HasColumnName("CheckIn_Longitude");
            });

            b.OwnsMany(x => x.Events, e =>
            {
                e.ToTable("Workforce_AttendanceEvents");
                e.WithOwner().HasForeignKey("SessionId");
                e.HasKey(x => x.Id);
                e.Property(x => x.Source).HasConversion<string>();
                e.OwnsOne(x => x.Location, loc =>
                {
                    loc.Property(p => p.Latitude).HasColumnName("Latitude");
                    loc.Property(p => p.Longitude).HasColumnName("Longitude");
                });
            });
        });

        modelBuilder.Entity<AttendanceRecord>(b =>
        {
            b.ToTable("Workforce_AttendanceRecords");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Date }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.WorkedHours).HasPrecision(18, 2);
            b.Property(x => x.OvertimeHours).HasPrecision(18, 2);
        });

        modelBuilder.Entity<AttendanceAnomaly>(b =>
        {
            b.ToTable("Workforce_AttendanceAnomalies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.ConfidenceScore).HasPrecision(5, 2);
        });

        modelBuilder.Entity<AttendancePolicy>(b =>
        {
            b.ToTable("Workforce_AttendancePolicies");
            b.HasKey(x => x.Id);
            b.OwnsMany(x => x.Rules, r =>
            {
                r.ToTable("Workforce_ComplianceRules");
                r.WithOwner().HasForeignKey("PolicyId");
                r.HasKey(x => x.Id);
                r.Property(x => x.Type).HasConversion<string>();
                r.Property(x => x.Severity).HasConversion<string>();
                r.Property(x => x.Threshold).HasPrecision(18, 2);
            });
        });

        // â”€â”€ Legacy Infrastructure (Mismatched but needed for build consistency for now) â”€â”€

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
            b.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<LeaveBalance>(b =>
        {
            b.ToTable("LeaveBalances");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PolicyId }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Ignore(x => x.AvailableBalance);

            b.OwnsMany(x => x.Entries, e =>
            {
                e.ToTable("LeaveBalanceEntries");
                e.WithOwner().HasForeignKey("BalanceId");
                e.HasKey(x => x.Id);
                e.Property(x => x.EntryType).HasConversion<string>();
                e.Property(x => x.Quantity).HasPrecision(18, 2);
                e.HasIndex(x => new { x.EmployeeId, x.PolicyId });
            });
        });

        modelBuilder.Entity<LeaveBalanceReadModel>(b =>
        {
            b.ToTable("Projections_LeaveBalances");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PolicyId }).IsUnique();
            b.Property(x => x.AvailableBalance).HasPrecision(18, 2);
            b.Property(x => x.ConsumedBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Timesheet>(b =>
        {
            b.ToTable("Timesheets");
            b.HasKey(x => x.Id);
            b.Property(x => x.EmployeeTimeZoneId).HasMaxLength(64).IsRequired();
            b.Ignore(x => x.TotalHours);
            b.OwnsMany(x => x.Entries, e => e.ToJson());
            b.OwnsMany(x => x.AuditLog, a => a.ToJson());
        });

        modelBuilder.Entity<ProjectMetrics>(b =>
        {
            b.ToTable("Analytics_ProjectMetrics");
            b.HasKey(x => new { x.TenantId, x.ProjectId, x.Date });
        });

        modelBuilder.Entity<ProcessedEventLog>(b =>
        {
            b.ToTable("Analytics_ProcessedEventLog");
            b.HasKey(x => new { x.EventId, x.ConsumerName });
        });
    }
}
