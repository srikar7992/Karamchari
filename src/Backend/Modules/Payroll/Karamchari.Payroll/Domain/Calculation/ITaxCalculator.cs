namespace Karamchari.Payroll.Domain.Calculation;

/// <summary>
/// Inputs provided to the tax calculator for a single employee's payroll period.
/// </summary>
public sealed record PayrollTaxContext(
    Guid EmployeeId,
    string TenantId,
    int FinancialYear,
    decimal YearToDateGross,
    decimal CurrentPeriodGross,
    decimal PfDeduction,
    decimal ProfessionalTax,
    string TaxRegime,
    decimal DeclaredInvestments);

/// <summary>
/// Tax calculation result for a single employee.
/// </summary>
public sealed record TaxResult(
    Guid EmployeeId,
    decimal TaxableIncome,
    decimal TaxLiability,
    decimal TdsThisPeriod,
    string TaxRuleVersionId);

/// <summary>
/// Versioned tax calculator. Implementations load the rule version active for the given financial year.
/// </summary>
public interface ITaxCalculator
{
    TaxResult Calculate(PayrollTaxContext context);
}
