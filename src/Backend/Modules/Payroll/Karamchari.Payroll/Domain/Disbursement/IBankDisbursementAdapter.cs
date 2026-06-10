// -----------------------------------------------------------------------
// <copyright file="IBankDisbursementAdapter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Disbursement;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record BankDisbursementRequest(
    string BatchReference,
    IReadOnlyList<DisbursementEntry> Entries,
    string DebitAccountNumber,
    DateOnly ValueDate);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record BankDisbursementResult(
    string BatchReference,
    bool IsSuccess,
    IReadOnlyList<EntryResult> EntryResults,
    string? BankAcknowledgementId,
    string? ErrorMessage);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record EntryResult(
    Guid EntryId,
    bool IsSuccess,
    string? BankTransactionId,
    string? FailureReason);

/// <summary>
/// Bank adapter abstraction. Each bank provides a concrete implementation.
/// Adapter handles file format, encryption, API calls, and acknowledgement parsing.
/// </summary>
public interface IBankDisbursementAdapter
{
    BankProvider Provider { get; }
    Task<BankDisbursementResult> DisburseAsync(BankDisbursementRequest request, CancellationToken ct);
    Task<BankDisbursementResult> QueryStatusAsync(string batchReference, CancellationToken ct);
}
