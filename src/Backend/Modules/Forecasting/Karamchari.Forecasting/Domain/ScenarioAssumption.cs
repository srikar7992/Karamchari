namespace Karamchari.Forecasting.Domain;

public sealed record ScenarioAssumption(AssumptionType Type, decimal Value, string? Notes);
