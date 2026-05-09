using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Domain.Analytics;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;

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

    // ── Workforce Operational intelligence (Phase 1C) ─────────────────────────

    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    public DbSet<WorkforceSchedule> WorkforceSchedules => Set<WorkforceSchedule>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceAnomaly> AttendanceAnomalies => Set<AttendanceAnomaly>();
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();

    // ── Core Attendance/Leave sets ───────────────────────────────────────────

    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();

    // ── Analytics ───────────────────────────────────────────────────────────

    public DbSet<ProjectMetrics> ProjectMetrics => Set<ProjectMetrics>();
    public DbSet<ProcessedEventLog> ProcessedEventLogs => Set<ProcessedEventLog>();

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

        // ── Workforce Aggregates Mapping ─────────────────────────────────────

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
            
            b.OwnsMany(x => x.Events, e =>
            {
                e.ToTable("Workforce_AttendanceEvents");
                e.WithOwner().HasForeignKey("SessionId");
                e.HasKey(x => x.Id);
                e.Property(x => x.Source).HasConversion<string>();
            });
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

        // ── Legacy Infrastructure (Mismatched but needed for build consistency for now) ──

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
