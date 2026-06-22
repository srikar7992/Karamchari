// -----------------------------------------------------------------------
// <copyright file="IndiaCountryConstants.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.CountryEngine;

using Karamchari.Payroll.Domain.CountryEngine;

/// <summary>
/// All static metadata for India's payroll environment.
/// Centralised here so IndiaPayrollEngine stays clean and these values
/// can be updated in one place when regulations change.
/// </summary>
internal static class IndiaCountryConstants
{
    internal static readonly CountryMetadata Metadata = new(
        CountryCode: "IN",
        CountryName: "India",
        DefaultCurrency: "INR",
        SupportedPayrollFrequencies: ["Monthly"],
        SupportedTaxRegimes:
        [
            new TaxRegimeOption("New", "New Tax Regime",
                "Lower slab rates with Standard Deduction of ₹75,000. No exemptions for HRA, LTA, or 80C deductions."),
            new TaxRegimeOption("Old", "Old Tax Regime",
                "Higher slab rates with exemptions for HRA, LTA, 80C (₹1.5L cap), 80D, and Standard Deduction of ₹50,000.")
        ],
        RequiredEmployeeIdentifiers:
        [
            new RequiredEmployeeIdentifier(
                Key: "PAN",
                DisplayLabel: "Permanent Account Number",
                HelpText: "10-character alphanumeric ID issued by Income Tax Department. Mandatory for TDS deduction.",
                IsMandatory: true,
                ValidationPattern: @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"),
            new RequiredEmployeeIdentifier(
                Key: "UAN",
                DisplayLabel: "Universal Account Number",
                HelpText: "12-digit number issued by EPFO. Required for EPF contributions to be credited to the employee.",
                IsMandatory: false,
                ValidationPattern: @"^\d{12}$"),
            new RequiredEmployeeIdentifier(
                Key: "ESIC_NUMBER",
                DisplayLabel: "ESIC Insurance Number",
                HelpText: "Issued by ESIC. Required only for employees with Gross ≤ ₹21,000/month.",
                IsMandatory: false,
                ValidationPattern: null)
        ],
        SupportedStatutoryPrograms:
        [
            new StatutoryProgram("EPF", "Employee Provident Fund",
                IsEmployeeContribution: true, IsEmployerContribution: true,
                "12% of EPF wages (capped at ₹15,000 unless Voluntary PF opted)."),
            new StatutoryProgram("ESIC", "Employee State Insurance",
                IsEmployeeContribution: true, IsEmployerContribution: true,
                "0.75% employee + 3.25% employer for employees with Gross ≤ ₹21,000."),
            new StatutoryProgram("PT", "Professional Tax",
                IsEmployeeContribution: true, IsEmployerContribution: false,
                "State-specific slab-based tax. Varies by state code and gross salary."),
            new StatutoryProgram("TDS", "Tax Deducted at Source",
                IsEmployeeContribution: true, IsEmployerContribution: false,
                "Monthly TDS on projected annual income under Old or New tax regime.")
        ]
    );

    internal static readonly StatutoryReportingCalendar ReportingCalendar = new(
        CountryCode: "IN",
        Obligations:
        [
            new ComplianceObligation(
                Code: "TDS_MONTHLY",
                DisplayName: "Monthly TDS Deposit (Form 24Q)",
                Frequency: FilingFrequency.Monthly,
                DueDateRule: new FixedDayNextMonth(7),
                Description: "Monthly TDS deducted from employee salaries must be deposited with the government.",
                LateFilingPenalty: "Interest @1.5% per month under Section 201(1A)"),
            new ComplianceObligation(
                Code: "EPF_ECR",
                DisplayName: "EPF Electronic Challan cum Return",
                Frequency: FilingFrequency.Monthly,
                DueDateRule: new FixedDayNextMonth(15),
                Description: "Monthly EPF contributions to be filed and deposited with EPFO.",
                LateFilingPenalty: "Damages @5% to 25% p.a. under EPF Act Section 14B"),
            new ComplianceObligation(
                Code: "ESIC_RETURN",
                DisplayName: "ESIC Monthly Contribution Return",
                Frequency: FilingFrequency.Monthly,
                DueDateRule: new FixedDayNextMonth(15),
                Description: "Monthly ESIC contributions for eligible employees.",
                LateFilingPenalty: "Simple interest @12% p.a. under ESI Act Section 85B"),
            new ComplianceObligation(
                Code: "24Q_QUARTERLY",
                DisplayName: "TDS Quarterly Return (Form 24Q)",
                Frequency: FilingFrequency.Quarterly,
                DueDateRule: new FixedDayAfterMonths(31, 1),
                Description: "Quarterly statement of TDS deducted from salaries (Q1: Jul 31, Q2: Oct 31, Q3: Jan 31, Q4: May 31).",
                LateFilingPenalty: "₹200 per day under Section 234E, capped at TDS amount"),
            new ComplianceObligation(
                Code: "FORM_16",
                DisplayName: "Form 16 — Annual TDS Certificate",
                Frequency: FilingFrequency.Annual,
                DueDateRule: new FixedDayAfterMonths(15, 2),
                Description: "Annual TDS certificate issued to each employee after financial year close (due June 15).",
                LateFilingPenalty: "₹100 per day under Section 272A")
        ]
    );

    internal static readonly PayrollCalendar PayrollCalendar = new(
        CountryCode: "IN",
        SupportedFrequencies: ["Monthly"],
        DefaultFrequency: "Monthly",
        DefaultProcessingDayOfMonth: 28,
        FinancialYearStart: new DateOnly(2000, 4, 1), // April 1; year component is ignored, only M/D used
        WeekendDays: ["Saturday", "Sunday"],
        MaxOpenPeriods: 1
    );
}
