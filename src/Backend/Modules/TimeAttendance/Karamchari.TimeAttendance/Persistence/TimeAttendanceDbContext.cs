// -----------------------------------------------------------------------
// <copyright file="TimeAttendanceDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Domain.Absence;
using Karamchari.TimeAttendance.Domain.Analytics;
using Karamchari.TimeAttendance.Domain.Approval;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Biometric;
using Karamchari.TimeAttendance.Domain.Compliance;
using Karamchari.TimeAttendance.Domain.Geo;
using Karamchari.TimeAttendance.Domain.Governance;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Intelligence;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Operations;
using Karamchari.TimeAttendance.Domain.Policy;
using Karamchari.TimeAttendance.Domain.Rostering;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Domain.Scheduling;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Domain.WorkforcePlanning;
using Microsoft.EntityFrameworkCore;
using RegularizationRequest = Karamchari.TimeAttendance.Domain.Attendance.RegularizationRequest;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Database context for the Time and Attendance bounded context.
/// Phase 1C: Workforce Operational Intelligence Platform.
/// </summary>
public class TimeAttendanceDbContext(DbContextOptions<TimeAttendanceDbContext> options, ITenantProvider tenantProvider) : KaramchariDbContext(options, tenantProvider)
{

    // -- Biometric Engine (Phase Attendance) ---------------------------------------

    public DbSet<PunchEvent> PunchEvents => Set<PunchEvent>();
    public DbSet<BiometricTemplate> BiometricTemplates => Set<BiometricTemplate>();
    public DbSet<BiometricDevice> BiometricDevices => Set<BiometricDevice>();
    public DbSet<BiometricEnrollmentSession> BiometricEnrollmentSessions => Set<BiometricEnrollmentSession>();

    // -- Geo Engine ---------------------------------------------------------------

    public DbSet<GeoZone> GeoZones => Set<GeoZone>();

    // -- Policy Hierarchy ---------------------------------------------------------

    public DbSet<AttendancePolicyHierarchy> AttendancePolicyHierarchies => Set<AttendancePolicyHierarchy>();

    // â"€â"€ Workforce Operational intelligence (Phase 1C) â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    // Phase 2: Rostering
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.Roster> Rosters => Set<Karamchari.TimeAttendance.Domain.Rostering.Roster>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.RosterShiftAssignment> RosterShiftAssignments => Set<Karamchari.TimeAttendance.Domain.Rostering.RosterShiftAssignment>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.RosterShift> RosterShifts => Set<Karamchari.TimeAttendance.Domain.Rostering.RosterShift>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.CoverageRule> CoverageRules => Set<Karamchari.TimeAttendance.Domain.Rostering.CoverageRule>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.EmployeeSkill> EmployeeSkills => Set<Karamchari.TimeAttendance.Domain.Rostering.EmployeeSkill>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.ShiftSwap> ShiftSwaps => Set<Karamchari.TimeAttendance.Domain.Rostering.ShiftSwap>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.ScheduleAlert> ScheduleAlerts => Set<Karamchari.TimeAttendance.Domain.Rostering.ScheduleAlert>();
    public DbSet<Karamchari.TimeAttendance.Domain.Rostering.EmployeeAvailabilityPreference> EmployeeAvailabilityPreferences => Set<Karamchari.TimeAttendance.Domain.Rostering.EmployeeAvailabilityPreference>();

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
    public DbSet<AttendanceAnomaly> AttendanceAnomalies => Set<AttendanceAnomaly>();
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();

    // Phase 3: Attendance Intelligence
    public DbSet<AttendanceViolation> AttendanceViolations => Set<AttendanceViolation>();
    public DbSet<RegularizationRequest> RegularizationRequests => Set<RegularizationRequest>();
    public DbSet<AttendanceScore> AttendanceScores => Set<AttendanceScore>();
    public DbSet<AttendanceShiftPolicy> AttendanceShiftPolicies => Set<AttendanceShiftPolicy>();
    public DbSet<ShiftAssignmentCache> ShiftAssignmentCache => Set<ShiftAssignmentCache>();
    public DbSet<AttendanceAuditLog> AttendanceAuditLog => Set<AttendanceAuditLog>();

    // â"€â"€ Core Attendance/Leave sets â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<LeavePolicyVersion> LeavePolicyVersions => Set<LeavePolicyVersion>();
    public DbSet<EmployeeLeavePolicy> EmployeeLeavePolicies => Set<EmployeeLeavePolicy>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveAccrualSchedule> LeaveAccrualSchedules => Set<LeaveAccrualSchedule>();
    public DbSet<LeaveEncashment> LeaveEncashments => Set<LeaveEncashment>();
    public DbSet<LeaveApproval> LeaveApprovals => Set<LeaveApproval>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<LeaveYear> LeaveYears => Set<LeaveYear>();
    public DbSet<CompOffGrant> CompOffGrants => Set<CompOffGrant>();
    public DbSet<LeaveCarryForward> LeaveCarryForwards => Set<LeaveCarryForward>();
    public DbSet<PayrollLeaveAdvisory> PayrollLeaveAdvisories => Set<PayrollLeaveAdvisory>();
    public DbSet<TeamLeaveProjection> TeamLeaveProjections => Set<TeamLeaveProjection>();
    public DbSet<AttendanceLeaveReconciliation> AttendanceLeaveReconciliations => Set<AttendanceLeaveReconciliation>();
    public DbSet<BradfordFactorScore> BradfordFactorScores => Set<BradfordFactorScore>();
    public DbSet<ApprovalWorkflowDefinition> ApprovalWorkflowDefinitions => Set<ApprovalWorkflowDefinition>();
    public DbSet<ScheduledDomainAction> ScheduledDomainActions => Set<ScheduledDomainAction>();
    public DbSet<LeaveBalanceReadModel> LeaveBalanceReadModels => Set<LeaveBalanceReadModel>();
    public DbSet<LeaveCalendarEntry> LeaveCalendarEntries => Set<LeaveCalendarEntry>();
    public DbSet<LeaveBlackoutPeriod> LeaveBlackoutPeriods => Set<LeaveBlackoutPeriod>();
    public DbSet<EmployeeLeaveSummary> EmployeeLeaveSummaries => Set<EmployeeLeaveSummary>();
    public DbSet<LeaveTimelineEntry> LeaveTimelineEntries => Set<LeaveTimelineEntry>();
    public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
    public DbSet<BulkOperation> BulkOperations => Set<BulkOperation>();
    public DbSet<LeaveLiabilitySnapshot> LeaveLiabilitySnapshots => Set<LeaveLiabilitySnapshot>();
    public DbSet<StatutoryRegisterEntry> StatutoryRegisterEntries => Set<StatutoryRegisterEntry>();
    public DbSet<WorkforceAvailabilityIndex> WorkforceAvailabilityIndices => Set<WorkforceAvailabilityIndex>();

    // -- Sprint 2: Operational Resilience -----------------------------------------
    public DbSet<ApprovalSlaPolicy> ApprovalSlaPolicies => Set<ApprovalSlaPolicy>();
    public DbSet<ApprovalEscalation> ApprovalEscalations => Set<ApprovalEscalation>();
    public DbSet<LeaveInvestigationCase> LeaveInvestigationCases => Set<LeaveInvestigationCase>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<PurgeJob> PurgeJobs => Set<PurgeJob>();
    public DbSet<ComplianceViolation> ComplianceViolations => Set<ComplianceViolation>();

    // -- Sprint 3: Workforce Intelligence ------------------------------------------
    public DbSet<LeaveForecast> LeaveForecasts => Set<LeaveForecast>();
    public DbSet<AnomalyRule> AnomalyRules => Set<AnomalyRule>();
    public DbSet<AnomalyFinding> AnomalyFindings => Set<AnomalyFinding>();
    public DbSet<BurnoutRiskScore> BurnoutRiskScores => Set<BurnoutRiskScore>();
    public DbSet<ManagerLeaveScorecard> ManagerLeaveScorecards => Set<ManagerLeaveScorecard>();
    public DbSet<WorkforceAvailabilityTrend> WorkforceAvailabilityTrends => Set<WorkforceAvailabilityTrend>();

    // -- WorkforcePlanning (extraction candidate: Karamchari.WorkforcePlanning) --
    public DbSet<WorkforceDemand> WorkforceDemands => Set<WorkforceDemand>();
    public DbSet<WorkforceSupply> WorkforceSupplies => Set<WorkforceSupply>();
    public DbSet<CapacityGap> CapacityGaps => Set<CapacityGap>();
    public DbSet<CoverageRisk> CoverageRisks => Set<CoverageRisk>();

    // -- AbsenceManagement (extraction candidate: Karamchari.AbsenceManagement) -
    public DbSet<AbsenceCase> AbsenceCases => Set<AbsenceCase>();
    public DbSet<ReturnToWorkAssessment> ReturnToWorkAssessments => Set<ReturnToWorkAssessment>();
    public DbSet<MedicalRestriction> MedicalRestrictions => Set<MedicalRestriction>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();

    // â"€â"€ Analytics â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

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


        // -- Biometric Engine -------------------------------------------------------

        modelBuilder.Entity<PunchEvent>(b =>
        {
            b.ToTable("Biometric_PunchEvents");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ReceivedAtUtc });
            b.HasIndex(x => new { x.TenantId, x.IdempotencyKey, x.ReceivedAtUtc });
            b.HasIndex(x => new { x.TenantId, x.DeviceId, x.ReceivedAtUtc });
            b.Property(x => x.Direction).HasConversion<string>();
            b.Property(x => x.CaptureMode).HasConversion<string>();
            b.Property(x => x.Modality).HasConversion<string>();
            b.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(1024);
            b.Property(x => x.BiometricMatchScore).HasPrecision(5, 4);
            b.Property(x => x.GpsAccuracyMeters).HasPrecision(10, 2);
            // Immutable — no UPDATE/DELETE permitted. Enforce via interceptor.
        });

        modelBuilder.Entity<BiometricTemplate>(b =>
        {
            b.ToTable("Biometric_Templates");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Modality });
            b.Property(x => x.Modality).HasConversion<string>();
            b.Property(x => x.MatchMode).HasConversion<string>();
            b.Property(x => x.KmsKeyVersion).HasMaxLength(128).IsRequired();
            // EncryptedTemplateBlob: AES-256-GCM ciphertext as Base64. Max ~8KB for most templates.
            b.Property(x => x.EncryptedTemplateBlob).HasMaxLength(16384);
            b.Ignore(x => x.AuthorizedDeviceIds); // stored as separate table
        });

        modelBuilder.Entity<BiometricDevice>(b =>
        {
            b.ToTable("Biometric_Devices");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SerialNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.ZoneId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.SerialNumber).HasMaxLength(128).IsRequired();
            b.Property(x => x.Vendor).HasMaxLength(128).IsRequired();
            b.Property(x => x.Model).HasMaxLength(128).IsRequired();
            b.Property(x => x.FirmwareVersion).HasMaxLength(64).IsRequired();
            b.Property(x => x.ZoneName).HasMaxLength(256);
            b.Property(x => x.BuildingFloorGate).HasMaxLength(256);
        });

        modelBuilder.Entity<BiometricEnrollmentSession>(b =>
        {
            b.ToTable("Biometric_EnrollmentSessions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Modality).HasConversion<string>();
            b.Property(x => x.OperatorId).HasMaxLength(256).IsRequired();
        });

        // -- Geo Engine ------------------------------------------------------------

        modelBuilder.Entity<GeoZone>(b =>
        {
            b.ToTable("Geo_Zones");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.ParentZoneId });
            b.Property(x => x.ZoneType).HasConversion<string>();
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Label).HasMaxLength(128).IsRequired();
            b.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
            b.Property(x => x.PolygonWkt).HasMaxLength(8192);
            b.Property(x => x.CenterLatitude).HasPrecision(10, 7);
            b.Property(x => x.CenterLongitude).HasPrecision(10, 7);
            b.Property(x => x.ApproximateAreaSquareMeters).HasPrecision(18, 2);
        });

        // -- Policy Hierarchy ------------------------------------------------------

        modelBuilder.Entity<AttendancePolicyHierarchy>(b =>
        {
            b.ToTable("Attendance_PolicyHierarchy");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Scope, x.ScopeEntityId }).IsUnique();
            b.Property(x => x.Scope).HasConversion<string>();
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.WeekdayOtMultiplier).HasPrecision(5, 2);
            b.Property(x => x.HolidayOtMultiplier).HasPrecision(5, 2);
        });

        // â"€â"€ Workforce Aggregates Mapping â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

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
            // Non-unique: employees can have two sessions on the same day (split shifts, overtime).
            // Uniqueness is enforced per shift assignment, not per calendar day.
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.WorkDate });
            // Filtered unique: one session per shift assignment. Walk-in sessions (ShiftAssignmentId = Guid.Empty) exempt.
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ShiftAssignmentId })
                .IsUnique()
                .HasFilter("[ShiftAssignmentId] != '00000000-0000-0000-0000-000000000000'");
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
            // Phase 3: unique per (employee, shift) not per (employee, date) — supports split shifts
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ShiftId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.WorkDate });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Ignore(x => x.WorkedHours);
            b.Ignore(x => x.OvertimeHours);
            b.Ignore(x => x.Date);
        });

        // Phase 3: Attendance Intelligence tables
        modelBuilder.Entity<AttendanceViolation>(b =>
        {
            b.ToTable("Attendance_Exceptions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.AttendanceRecordId });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.Severity, x.Status });
            b.Property(x => x.ExceptionType).HasConversion<string>();
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Description).HasMaxLength(1024).IsRequired();
        });

        modelBuilder.Entity<RegularizationRequest>(b =>
        {
            b.ToTable("Attendance_Regularizations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.AttendanceRecordId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Explanation).HasMaxLength(2048).IsRequired();
            b.Property(x => x.Evidence).HasMaxLength(4096);
            b.Property(x => x.RejectionReason).HasMaxLength(1024);
        });

        modelBuilder.Entity<AttendanceScore>(b =>
        {
            b.ToTable("Attendance_Scores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();
            b.Property(x => x.ReliabilityScore).HasPrecision(5, 2);
            b.Property(x => x.ConsistencyScore).HasPrecision(5, 2);
            b.Property(x => x.ComplianceScore).HasPrecision(5, 2);
            b.Ignore(x => x.NoShowDeductionPoints);
            b.Ignore(x => x.MissingPunchDeductionPoints);
            b.Ignore(x => x.OvertimeDeductionPoints);
            b.Ignore(x => x.LocationViolationDeductionPoints);
        });

        modelBuilder.Entity<AttendanceShiftPolicy>(b =>
        {
            b.ToTable("Attendance_ShiftPolicies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationId });
            b.HasIndex(x => new { x.TenantId, x.IsDefault });
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<ShiftAssignmentCache>(b =>
        {
            b.ToTable("Attendance_ShiftAssignmentCache");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ShiftId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.WorkDate });
        });

        modelBuilder.Entity<AttendanceAuditLog>(b =>
        {
            b.ToTable("Attendance_AuditLog");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.AttendanceRecordId, x.ChangedAt });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ChangedAt });
            b.Property(x => x.Action).HasMaxLength(64).IsRequired();
            b.Property(x => x.ChangedBy).HasMaxLength(256).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(1024);
            b.Property(x => x.ChangedFields).HasMaxLength(4096);
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

        // â"€â"€ Legacy Infrastructure (Mismatched but needed for build consistency for now) â"€â"€

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

        modelBuilder.Entity<LeaveType>(b =>
        {
            b.ToTable("Leave_Types");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(16).IsRequired();
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Kind).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<LeavePolicy>(b =>
        {
            b.ToTable("Leave_Policies");
            b.HasKey(x => x.Id);
            b.Property(x => x.Level).HasConversion<string>();
            b.Property(x => x.ScopeKey).HasMaxLength(128);
            b.HasIndex(x => new { x.TenantId, x.LeaveTypeId, x.Level, x.ScopeKey });
            b.OwnsOne(x => x.Rules, r =>
            {
                r.ToJson();
                r.Property(p => p.AccrualFrequency).HasConversion<string>();
                r.Property(p => p.Category).HasConversion<string>();
                r.OwnsMany(p => p.TenureBrackets);
            });
            b.OwnsMany(x => x.Versions, v =>
            {
                v.ToTable("Leave_PolicyVersions");
                v.WithOwner().HasForeignKey("PolicyId");
                v.HasKey(x => x.Id);
                v.OwnsOne(x => x.Rules, r =>
                {
                    r.ToJson();
                    r.Property(p => p.AccrualFrequency).HasConversion<string>();
                    r.Property(p => p.Category).HasConversion<string>();
                    r.OwnsMany(p => p.TenureBrackets);
                });
            });
        });

        modelBuilder.Entity<EmployeeLeavePolicy>(b =>
        {
            b.ToTable("Leave_EmployeePolicies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.LeaveTypeId, x.IsActive });
            b.Property(x => x.ResolvedEntitlement).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LeaveRequest>(b =>
        {
            b.ToTable("Leave_Requests");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Mode).HasConversion<string>();
            b.Property(x => x.HalfDayPeriod).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
            b.OwnsOne(x => x.SubmittedPolicySnapshot, snap =>
            {
                snap.ToJson();
                snap.Property(p => p.NoticePeriodLeaveMode).HasConversion<string>();
            });
        });

        modelBuilder.Entity<LeaveBalance>(b =>
        {
            b.ToTable("Leave_Balances");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PolicyId }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Ignore(x => x.AvailableBalance);

            b.OwnsMany(x => x.Entries, e =>
            {
                e.ToTable("Leave_BalanceEntries");
                e.WithOwner().HasForeignKey("BalanceId");
                e.HasKey(x => x.Id);
                e.Property(x => x.EntryType).HasConversion<string>();
                e.Property(x => x.Quantity).HasPrecision(18, 2);
                e.HasIndex(x => new { x.EmployeeId, x.PolicyId });
            });
        });

        modelBuilder.Entity<LeaveAccrualSchedule>(b =>
        {
            b.ToTable("Leave_AccrualSchedules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PolicyId }).IsUnique();
            b.Property(x => x.Frequency).HasConversion<string>();
            b.Property(x => x.AccruedYtd).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LeaveEncashment>(b =>
        {
            b.ToTable("Leave_Encashments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Days).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LeaveApproval>(b =>
        {
            b.ToTable("Leave_Approvals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LeaveRequestId }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.OwnsMany(x => x.Steps, s =>
            {
                s.ToTable("Leave_ApprovalSteps");
                s.WithOwner().HasForeignKey("ApprovalId");
                s.HasKey(x => x.Id);
                s.Property(x => x.Role).HasConversion<string>();
                s.Property(x => x.Decision).HasConversion<string>();
            });
        });

        modelBuilder.Entity<LeaveYear>(b =>
        {
            b.ToTable("Leave_Years");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(64).IsRequired();
            b.Property(x => x.YearType).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
        });

        modelBuilder.Entity<CompOffGrant>(b =>
        {
            b.ToTable("Leave_CompOffGrants");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.DaysEarned).HasPrecision(18, 2);
            b.Property(x => x.DaysConsumed).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LeaveCarryForward>(b =>
        {
            b.ToTable("Leave_CarryForwards");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PolicyId, x.FromYearId }).IsUnique();
            b.Property(x => x.EligibleDays).HasPrecision(18, 2);
            b.Property(x => x.CarriedDays).HasPrecision(18, 2);
            b.Property(x => x.LapsedDays).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LeaveCalendarEntry>(b =>
        {
            b.ToTable("Projections_LeaveCalendar");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Date });
            b.HasIndex(x => new { x.TenantId, x.Date, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Duration).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LeaveBlackoutPeriod>(b =>
        {
            b.ToTable("Leave_BlackoutPeriods");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
        });

        modelBuilder.Entity<EmployeeLeaveSummary>(b =>
        {
            b.ToTable("Projections_EmployeeLeaveSummary");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.LeaveYearId }).IsUnique();
            b.Property(x => x.TotalDaysOnLeaveYtd).HasPrecision(18, 2);
            b.Property(x => x.TotalLopDaysYtd).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ApprovalDelegation>(b =>
        {
            b.ToTable("Leave_ApprovalDelegations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.DelegatorId, x.Status });
            b.Property(x => x.Scope).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<PayrollLeaveAdvisory>(b =>
        {
            b.ToTable("Leave_PayrollAdvisories");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PayPeriod }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Ignore(x => x.TotalLopDays);
            b.Ignore(x => x.TotalLwpDays);
            b.Ignore(x => x.TotalEncashedDays);
            b.OwnsMany(x => x.Lines, l =>
            {
                l.ToTable("Leave_PayrollAdvisoryLines");
                l.WithOwner().HasForeignKey("AdvisoryId");
                l.HasKey(x => x.Id);
                l.Property(x => x.LineType).HasConversion<string>();
                l.Property(x => x.Days).HasPrecision(18, 2);
            });
        });

        modelBuilder.Entity<TeamLeaveProjection>(b =>
        {
            b.ToTable("Projections_TeamLeave");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.TeamId, x.Date }).IsUnique();
            b.Ignore(x => x.Available);
            b.Ignore(x => x.CapacityPercent);
        });

        modelBuilder.Entity<AttendanceLeaveReconciliation>(b =>
        {
            b.ToTable("Projections_AttendanceLeaveReconciliation");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Date }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.ExpectedLeaveDays).HasPrecision(18, 2);
            b.Property(x => x.ActualAttendanceHours).HasPrecision(18, 2);
        });

        modelBuilder.Entity<BradfordFactorScore>(b =>
        {
            b.ToTable("Compliance_BradfordFactor");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();
            b.Property(x => x.TotalAbsenceDays).HasPrecision(18, 2);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Ignore(x => x.Score);
        });

        modelBuilder.Entity<ApprovalWorkflowDefinition>(b =>
        {
            b.ToTable("Approval_WorkflowDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.WorkflowName).HasMaxLength(128).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EntityType, x.IsActive });
            b.OwnsMany(x => x.Steps, s =>
            {
                s.ToTable("Approval_WorkflowSteps");
                s.WithOwner().HasForeignKey("WorkflowId");
                s.HasKey(x => x.Id);
                s.Property(x => x.Role).HasConversion<string>();
            });
        });

        modelBuilder.Entity<ScheduledDomainAction>(b =>
        {
            b.ToTable("Scheduling_DomainActions");
            b.HasKey(x => x.Id);
            b.Property(x => x.ActionType).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.Status, x.ScheduledFor });
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

        // -- Enterprise Platform Gaps -----------------------------------------

        modelBuilder.Entity<LeaveTimelineEntry>(b =>
        {
            b.ToTable("Audit_LeaveTimeline");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            b.Property(x => x.ActorId).HasMaxLength(256).IsRequired();
            b.Property(x => x.ActorRole).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.LeaveRequestId, x.OccurredAt });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.OccurredAt });
        });

        modelBuilder.Entity<LegalHold>(b =>
        {
            b.ToTable("Compliance_LegalHolds");
            b.HasKey(x => x.Id);
            b.Property(x => x.Scope).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Reason).HasMaxLength(1024).IsRequired();
            b.Property(x => x.CaseReference).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Status, x.Scope, x.ScopeEntityId });
        });

        modelBuilder.Entity<BulkOperation>(b =>
        {
            b.ToTable("Operations_BulkOperations");
            b.HasKey(x => x.Id);
            b.Property(x => x.OperationType).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Description).HasMaxLength(512).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
            b.OwnsMany(x => x.Items, i =>
            {
                i.ToTable("Operations_BulkOperationItems");
                i.WithOwner().HasForeignKey("BulkOperationId");
                i.HasKey(x => x.Id);
                i.Property(x => x.Status).HasConversion<string>();
                i.HasIndex(x => new { x.TenantId, x.TargetEntityId });
            });
        });

        modelBuilder.Entity<LeaveLiabilitySnapshot>(b =>
        {
            b.ToTable("Finance_LeaveLiabilitySnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalEarnedLeaveBalanceDays).HasPrecision(18, 2);
            b.Property(x => x.TotalCarryForwardDays).HasPrecision(18, 2);
            b.Property(x => x.TotalEncashableBalanceDays).HasPrecision(18, 2);
            b.Property(x => x.TotalLopDaysYtd).HasPrecision(18, 2);
            b.Property(x => x.EstimatedLiabilityAmount).HasPrecision(18, 2);
            b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Year, x.Month, x.DepartmentId }).IsUnique();
        });

        modelBuilder.Entity<StatutoryRegisterEntry>(b =>
        {
            b.ToTable("Compliance_StatutoryRegister");
            b.HasKey(x => x.Id);
            b.Property(x => x.RegisterType).HasConversion<string>();
            b.Property(x => x.EntryStatus).HasConversion<string>();
            b.Property(x => x.Days).HasPrecision(18, 2);
            b.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            b.Property(x => x.ClosingBalance).HasPrecision(18, 2);
            b.Property(x => x.EmployeeCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeName).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.RegisterType, x.EmployeeId, x.CalendarYear });
            b.HasIndex(x => new { x.TenantId, x.LeaveRequestId });
        });

        modelBuilder.Entity<WorkforceAvailabilityIndex>(b =>
        {
            b.ToTable("Workforce_AvailabilityIndex");
            b.HasKey(x => x.Id);
            b.Property(x => x.GroupType).HasConversion<string>();
            b.Property(x => x.GroupName).HasMaxLength(256).IsRequired();
            b.Property(x => x.ThresholdPercent).HasPrecision(5, 1);
            b.Ignore(x => x.AvailabilityPercent);
            b.HasIndex(x => new { x.TenantId, x.Date, x.GroupType, x.GroupId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.BelowThreshold, x.Date });
        });

        // -- Sprint 2: Operational Resilience ------------------------------------

        modelBuilder.Entity<ApprovalSlaPolicy>(b =>
        {
            b.ToTable("Approval_SlaPolicies");
            b.HasKey(x => x.Id);
            b.Property(x => x.EscalationAction).HasConversion<string>();
            b.Property(x => x.EscalationTargetRole).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.WorkflowDefinitionId, x.StepSequence }).IsUnique();
        });

        modelBuilder.Entity<ApprovalEscalation>(b =>
        {
            b.ToTable("Approval_Escalations");
            b.HasKey(x => x.Id);
            b.Property(x => x.ActionTaken).HasConversion<string>();
            b.Property(x => x.BreachType).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.LeaveRequestId, x.BreachedAt });
            b.HasIndex(x => new { x.TenantId, x.Resolved });
        });

        modelBuilder.Entity<LeaveInvestigationCase>(b =>
        {
            b.ToTable("Investigation_Cases");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Resolution).HasConversion<string>();
            b.Property(x => x.CaseNumber).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.CaseNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.OwnsMany(x => x.EvidenceLinks, e =>
            {
                e.ToTable("Investigation_EvidenceLinks");
                e.WithOwner().HasForeignKey("CaseId");
                e.HasKey(x => x.Id);
                e.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            });
            b.OwnsMany(x => x.Notes, n =>
            {
                n.ToTable("Investigation_Notes");
                n.WithOwner().HasForeignKey("CaseId");
                n.HasKey(x => x.Id);
                n.Property(x => x.AuthorId).HasMaxLength(256).IsRequired();
            });
        });

        modelBuilder.Entity<RetentionPolicy>(b =>
        {
            b.ToTable("Governance_RetentionPolicies");
            b.HasKey(x => x.Id);
            b.Property(x => x.DataClass).HasConversion<string>();
            b.Property(x => x.DataClassName).HasMaxLength(128).IsRequired();
            b.Property(x => x.LegalBasis).HasMaxLength(512).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.DataClass }).IsUnique();
        });

        modelBuilder.Entity<PurgeJob>(b =>
        {
            b.ToTable("Governance_PurgeJobs");
            b.HasKey(x => x.Id);
            b.Property(x => x.DataClass).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.Status, x.ScheduledAt });
        });

        modelBuilder.Entity<ComplianceViolation>(b =>
        {
            b.ToTable("Compliance_Violations");
            b.HasKey(x => x.Id);
            b.Property(x => x.ViolationType).HasConversion<string>();
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Summary).HasMaxLength(512).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ViolationType, x.DetectedAt });
            b.HasIndex(x => new { x.TenantId, x.Status, x.Severity });
        });

        // -- Sprint 3: Workforce Intelligence ------------------------------------

        modelBuilder.Entity<LeaveForecast>(b =>
        {
            b.ToTable("Intelligence_LeaveForecasts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Granularity).HasConversion<string>();
            b.Property(x => x.ForecastedLeavePercent).HasPrecision(5, 2);
            b.Property(x => x.ConfidenceScore).HasPrecision(5, 2);
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.Granularity, x.PeriodStart }).IsUnique();
        });

        modelBuilder.Entity<AnomalyRule>(b =>
        {
            b.ToTable("Intelligence_AnomalyRules");
            b.HasKey(x => x.Id);
            b.Property(x => x.RuleType).HasConversion<string>();
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.RuleName).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.RuleType, x.IsActive });
        });

        modelBuilder.Entity<AnomalyFinding>(b =>
        {
            b.ToTable("Intelligence_AnomalyFindings");
            b.HasKey(x => x.Id);
            b.Property(x => x.RuleType).HasConversion<string>();
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Summary).HasMaxLength(512).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.DetectedAt });
            b.HasIndex(x => new { x.TenantId, x.Status, x.Severity });
        });

        modelBuilder.Entity<BurnoutRiskScore>(b =>
        {
            b.ToTable("Intelligence_BurnoutRiskScores");
            b.HasKey(x => x.Id);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.OvertimeHoursLast90Days).HasPrecision(18, 2);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ComputationYear, x.ComputationMonth }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RiskLevel, x.ComputationYear, x.ComputationMonth });
        });

        modelBuilder.Entity<ManagerLeaveScorecard>(b =>
        {
            b.ToTable("Intelligence_ManagerScorecards");
            b.HasKey(x => x.Id);
            b.Property(x => x.TeamLeaveUtilizationPercent).HasPrecision(5, 1);
            b.Property(x => x.AverageTeamAvailabilityPercent).HasPrecision(5, 1);
            b.Property(x => x.CompositeScore).HasPrecision(5, 1);
            b.Ignore(x => x.SlaCompliancePercent);
            b.Ignore(x => x.RejectionRate);
            b.HasIndex(x => new { x.TenantId, x.ManagerId, x.Year, x.Month }).IsUnique();
        });

        modelBuilder.Entity<WorkforceAvailabilityTrend>(b =>
        {
            b.ToTable("Intelligence_AvailabilityTrends");
            b.HasKey(x => x.Id);
            b.Property(x => x.Window).HasConversion<string>();
            b.Property(x => x.AverageAvailabilityPercent).HasPrecision(5, 1);
            b.Property(x => x.MinAvailabilityPercent).HasPrecision(5, 1);
            b.Property(x => x.MaxAvailabilityPercent).HasPrecision(5, 1);
            b.Property(x => x.ThresholdPercent).HasPrecision(5, 1);
            b.Property(x => x.ForecastedAvailabilityNextWeek).HasPrecision(5, 1);
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.Window, x.WindowEndDate }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsAtRisk, x.WindowEndDate });
        });

        // -- WorkforcePlanning --------------------------------------------------

        modelBuilder.Entity<WorkforceDemand>(b =>
        {
            b.ToTable("Planning_WorkforceDemands");
            b.HasKey(x => x.Id);
            b.Property(x => x.PeriodType).HasConversion<string>();
            b.Property(x => x.GroupName).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.PeriodStart, x.PeriodEnd });
        });

        modelBuilder.Entity<WorkforceSupply>(b =>
        {
            b.ToTable("Planning_WorkforceSupply");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.DemandId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.PeriodStart });
        });

        modelBuilder.Entity<CapacityGap>(b =>
        {
            b.ToTable("Planning_CapacityGaps");
            b.HasKey(x => x.Id);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.GapPercent).HasPrecision(5, 1);
            b.HasIndex(x => new { x.TenantId, x.DemandId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RiskLevel, x.PeriodStart });
        });

        modelBuilder.Entity<CoverageRisk>(b =>
        {
            b.ToTable("Planning_CoverageRisks");
            b.HasKey(x => x.Id);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.ForecastedLeavePercent).HasPrecision(5, 1);
            b.Property(x => x.RiskReason).HasMaxLength(512).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.GroupId, x.RiskDate });
            b.HasIndex(x => new { x.TenantId, x.RiskLevel, x.RiskDate });
        });

        // -- AbsenceManagement --------------------------------------------------

        modelBuilder.Entity<AbsenceCase>(b =>
        {
            b.ToTable("Absence_Cases");
            b.HasKey(x => x.Id);
            b.Property(x => x.AbsenceType).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Resolution).HasConversion<string>();
            b.Property(x => x.CaseNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.AbsenceDays).HasPrecision(18, 2);
            b.HasIndex(x => new { x.TenantId, x.CaseNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.AbsenceDate });
            b.HasIndex(x => new { x.TenantId, x.Status, x.AbsenceType });
        });

        modelBuilder.Entity<ReturnToWorkAssessment>(b =>
        {
            b.ToTable("Absence_ReturnToWork");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.AbsenceCaseId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ReturnDate });
        });

        modelBuilder.Entity<MedicalRestriction>(b =>
        {
            b.ToTable("Absence_MedicalRestrictions");
            b.HasKey(x => x.Id);
            b.Property(x => x.RestrictionDescription).HasMaxLength(1024).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.EffectiveFrom, x.EffectiveTo });
        });

        // -- Phase 2: Rostering -------------------------------------------------------

        modelBuilder.Entity<Domain.Rostering.Roster>(b =>
        {
            b.ToTable("Roster_Rosters");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasMany(x => x.Shifts).WithOne().HasForeignKey(s => s.RosterId);
            b.HasMany(x => x.Assignments).WithOne().HasForeignKey(a => a.RosterId);
            b.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Domain.Rostering.RosterShiftAssignment>(b =>
        {
            b.ToTable("Roster_ShiftAssignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.HasIndex(x => new { x.RosterShiftId, x.EmployeeId }).IsUnique();
            b.HasIndex(x => new { x.RosterId, x.WorkDate });
            b.HasIndex(x => new { x.EmployeeId, x.WorkDate });
        });

        modelBuilder.Entity<Domain.Rostering.RosterShift>(b =>
        {
            b.ToTable("Roster_Shifts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Type).HasConversion<string>();
            b.OwnsMany(x => x.RequiredSkills, sk =>
            {
                sk.ToTable("Roster_ShiftSkillRequirements");
                sk.WithOwner().HasForeignKey("RosterShiftId");
                sk.Property(s => s.SkillName).HasMaxLength(128).IsRequired();
            });
            b.HasIndex(x => new { x.RosterId, x.WorkDate });
            b.HasIndex(x => new { x.TenantId, x.LocationId, x.WorkDate });
        });

        modelBuilder.Entity<Domain.Rostering.CoverageRule>(b =>
        {
            b.ToTable("Roster_CoverageRules");
            b.HasKey(x => x.Id);
            b.Property(x => x.LocationName).HasMaxLength(256).IsRequired();
            b.OwnsMany(x => x.SkillMix, sm =>
            {
                sm.ToTable("Roster_CoverageSkillMix");
                sm.WithOwner().HasForeignKey("CoverageRuleId");
                sm.Property(s => s.SkillName).HasMaxLength(128).IsRequired();
            });
            b.HasIndex(x => new { x.TenantId, x.LocationId, x.IsActive });
        });

        modelBuilder.Entity<Domain.Rostering.EmployeeSkill>(b =>
        {
            b.ToTable("Roster_EmployeeSkills");
            b.HasKey(x => x.Id);
            b.Property(x => x.SkillName).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.SkillId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.SkillId, x.ExpiryDate });
        });

        modelBuilder.Entity<Domain.Rostering.ShiftSwap>(b =>
        {
            b.ToTable("Roster_ShiftSwaps");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RejectionReason).HasMaxLength(512);
            b.HasIndex(x => new { x.TenantId, x.RequestedByEmployeeId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.Status, x.RequestedAtUtc });
        });

        modelBuilder.Entity<Domain.Rostering.ScheduleAlert>(b =>
        {
            b.ToTable("Roster_ScheduleAlerts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Message).HasMaxLength(512).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.RosterId, x.Type, x.IsResolved });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.IsResolved });
        });

        modelBuilder.Entity<Domain.Rostering.EmployeeAvailabilityPreference>(b =>
        {
            b.ToTable("Roster_EmployeeAvailability");
            b.HasKey(x => x.Id);
            b.Property(x => x.DayOfWeek).HasConversion<int>();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.DayOfWeek });
        });
    }
}
