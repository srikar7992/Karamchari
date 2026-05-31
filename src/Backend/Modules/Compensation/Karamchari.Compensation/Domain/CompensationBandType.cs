namespace Karamchari.Compensation.Domain;

/// <summary>Type of compensation band.</summary>
public enum CompensationBandType
{
    /// <summary>Fixed base salary band.</summary>
    BaseSalary = 1,

    /// <summary>Total cash compensation including target variable.</summary>
    TotalCashCompensation = 2,

    /// <summary>Total cost to company including benefits and allowances.</summary>
    TotalCTC = 3,
}
