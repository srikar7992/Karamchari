namespace Karamchari.Forecasting.Domain;

public enum ScenarioType { Base = 1, Optimistic = 2, Conservative = 3 }
public enum ScenarioStatus { Draft = 1, Active = 2, Archived = 3 }
public enum AssumptionType { AttritionRate = 1, HiringVelocity = 2, SalaryInflation = 3, ContractWinRate = 4 }
