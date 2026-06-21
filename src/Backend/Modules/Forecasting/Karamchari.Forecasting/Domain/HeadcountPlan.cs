namespace Karamchari.Forecasting.Domain;
public sealed record HeadcountPlan(
    Guid DepartmentId, int FiscalYear,
    int PlannedHeadcount, int ApprovedHeadcount,
    decimal TotalBudget, decimal AvgSalary);
