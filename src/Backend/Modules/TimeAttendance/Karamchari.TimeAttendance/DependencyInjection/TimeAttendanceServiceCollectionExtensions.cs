// -----------------------------------------------------------------------
// <copyright file="TimeAttendanceServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.TimeAttendance.DependencyInjection;

/// <summary>
/// Extension methods for registering Time and Attendance module services.
/// Phase 1C: Workforce Operational Intelligence Platform.
/// </summary>
public static class TimeAttendanceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariTimeAttendance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariTimeAttendanceConsumers(busConfigurator);
        }

        // â”€â”€ RLS: Workforce operational intelligence (Phase 1C) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // RLS: Phase 2 Rostering tables
        services.RegisterTenantTable("Roster_Rosters");
        services.RegisterTenantTable("Roster_ShiftAssignments");
        services.RegisterTenantTable("Roster_CoverageRules");
        services.RegisterTenantTable("Roster_CoverageSkillMix");
        services.RegisterTenantTable("Roster_EmployeeSkills");
        services.RegisterTenantTable("Roster_ShiftSwaps");
        services.RegisterTenantTable("Roster_ScheduleAlerts");
        services.RegisterTenantTable("Roster_Shifts");
        services.RegisterTenantTable("Roster_ShiftSkillRequirements");
        services.RegisterTenantTable("Roster_EmployeeAvailability");

        // -- Biometric Engine --------------------------------------------------------
        services.RegisterTenantTable("Biometric_PunchEvents");
        services.RegisterTenantTable("Biometric_Templates");
        services.RegisterTenantTable("Biometric_Devices");
        services.RegisterTenantTable("Biometric_EnrollmentSessions");

        // -- Geo Engine --------------------------------------------------------------
        services.RegisterTenantTable("Geo_Zones");

        // -- Policy Hierarchy --------------------------------------------------------
        services.RegisterTenantTable("Attendance_PolicyHierarchy");

        services.RegisterTenantTable("Workforce_ShiftDefinitions");
        services.RegisterTenantTable("Workforce_Schedules");
        services.RegisterTenantTable("Workforce_ShiftAssignments");
        services.RegisterTenantTable("Workforce_AttendanceSessions");
        services.RegisterTenantTable("Workforce_AttendanceEvents");
        services.RegisterTenantTable("Workforce_AttendanceAnomalies");
        services.RegisterTenantTable("Workforce_AttendancePolicies");
        services.RegisterTenantTable("Workforce_ComplianceRules");
        services.RegisterTenantTable("Workforce_AttendanceRecords");

        // Phase 3: Attendance Intelligence
        services.RegisterTenantTable("Attendance_Exceptions");
        services.RegisterTenantTable("Attendance_Regularizations");
        services.RegisterTenantTable("Attendance_Scores");
        services.RegisterTenantTable("Attendance_ShiftPolicies");
        services.RegisterTenantTable("Attendance_ShiftAssignmentCache");
        services.RegisterTenantTable("Attendance_AuditLog");

        // â”€â”€ Legacy Core Tables â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        services.RegisterTenantTable("HolidayCalendars");
        services.RegisterTenantTable("Holidays");
        services.RegisterTenantTable("Leave_Types");
        services.RegisterTenantTable("Leave_Policies");
        services.RegisterTenantTable("Leave_PolicyVersions");
        services.RegisterTenantTable("Leave_EmployeePolicies");
        services.RegisterTenantTable("Leave_Requests");
        services.RegisterTenantTable("Leave_Balances");
        services.RegisterTenantTable("Leave_BalanceEntries");
        services.RegisterTenantTable("Leave_AccrualSchedules");
        services.RegisterTenantTable("Leave_Encashments");
        services.RegisterTenantTable("Leave_Approvals");
        services.RegisterTenantTable("Leave_ApprovalSteps");
        services.RegisterTenantTable("Leave_Years");
        services.RegisterTenantTable("Leave_CompOffGrants");
        services.RegisterTenantTable("Leave_CarryForwards");
        services.RegisterTenantTable("Leave_BlackoutPeriods");
        services.RegisterTenantTable("Leave_ApprovalDelegations");
        services.RegisterTenantTable("Leave_PayrollAdvisories");
        services.RegisterTenantTable("Leave_PayrollAdvisoryLines");
        services.RegisterTenantTable("Projections_LeaveCalendar");
        services.RegisterTenantTable("Projections_EmployeeLeaveSummary");
        services.RegisterTenantTable("Projections_TeamLeave");
        services.RegisterTenantTable("Projections_AttendanceLeaveReconciliation");
        services.RegisterTenantTable("Compliance_BradfordFactor");
        services.RegisterTenantTable("Approval_WorkflowDefinitions");
        services.RegisterTenantTable("Approval_WorkflowSteps");
        services.RegisterTenantTable("Scheduling_DomainActions");
        services.RegisterTenantTable("Timesheets");

        // â”€â”€ Analytics â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        services.RegisterTenantTable("Analytics_ProjectMetrics");
        services.RegisterTenantTable("Analytics_ProcessedEventLog");

        // -- Enterprise Platform Gaps -----------------------------------------
        services.RegisterTenantTable("Audit_LeaveTimeline");
        services.RegisterTenantTable("Compliance_LegalHolds");
        services.RegisterTenantTable("Operations_BulkOperations");
        services.RegisterTenantTable("Operations_BulkOperationItems");
        services.RegisterTenantTable("Finance_LeaveLiabilitySnapshots");
        services.RegisterTenantTable("Compliance_StatutoryRegister");
        services.RegisterTenantTable("Workforce_AvailabilityIndex");

        // -- Sprint 2: Operational Resilience -----------------------------------------
        services.RegisterTenantTable("Approval_SlaPolicies");
        services.RegisterTenantTable("Approval_Escalations");
        services.RegisterTenantTable("Investigation_Cases");
        services.RegisterTenantTable("Investigation_EvidenceLinks");
        services.RegisterTenantTable("Investigation_Notes");
        services.RegisterTenantTable("Governance_RetentionPolicies");
        services.RegisterTenantTable("Governance_PurgeJobs");
        services.RegisterTenantTable("Compliance_Violations");

        // -- Sprint 3: Workforce Intelligence -----------------------------------------
        services.RegisterTenantTable("Intelligence_LeaveForecasts");
        services.RegisterTenantTable("Intelligence_AnomalyRules");
        services.RegisterTenantTable("Intelligence_AnomalyFindings");
        services.RegisterTenantTable("Intelligence_BurnoutRiskScores");
        services.RegisterTenantTable("Intelligence_ManagerScorecards");
        services.RegisterTenantTable("Intelligence_AvailabilityTrends");

        // -- WorkforcePlanning (extraction candidate) --------------------------
        services.RegisterTenantTable("Planning_WorkforceDemands");
        services.RegisterTenantTable("Planning_WorkforceSupply");
        services.RegisterTenantTable("Planning_CapacityGaps");
        services.RegisterTenantTable("Planning_CoverageRisks");

        // -- AbsenceManagement (extraction candidate) --------------------------
        services.RegisterTenantTable("Absence_Cases");
        services.RegisterTenantTable("Absence_ReturnToWork");
        services.RegisterTenantTable("Absence_MedicalRestrictions");

        services.AddDbContext<TimeAttendanceDbContext>((serviceProvider, options) =>
        {
            string connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before TimeAttendanceDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<LeaveRequestService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.LeaveBalanceService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.ILeavePolicyResolver,
                           Karamchari.TimeAttendance.Services.LeavePolicyResolver>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.LeaveValidationEngine>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.BalanceValidationRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.NoticePeriodRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.ConsecutiveDaysRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.OverlapRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.BlackoutRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.SandwichRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.PrefixSuffixRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.Validation.ILeaveValidationRule,
                           Karamchari.TimeAttendance.Services.Validation.Rules.RetroactiveLeaveRule>();
        services.AddScoped<Karamchari.TimeAttendance.Services.HolidayRecalculationService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.IApprovalWorkflowResolver,
                           Karamchari.TimeAttendance.Services.ApprovalWorkflowResolver>();
        services.AddScoped<Karamchari.TimeAttendance.Services.LeaveImpactAssessmentService>();
        services.AddScoped<Karamchari.Core.Projections.IProjectionRebuilder,
                           Karamchari.TimeAttendance.Services.LeaveProjectionRebuilder>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AttendanceReconciliationService>();
        services.AddScoped<Karamchari.Core.Projections.IProjectionRebuilder, Karamchari.TimeAttendance.Services.LeaveBalanceProjectionRebuilder>();
        services.AddScoped<Karamchari.TimeAttendance.Services.TimesheetService>();
        services.AddSingleton<Karamchari.TimeAttendance.Services.ICapacityProvider,
                              Karamchari.TimeAttendance.Services.NullCapacityProvider>();

        services.AddScoped<Karamchari.TimeAttendance.Services.ShiftRosteringEngine>();
        services.AddScoped<Karamchari.TimeAttendance.Services.PolicySimulationService>();

        // -- Biometric + Geo + Policy Hierarchy (Phase Attendance) --------------------
        services.AddScoped<Karamchari.TimeAttendance.Services.GeoConfidenceScorer>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AttendancePolicyHierarchyResolver>();
        services.AddScoped<Karamchari.TimeAttendance.Services.PunchIngestionService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.CompOffAccrualService>();
        services.AddScoped<Karamchari.TimeAttendance.Services.ConsecutiveAbsenceMonitor>();

        // Phase 3: Attendance Intelligence
        services.AddSingleton<Karamchari.TimeAttendance.Observability.AttendanceMetrics>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AttendanceProcessingEngine>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AttendanceScoreCalculator>();
        services.AddScoped<Karamchari.TimeAttendance.Services.ShiftResolver>();

        services.AddScoped<Karamchari.TimeAttendance.Services.ShiftAssignmentCacheRebuilder>();

        // Period finalization
        services.AddScoped<Karamchari.TimeAttendance.Services.AttendancePeriodFinalizationService>();

        // Intelligence pipeline — 4 focused analyzers + thin facade
        services.AddScoped<Karamchari.TimeAttendance.Services.TrendAnalyzer>();
        services.AddScoped<Karamchari.TimeAttendance.Services.CoverageAnalyzer>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AbsenceTrendAnalyzer>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AlertGenerator>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AbsenceRiskPredictor>();
        services.AddScoped<Karamchari.TimeAttendance.Services.AttendanceIntelligenceService>();

        return services;
    }

    /// <summary>
    /// Registers Time and Attendance module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariTimeAttendanceConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TenantProvisionedConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TimesheetApprovedConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.TimesheetApprovedAnalyticsConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.BillingAnalyticsConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.WorkflowCompletedConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.ScheduledShiftCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.TimeAttendance.Consumers.ShiftUnassignedConsumer>();
    }
}
