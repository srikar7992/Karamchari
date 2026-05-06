namespace Karamchari.Billing.Contracts;

public sealed record InvoiceIssuedIntegrationEvent(
    Guid EventId,
    Guid InvoiceId,
    Guid ProjectId,
    string TenantId,
    decimal TotalAmount,
    decimal TaxAmount,
    DateTimeOffset OccurredAt);

public sealed record PaymentReceivedIntegrationEvent(
    Guid EventId,
    Guid PaymentId,
    Guid InvoiceId,
    Guid ProjectId,
    string TenantId,
    decimal Amount,
    DateTimeOffset OccurredAt);
