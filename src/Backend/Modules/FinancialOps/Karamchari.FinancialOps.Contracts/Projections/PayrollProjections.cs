// -----------------------------------------------------------------------
// <copyright file="PayrollProjections.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;

namespace Karamchari.FinancialOps.Contracts.Projections;

/// <summary>
/// Projection of approved reimbursements ready for payroll processing.
/// Payroll consumes this to include reimbursements in the payslip.
/// </summary>
public record ReimbursementPayrollProjection(
    string TenantId,
    Guid EmployeeId,
    Guid ClaimId,
    string ClaimNumber,
    decimal Amount,
    string Currency,
    TaxTreatment TaxTreatment,
    DateTimeOffset ApprovedAt);

/// <summary>
/// Projection of active loan deductions for the current payroll cycle.
/// </summary>
public record LoanDeductionProjection(
    string TenantId,
    Guid EmployeeId,
    Guid LoanId,
    decimal InstallmentAmount,
    decimal RemainingPrincipal,
    int RemainingTenureMonths);
