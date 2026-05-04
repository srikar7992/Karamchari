namespace Karamchari.Payroll.Domain.SalaryStructures;

/// <summary>
/// Defines the broad classification of a salary component.
/// </summary>
public enum ComponentType
{
    /// <summary>Amounts paid to the employee (e.g., Basic, HRA).</summary>
    Earning = 0,

    /// <summary>Amounts deducted from the gross pay (e.g., PF Employee share).</summary>
    Deduction = 1,

    /// <summary>Amounts paid by the company on behalf of the employee (e.g., PF Employer share).</summary>
    EmployerContribution = 2
}

/// <summary>
/// Defines how a component's value is derived.
/// </summary>
public enum ComponentCalculationType
{
    /// <summary>A literal fixed amount.</summary>
    FixedAmount = 0,

    /// <summary>A percentage of the total Annual CTC.</summary>
    PercentageOfCTC = 1,

    /// <summary>A percentage of other computed components (e.g. HRA = 40% of Basic).</summary>
    PercentageOfComponents = 2,

    /// <summary>The balancing figure (CTC - Sum of other earnings/contributions).</summary>
    Remainder = 3
}

/// <summary>
/// Defines how multiple dependencies are combined.
/// </summary>
public enum DependencyAggregationType
{
    /// <summary>No dependencies.</summary>
    None = 0,

    /// <summary>Sum of all dependent values.</summary>
    Sum = 1,

    /// <summary>The maximum value among dependencies.</summary>
    Max = 2
}

/// <summary>
/// Defines the precision strategy for the component value.
/// </summary>
public enum RoundingStrategy
{
    /// <summary>Standard mathematical rounding to nearest integer.</summary>
    NearestRupee = 0,

    /// <summary>Always round down.</summary>
    Floor = 1,

    /// <summary>Always round up.</summary>
    Ceil = 2,

    /// <summary>No rounding applied.</summary>
    Exact = 3
}
