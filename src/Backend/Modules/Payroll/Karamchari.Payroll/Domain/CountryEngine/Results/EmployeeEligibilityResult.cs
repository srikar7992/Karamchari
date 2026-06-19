// -----------------------------------------------------------------------
// <copyright file="EmployeeEligibilityResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// The result of validating whether an employee's payroll profile satisfies the
/// statutory requirements of the country engine before a pay run begins.
/// </summary>
public sealed record EmployeeEligibilityResult(
    bool IsEligible,

    /// <summary>Blocking issues that must be resolved before payroll can proceed.</summary>
    IReadOnlyList<string> Errors,

    /// <summary>Non-blocking issues that should be flagged but do not prevent processing.</summary>
    IReadOnlyList<string> Warnings
)
{
    public static EmployeeEligibilityResult Eligible() =>
        new(true, [], []);

    public static EmployeeEligibilityResult Ineligible(params string[] errors) =>
        new(false, errors, []);
}
