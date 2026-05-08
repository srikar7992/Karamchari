namespace Karamchari.Performance.Domain.KPIs;

public enum KPIType
{
    /// <summary>HR manually enters value each period.</summary>
    Manual = 1,
    /// <summary>Computed from other KPI values at period close.</summary>
    Calculated = 2,
    /// <summary>Populated by integration event consumers (e.g., attendance, defects).</summary>
    EventDriven = 3,
    /// <summary>Evaluated via IFormulaEngine at period close.</summary>
    Formula = 4
}
