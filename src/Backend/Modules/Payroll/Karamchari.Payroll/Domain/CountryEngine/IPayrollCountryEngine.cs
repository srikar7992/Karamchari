// -----------------------------------------------------------------------
// <copyright file="IPayrollCountryEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine;

using Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// Defines the contract that every country-specific payroll implementation must satisfy.
/// Country rules become plug-ins: IndiaPayrollEngine, UAEPayrollEngine, UKPayrollEngine, etc.
///
/// Method categories:
/// - Metadata (sync): Static declarations about the country — used by UI and scheduler.
/// - Calculations (async): Statutory deduction pipeline requires DB lookups (IT declarations).
/// - Sync calculations: Pure math with no I/O (tax estimation, eligibility checks).
/// - Report generation (sync): Data already computed; generators are deterministic.
/// </summary>
public interface IPayrollCountryEngine
{
    /// <summary>ISO 3166-1 alpha-2 country code this engine handles (e.g., "IN", "AE", "GB").</summary>
    string CountryCode { get; }

    // ── Metadata ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns static metadata about this country's payroll environment.
    /// The UI uses this to drive field visibility, labels, and validation patterns
    /// without any country-conditional logic in the frontend.
    /// </summary>
    CountryMetadata GetCountryMetadata();

    /// <summary>
    /// Returns the compliance filing calendar for this country.
    /// The scheduling engine reads this to create and track compliance tasks automatically.
    /// </summary>
    StatutoryReportingCalendar GetStatutoryReportingCalendar();

    /// <summary>
    /// Returns payroll period structure rules for this country.
    /// Used when creating pay periods and validating payroll schedules.
    /// </summary>
    PayrollCalendar GetPayrollCalendar();

    // ── Calculations ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validates that an employee's profile satisfies all statutory prerequisites
    /// for this country before a pay run begins.
    /// </summary>
    EmployeeEligibilityResult ValidateEmployeeEligibility(PayrollContext context);

    /// <summary>
    /// Synchronous tax estimate using projected income and declared investments.
    /// Suitable for previews and simulations. Does not consult IT declaration approvals.
    /// For TDS with approved declarations use <see cref="CalculateStatutoryDeductionsAsync"/>.
    /// </summary>
    TaxCalculationResult CalculateTax(PayrollContext context);

    /// <summary>
    /// Runs the full statutory deduction pipeline for one employee in one pay period.
    /// Async because TDS calculation reads approved IT declarations from the database.
    /// </summary>
    Task<StatutoryDeductionResult> CalculateStatutoryDeductionsAsync(
        PayrollContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Produces the aggregated payroll result for one employee combining gross, deductions, and net.
    /// Async because it orchestrates <see cref="CalculateStatutoryDeductionsAsync"/> internally.
    /// </summary>
    Task<PayrollCalculationResult> CalculatePayrollAsync(
        PayrollContext context,
        CancellationToken cancellationToken = default);

    // ── Report Generation ────────────────────────────────────────────────────

    /// <summary>
    /// Generates all statutory compliance reports due for the given period.
    /// Input summaries must be fully computed before calling this method.
    /// Each report's <see cref="ComplianceReport.DueDate"/> is derived from
    /// the <see cref="GetStatutoryReportingCalendar"/> calendar.
    /// </summary>
    IReadOnlyList<ComplianceReport> GenerateComplianceReports(
        ReportingPeriod period,
        IReadOnlyList<EmployeePayrollSummary> payrollResults);
}
