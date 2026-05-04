using Karamchari.Core.Messaging;

namespace Karamchari.HR.Contracts.Employees;

/// <summary>
/// Integration event published when an employee is successfully onboarded.
/// Subscribed by other bounded contexts (e.g., Payroll) to react to new hires.
/// </summary>
public record EmployeeOnboardedIntegrationEvent(
    Guid EmployeeId,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn) : IIntegrationEvent;
