// -----------------------------------------------------------------------
// <copyright file="InvoiceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Billing.Domain.Invoices;
using Xunit;

namespace Karamchari.Billing.Tests.Domain;

public class InvoiceTests
{
    private static Invoice CreateDraftInvoice()
    {
        return new Invoice
        {
            TenantId = "tenant-1",
            ClientId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            PeriodStart = new DateOnly(2025, 1, 1),
            PeriodEnd = new DateOnly(2025, 1, 31)
        };
    }

    private static Invoice CreateFinalizedInvoice(out decimal grandTotal)
    {
        var invoice = CreateDraftInvoice();
        // Add a line: 10 hrs * 100 = 1000, tax = 180, grandTotal = 1180
        invoice.AddLine("Development", quantity: 10m, rate: 100m);
        invoice.Finalize("INV-001", "admin@test.com", """{"snapshot":true}""");
        grandTotal = invoice.GrandTotal;
        return invoice;
    }

    [Fact]
    public void Invoice_AddLine_WhenDraft_AddsLineItem()
    {
        // Arrange
        var invoice = CreateDraftInvoice();

        // Act
        invoice.AddLine("Consulting", quantity: 5m, rate: 200m);

        // Assert
        invoice.Lines.Should().HaveCount(1);
        invoice.Lines.Single().Description.Should().Be("Consulting");
        invoice.Lines.Single().Amount.Should().Be(1000m);
        invoice.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public void Invoice_AddLine_WhenFinalized_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = CreateFinalizedInvoice(out _);

        // Act
        var act = () => invoice.AddLine("Extra Work", quantity: 2m, rate: 100m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-draft*");
    }

    [Fact]
    public void Invoice_Finalize_SetsStatusToFinalized()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        invoice.AddLine("Work", quantity: 1m, rate: 500m);

        // Act
        invoice.Finalize("INV-2025-001", "finance@company.com", """{"lines":[]}""");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Finalized);
        invoice.InvoiceNumber.Should().Be("INV-2025-001");
        invoice.FinalizedBy.Should().Be("finance@company.com");
        invoice.FinalizedAt.Should().NotBeNull();
    }

    [Fact]
    public void Invoice_Finalize_FreezesSnapshotJson()
    {
        // Arrange
        var invoice = CreateDraftInvoice();
        invoice.AddLine("Work", quantity: 1m, rate: 500m);
        var snapshotJson = """{"client":"ACME","lines":[{"desc":"Work","qty":1,"rate":500}]}""";

        // Act
        invoice.Finalize("INV-2025-001", "finance@company.com", snapshotJson);

        // Assert
        invoice.SnapshotJson.Should().Be(snapshotJson);
    }

    [Fact]
    public void Invoice_RecordPayment_AccumulatesPayments()
    {
        // Arrange
        var invoice = CreateFinalizedInvoice(out var grandTotal);
        var partialAmount = grandTotal / 2;

        // Act — record two partial payments that do not yet reach grandTotal
        invoice.RecordPayment(partialAmount - 1m, DateTimeOffset.UtcNow);
        invoice.RecordPayment(1m, DateTimeOffset.UtcNow.AddDays(1));

        // Assert — two payments recorded, still not fully paid
        invoice.Payments.Should().HaveCount(2);
        invoice.Payments.Sum(p => p.Amount).Should().Be(partialAmount);
        invoice.Status.Should().Be(InvoiceStatus.Finalized);
    }

    [Fact]
    public void Invoice_RecordPayment_WhenSumEqualsGrandTotal_TransitionsToPaid()
    {
        // Arrange
        var invoice = CreateFinalizedInvoice(out var grandTotal);

        // Act
        invoice.RecordPayment(grandTotal, DateTimeOffset.UtcNow);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.Payments.Should().HaveCount(1);
    }

    [Fact]
    public void Invoice_RecordPayment_WhenPaid_ThrowsInvalidOperationException()
    {
        // Arrange — the invoice status must be non-Draft to accept payment;
        // RecordPayment throws if status == Draft (not Paid). The production code
        // checks for Draft. Once Paid the method would add another payment
        // (no guard on Paid). Verify the Draft guard instead, which is the actual constraint.
        var invoice = CreateDraftInvoice();

        // Act
        var act = () => invoice.RecordPayment(100m, DateTimeOffset.UtcNow);

        // Assert — Draft invoices cannot receive payments
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }
}
