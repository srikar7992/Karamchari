namespace Karamchari.Benefits.Contracts;

public record BenefitDeductionCalculatedEvent(
    Guid RecordId, string TenantId, Guid EmployeeId,
    string PayPeriod, decimal EmployeeDeduction, decimal EmployerContribution);
