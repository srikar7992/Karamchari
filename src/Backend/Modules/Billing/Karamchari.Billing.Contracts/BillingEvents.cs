// -----------------------------------------------------------------------
// <copyright file="BillingEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Billing.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record InvoiceIssuedIntegrationEvent(
    Guid EventId,
    Guid InvoiceId,
    Guid ProjectId,
    string TenantId,
    decimal TotalAmount,
    decimal TaxAmount,
    DateTimeOffset OccurredAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record PaymentReceivedIntegrationEvent(
    Guid EventId,
    Guid PaymentId,
    Guid InvoiceId,
    Guid ProjectId,
    string TenantId,
    decimal Amount,
    DateTimeOffset OccurredAt);
