// -----------------------------------------------------------------------
// <copyright file="IndiaPayrollEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.CountryEngine;

using Karamchari.Payroll.Domain.Compliance;
using Karamchari.Payroll.Domain.CountryEngine;
using Karamchari.Payroll.Domain.CountryEngine.Results;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Compliance;
using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// India-specific implementation of <see cref="IPayrollCountryEngine"/>.
/// Delegates to the existing statutory pipeline (EPF, ESIC, PT, TDS),
/// tax calculator, and compliance generators — all of which encode Indian law.
///
/// The ruleset is constructed per-call (not injected as a singleton) because
/// <see cref="FY20262027RuleSet"/> requires tenant-specific component IDs
/// (EPF wage base, Basic+DA, HRA) that vary by salary template configuration
/// and are supplied via <see cref="PayrollContext"/>.
///
/// Adding a second country means creating a new <see cref="IPayrollCountryEngine"/>
/// implementation — this class is not extended.
/// </summary>
public sealed class IndiaPayrollEngine : IPayrollCountryEngine
{
    private readonly IStatutoryRuleSetFactory _ruleSetFactory;
    private readonly ITaxCalculatorService _taxCalculator;
    private readonly ITdsGenerator _tdsGenerator;
    private readonly IEcrGenerator _ecrGenerator;
    private readonly IEsicGenerator _esicGenerator;

    public string CountryCode => "IN";

    public IndiaPayrollEngine(
        IStatutoryRuleSetFactory ruleSetFactory,
        ITaxCalculatorService taxCalculator,
        ITdsGenerator tdsGenerator,
        IEcrGenerator ecrGenerator,
        IEsicGenerator esicGenerator)
    {
        _ruleSetFactory = ruleSetFactory;
        _taxCalculator = taxCalculator;
        _tdsGenerator = tdsGenerator;
        _ecrGenerator = ecrGenerator;
        _esicGenerator = esicGenerator;
    }

    // ── Metadata ─────────────────────────────────────────────────────────────

    public CountryMetadata GetCountryMetadata() => IndiaCountryConstants.Metadata;

    public StatutoryReportingCalendar GetStatutoryReportingCalendar() => IndiaCountryConstants.ReportingCalendar;

    public PayrollCalendar GetPayrollCalendar() => IndiaCountryConstants.PayrollCalendar;

    // ── Eligibility ───────────────────────────────────────────────────────────

    public EmployeeEligibilityResult ValidateEmployeeEligibility(PayrollContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(context.Profile.Pan))
            errors.Add("PAN is mandatory for payroll processing in India (Income Tax Act).");

        if (context.Profile.AnnualCTC <= 0)
            errors.Add("Annual CTC must be greater than zero before processing payroll.");

        if (context.Profile.SalaryTemplateId == Guid.Empty)
            errors.Add("A salary template must be assigned before running payroll.");

        if (string.IsNullOrWhiteSpace(context.Profile.Uan))
            warnings.Add("UAN is missing — EPF contributions cannot be deposited to the employee's PF account.");

        if (string.IsNullOrWhiteSpace(context.Profile.EsicNumber)
            && context.Breakdown.MonthlyGross <= 21_000m)
            warnings.Add("ESIC Insurance Number is missing for an employee eligible for ESIC (Gross ≤ ₹21,000).");

        return new EmployeeEligibilityResult(errors.Count == 0, errors, warnings);
    }

    // ── Tax Estimation (sync, no DB) ──────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Uses projected income without DB-sourced IT declaration approvals.
    /// PF and PT amounts default to zero — pass them explicitly if already known.
    /// For accurate TDS including declarations, use <see cref="CalculateStatutoryDeductionsAsync"/>.
    /// </remarks>
    public TaxCalculationResult CalculateTax(PayrollContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var input = new TaxCalculationInput(
            EmployeeId: context.Profile.EmployeeId,
            TaxRegime: context.Profile.TaxRegime.ToString(),
            Month: context.PayrollMonth,
            FinancialYear: context.FinancialYear.StartYear,
            YearToDateGross: context.YearToDateGross,
            YearToDateTds: context.YearToDateTds,
            CurrentMonthGross: context.Breakdown.MonthlyGross,
            PfDeduction: 0m,
            ProfessionalTax: 0m,
            DeclaredInvestments: context.DeclaredInvestments);

        var output = _taxCalculator.Calculate(input);

        return new TaxCalculationResult(
            EmployeeId: context.Profile.EmployeeId,
            TaxableIncome: output.TaxableIncome,
            AnnualTaxLiability: output.TaxLiability,
            TdsThisMonth: output.TdsThisMonth,
            YearToDateTdsDeducted: context.YearToDateTds,
            TaxRegime: context.Profile.TaxRegime.ToString(),
            RuleVersionId: output.RuleVersionId);
    }

    // ── Full Statutory Pipeline (async, reads IT declarations from DB) ─────────

    public async Task<StatutoryDeductionResult> CalculateStatutoryDeductionsAsync(
        PayrollContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Rule set is selected by EffectiveDate, not system date, so retro-pay and
        // historical audits always reproduce the rules active during the original period.
        var ruleSet = _ruleSetFactory.Create(
            context.FinancialYear,
            context.EpfWageComponentIds,
            context.BasicComponentIds,
            context.HraComponentIds);

        var statutoryContext = new StatutoryContext(
            context.Breakdown,
            context.Profile,
            context.FinancialYear,
            context.PayrollMonth);

        var result = await StatutoryPipelineEngine.ExecuteAsync(statutoryContext, ruleSet);

        return new StatutoryDeductionResult(
            NetPay: result.NetPay,
            Deductions: result.Deductions);
    }

    // ── Payroll Result Aggregation ─────────────────────────────────────────────

    public async Task<PayrollCalculationResult> CalculatePayrollAsync(
        PayrollContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var deductions = await CalculateStatutoryDeductionsAsync(context, cancellationToken);

        var earnings = context.Breakdown.MonthlyBreakdown
            .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);

        return new PayrollCalculationResult(
            EmployeeId: context.Profile.EmployeeId,
            EmployeeName: context.Profile.EmployeeName,
            GrossPay: context.Breakdown.MonthlyGross,
            TotalDeductions: deductions.TotalDeductions,
            NetPay: deductions.NetPay,
            EarningsBreakdown: earnings,
            DeductionsBreakdown: deductions.Deductions);
    }

    // ── Compliance Reports ─────────────────────────────────────────────────────

    public IReadOnlyList<ComplianceReport> GenerateComplianceReports(
        ReportingPeriod period,
        IReadOnlyList<EmployeePayrollSummary> payrollResults)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(payrollResults);

        return
        [
            GenerateTdsReport(period, payrollResults),
            GenerateEcrReport(period, payrollResults),
            GenerateEsicReport(period, payrollResults)
        ];
    }

    private ComplianceReport GenerateTdsReport(
        ReportingPeriod period,
        IReadOnlyList<EmployeePayrollSummary> summaries)
    {
        var records = summaries.Select(s => new TdsRecord
        {
            Pan = s.StatutoryIdentifiers.GetValueOrDefault("PAN", string.Empty),
            EmployeeName = s.EmployeeName,
            AnnualIncome = s.GrossPay * 12,
            TotalTax = s.Deductions.GetValueOrDefault("TDS") * 12,
            TdsDeducted = s.Deductions.GetValueOrDefault("TDS"),
            RemainingTax = 0m
        });

        var obligation = IndiaCountryConstants.ReportingCalendar.Obligations
            .First(o => o.Code == "TDS_MONTHLY");
        return new ComplianceReport(
            ReportCode: "TDS_MONTHLY",
            DisplayName: "Monthly TDS Deduction Statement",
            Format: ComplianceReportFormat.FixedWidth,
            TextContent: _tdsGenerator.Generate(records),
            BinaryContent: null,
            DueDate: obligation.CalculateDueDate(period.EndDate));
    }

    private ComplianceReport GenerateEcrReport(
        ReportingPeriod period,
        IReadOnlyList<EmployeePayrollSummary> summaries)
    {
        var records = summaries.Select(s =>
        {
            var ext = s.ExtendedData;
            var epfContribution = s.Deductions.GetValueOrDefault("EPF");
            var epsContribution = ext.GetValueOrDefault("EPS_CONTRIBUTION");
            return new EcrRecord
            {
                Uan = s.StatutoryIdentifiers.GetValueOrDefault("UAN", string.Empty),
                MemberName = s.EmployeeName,
                GrossWages = s.GrossPay,
                EpfWages = ext.GetValueOrDefault("EPF_WAGES"),
                EpsWages = ext.GetValueOrDefault("EPS_WAGES"),
                EdliWages = ext.GetValueOrDefault("EDLI_WAGES"),
                EpfContribution = epfContribution,
                EpsContribution = epsContribution,
                EpfEpsDiff = epfContribution - epsContribution,
                NcpDays = (int)ext.GetValueOrDefault("NCP_DAYS"),
                RefundOfAdvances = 0m
            };
        });

        var obligation = IndiaCountryConstants.ReportingCalendar.Obligations
            .First(o => o.Code == "EPF_ECR");
        return new ComplianceReport(
            ReportCode: "EPF_ECR",
            DisplayName: "EPF Electronic Challan cum Return",
            Format: ComplianceReportFormat.Csv,
            TextContent: _ecrGenerator.Generate(records),
            BinaryContent: null,
            DueDate: obligation.CalculateDueDate(period.EndDate));
    }

    private ComplianceReport GenerateEsicReport(
        ReportingPeriod period,
        IReadOnlyList<EmployeePayrollSummary> summaries)
    {
        // ESIC applies only to employees with Gross <= ₹21,000/month
        var eligible = summaries
            .Where(s => s.GrossPay <= 21_000m)
            .Select(s => new EsicRecord
            {
                InsuranceNumber = s.StatutoryIdentifiers.GetValueOrDefault("ESIC_NUMBER", string.Empty),
                EmployeeName = s.EmployeeName,
                GrossWages = s.GrossPay,
                EmployeeContribution = s.Deductions.GetValueOrDefault("ESIC"),
                EmployerContribution = Math.Round(s.GrossPay * 0.0325m, 0),
                DaysWorked = (int)s.ExtendedData.GetValueOrDefault("DAYS_WORKED", 26)
            });

        var obligation = IndiaCountryConstants.ReportingCalendar.Obligations
            .First(o => o.Code == "ESIC_RETURN");
        return new ComplianceReport(
            ReportCode: "ESIC_RETURN",
            DisplayName: "ESIC Monthly Contribution Return",
            Format: ComplianceReportFormat.Excel,
            TextContent: null,
            BinaryContent: _esicGenerator.Generate(eligible),
            DueDate: obligation.CalculateDueDate(period.EndDate));
    }
}
