// -----------------------------------------------------------------------
// <copyright file="IBillingReadService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Karamchari.Billing.Contracts;

public sealed record OutstandingInvoiceDto(
    Guid Id,
    Guid ClientId,
    decimal GrandTotal,
    DateOnly PeriodEnd,
    IReadOnlyList<decimal> PaymentAmounts);

public sealed record InvoiceFinalizedDetailsDto(
    Guid ClientId,
    DateTimeOffset? FinalizedAt);

public interface IBillingReadService
{
    Task<decimal> GetUnbilledRevenueAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(string tenantId, CancellationToken ct = default);
    Task<Guid?> GetEmployeeRoleIdAsync(string tenantId, Guid employeeId, Guid projectId, DateOnly asOfDate, CancellationToken ct = default);
    Task<decimal?> ResolveRateAsync(string tenantId, Guid projectId, Guid roleId, DateOnly asOfDate, CancellationToken ct = default);
    Task<InvoiceFinalizedDetailsDto?> GetInvoiceFinalizedDetailsAsync(Guid invoiceId, CancellationToken ct = default);
}
