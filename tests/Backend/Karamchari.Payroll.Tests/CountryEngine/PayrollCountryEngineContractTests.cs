// -----------------------------------------------------------------------
// <copyright file="PayrollCountryEngineContractTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Tests.CountryEngine;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.CountryEngine;
using Karamchari.Payroll.Domain.CountryEngine.Results;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Abstract contract test suite that every <see cref="IPayrollCountryEngine"/> implementation must pass.
///
/// Usage: derive a concrete test class, implement <see cref="CreateEngine"/> and
/// <see cref="BuildValidContext"/>, and all contract tests run automatically.
///
/// This prevents interface drift as new country engines are added. Any engine that
/// fails these tests violates the contract — not the contract's fault.
/// </summary>
public abstract class PayrollCountryEngineContractTests
{
    protected abstract IPayrollCountryEngine CreateEngine();

    /// <summary>Builds a context with sufficient data for the engine under test to process without errors.</summary>
    protected abstract PayrollContext BuildValidContext();

    // ── Metadata ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetCountryMetadata_Returns_Non_Empty_CountryCode()
    {
        var engine = CreateEngine();
        var metadata = engine.GetCountryMetadata();

        Assert.NotNull(metadata);
        Assert.False(string.IsNullOrWhiteSpace(metadata.CountryCode));
        Assert.Equal(engine.CountryCode, metadata.CountryCode);
    }

    [Fact]
    public void GetCountryMetadata_Returns_At_Least_One_RequiredIdentifier()
    {
        var metadata = CreateEngine().GetCountryMetadata();

        Assert.NotNull(metadata.RequiredEmployeeIdentifiers);
        Assert.NotEmpty(metadata.RequiredEmployeeIdentifiers);
    }

    [Fact]
    public void GetCountryMetadata_Returns_At_Least_One_StatutoryProgram()
    {
        var metadata = CreateEngine().GetCountryMetadata();

        Assert.NotNull(metadata.SupportedStatutoryPrograms);
        Assert.NotEmpty(metadata.SupportedStatutoryPrograms);
    }

    [Fact]
    public void GetCountryMetadata_Returns_Non_Empty_Currency()
    {
        var metadata = CreateEngine().GetCountryMetadata();

        Assert.False(string.IsNullOrWhiteSpace(metadata.DefaultCurrency));
    }

    // ── Compliance Calendar ───────────────────────────────────────────────────

    [Fact]
    public void GetStatutoryReportingCalendar_Returns_Same_CountryCode()
    {
        var engine = CreateEngine();
        var calendar = engine.GetStatutoryReportingCalendar();

        Assert.NotNull(calendar);
        Assert.Equal(engine.CountryCode, calendar.CountryCode);
    }

    [Fact]
    public void GetStatutoryReportingCalendar_Returns_At_Least_One_Obligation()
    {
        var calendar = CreateEngine().GetStatutoryReportingCalendar();

        Assert.NotNull(calendar.Obligations);
        Assert.NotEmpty(calendar.Obligations);
    }

    [Fact]
    public void GetStatutoryReportingCalendar_All_Obligations_Have_Codes()
    {
        var calendar = CreateEngine().GetStatutoryReportingCalendar();

        Assert.All(calendar.Obligations, o =>
            Assert.False(string.IsNullOrWhiteSpace(o.Code)));
    }

    [Fact]
    public void GetStatutoryReportingCalendar_All_DueDateRules_Produce_Future_Date()
    {
        var calendar = CreateEngine().GetStatutoryReportingCalendar();
        var periodEnd = new DateOnly(2026, 3, 31);

        Assert.All(calendar.Obligations, o =>
        {
            var dueDate = o.CalculateDueDate(periodEnd);
            Assert.True(dueDate > periodEnd,
                $"Obligation '{o.Code}': due date {dueDate} must be after period end {periodEnd}.");
        });
    }

    [Fact]
    public void GetStatutoryReportingCalendar_Obligation_Codes_Are_Unique()
    {
        var calendar = CreateEngine().GetStatutoryReportingCalendar();
        var codes = calendar.Obligations.Select(o => o.Code).ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    // ── Payroll Calendar ──────────────────────────────────────────────────────

    [Fact]
    public void GetPayrollCalendar_Returns_Same_CountryCode()
    {
        var engine = CreateEngine();
        var calendar = engine.GetPayrollCalendar();

        Assert.NotNull(calendar);
        Assert.Equal(engine.CountryCode, calendar.CountryCode);
    }

    [Fact]
    public void GetPayrollCalendar_DefaultFrequency_Is_In_SupportedList()
    {
        var calendar = CreateEngine().GetPayrollCalendar();

        Assert.Contains(calendar.DefaultFrequency, calendar.SupportedFrequencies);
    }

    [Fact]
    public void GetPayrollCalendar_ProcessingDay_Is_Valid()
    {
        var calendar = CreateEngine().GetPayrollCalendar();

        Assert.InRange(calendar.DefaultProcessingDayOfMonth, 1, 28);
    }

    // ── Eligibility ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateEmployeeEligibility_Returns_Result_For_Valid_Context()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = engine.ValidateEmployeeEligibility(context);

        Assert.NotNull(result);
        Assert.NotNull(result.Errors);
        Assert.NotNull(result.Warnings);
    }

    [Fact]
    public void ValidateEmployeeEligibility_Returns_Ineligible_When_AnnualCTC_Is_Zero()
    {
        var engine = CreateEngine();
        var base_context = BuildValidContext();

        // Override profile to have zero CTC
        var zeroCtcProfile = PayrollProfile.CreateDraft(Guid.NewGuid(), "Test Employee");
        var context = base_context with
        {
            Profile = zeroCtcProfile,
            Breakdown = new CTCBreakdownResult(0m, 0m,
                new Dictionary<Guid, decimal>(),
                new Dictionary<Guid, decimal>())
        };

        var result = engine.ValidateEmployeeEligibility(context);

        Assert.False(result.IsEligible);
        Assert.NotEmpty(result.Errors);
    }

    // ── Tax Calculation ───────────────────────────────────────────────────────

    [Fact]
    public void CalculateTax_Returns_Non_Negative_TdsThisMonth()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = engine.CalculateTax(context);

        Assert.NotNull(result);
        Assert.True(result.TdsThisMonth >= 0,
            $"TDS this month must be non-negative; got {result.TdsThisMonth}.");
    }

    [Fact]
    public void CalculateTax_Returns_Result_For_Valid_Context()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = engine.CalculateTax(context);

        Assert.NotNull(result);
        Assert.Equal(context.Profile.EmployeeId, result.EmployeeId);
        Assert.False(string.IsNullOrWhiteSpace(result.TaxRegime));
        Assert.False(string.IsNullOrWhiteSpace(result.RuleVersionId));
    }

    // ── Statutory Deductions ──────────────────────────────────────────────────

    [Fact]
    public async Task CalculateStatutoryDeductionsAsync_Returns_Non_Negative_NetPay()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = await engine.CalculateStatutoryDeductionsAsync(context);

        Assert.NotNull(result);
        Assert.True(result.NetPay >= 0,
            $"Net pay must be non-negative; got {result.NetPay}.");
    }

    [Fact]
    public async Task CalculateStatutoryDeductionsAsync_NetPay_Equals_Gross_Minus_Deductions()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = await engine.CalculateStatutoryDeductionsAsync(context);

        var expectedNet = context.Breakdown.MonthlyGross - result.TotalDeductions;
        Assert.Equal(expectedNet, result.NetPay);
    }

    [Fact]
    public async Task CalculateStatutoryDeductionsAsync_All_Deductions_Are_Non_Negative()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = await engine.CalculateStatutoryDeductionsAsync(context);

        Assert.All(result.Deductions, kvp =>
            Assert.True(kvp.Value >= 0,
                $"Deduction '{kvp.Key}' must be non-negative; got {kvp.Value}."));
    }

    // ── Payroll Calculation ───────────────────────────────────────────────────

    [Fact]
    public async Task CalculatePayrollAsync_NetPay_Equals_Gross_Minus_TotalDeductions()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = await engine.CalculatePayrollAsync(context);

        Assert.NotNull(result);
        Assert.Equal(result.GrossPay - result.TotalDeductions, result.NetPay);
    }

    [Fact]
    public async Task CalculatePayrollAsync_EmployeeId_Matches_Context()
    {
        var engine = CreateEngine();
        var context = BuildValidContext();

        var result = await engine.CalculatePayrollAsync(context);

        Assert.Equal(context.Profile.EmployeeId, result.EmployeeId);
    }
}
