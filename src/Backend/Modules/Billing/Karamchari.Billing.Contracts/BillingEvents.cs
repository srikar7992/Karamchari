// -----------------------------------------------------------------------
// <copyright file="BillingEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts;
namespace Karamchari.Billing.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record InvoiceIssuedIntegrationEventV1(
    Guid EventId,
    Guid InvoiceId,
    Guid ProjectId,
    string TenantId,
    decimal TotalAmount,
    decimal TaxAmount,
    DateTimeOffset OccurredAt) : IIntegrationEvent;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record PaymentReceivedIntegrationEventV1(
    Guid EventId,
    Guid PaymentId,
    Guid InvoiceId,
    Guid ProjectId,
    string TenantId,
    decimal Amount,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
