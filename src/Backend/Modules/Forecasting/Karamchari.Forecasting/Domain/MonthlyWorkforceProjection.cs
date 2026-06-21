namespace Karamchari.Forecasting.Domain;
public sealed record MonthlyWorkforceProjection(
    int Month,
    int ProjectedHeadcount,
    decimal ProjectedPayroll,
    int ProjectedHires,
    int ProjectedAttrition);
