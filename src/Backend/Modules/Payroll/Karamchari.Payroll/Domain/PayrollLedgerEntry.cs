// -----------------------------------------------------------------------
// <copyright file="PayrollLedgerEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a finalized payroll record for an employee for a specific month.
/// Used for YTD tax projections and historical reporting.
/// </summary>
public class PayrollLedgerEntry : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the employee identifier.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the month being recorded (1-12).</summary>
    public int Month { get; private set; }

    /// <summary>Gets the calendar year.</summary>
    public int Year { get; private set; }

    /// <summary>Gets the financial year start year.</summary>
    public int FinancialYearStart { get; private set; }

    /// <summary>Gets the total gross earnings for the month.</summary>
    public decimal MonthlyGross { get; private set; }

    /// <summary>Gets the total TDS deducted for the month.</summary>
    public decimal TdsDeducted { get; private set; }

    /// <summary>Gets the associated payroll run identifier.</summary>
    public Guid RunId { get; private set; }

    /// <summary>Gets the name of the period (e.g., "April 2026").</summary>
    public string PeriodName { get; private set; } = string.Empty;

    /// <summary>Gets all deductions for the month.</summary>
    public IReadOnlyDictionary<string, decimal> Deductions { get; private set; } = new Dictionary<string, decimal>();

    /// <summary>Gets all earnings for the month.</summary>
    public IReadOnlyDictionary<string, decimal> Earnings { get; private set; } = new Dictionary<string, decimal>();

    /// <summary>Gets the net pay after all deductions.</summary>
    public decimal NetPay { get; private set; }

    private PayrollLedgerEntry() { }

    /// <summary>
    /// Creates a new ledger entry.
    /// </summary>
    public static PayrollLedgerEntry Create(
        Guid runId,
        string periodName,
        Guid employeeId,
        int month,
        int year,
        int fyStart,
        decimal gross,
        decimal tds,
        decimal net,
        IReadOnlyDictionary<string, decimal> deductions,
        IReadOnlyDictionary<string, decimal> earnings)
    {
        return new PayrollLedgerEntry
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            PeriodName = periodName,
            EmployeeId = employeeId,
            Month = month,
            Year = year,
            FinancialYearStart = fyStart,
            MonthlyGross = gross,
            TdsDeducted = tds,
            NetPay = net,
            Deductions = deductions,
            Earnings = earnings
        };
    }
}
