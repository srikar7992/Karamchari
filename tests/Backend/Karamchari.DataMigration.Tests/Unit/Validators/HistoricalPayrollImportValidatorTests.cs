// -----------------------------------------------------------------------
// <copyright file="HistoricalPayrollImportValidatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.DataMigration.Features.HistoricalPayroll;
using Karamchari.DataMigration.Services;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Validators;

public sealed class HistoricalPayrollImportValidatorTests
{
    private readonly IReferenceResolver _resolver = Substitute.For<IReferenceResolver>();
    private readonly HistoricalPayrollImportValidator _sut;

    private static readonly Guid EmployeeId = Guid.NewGuid();

    public HistoricalPayrollImportValidatorTests()
    {
        _sut = new HistoricalPayrollImportValidator(_resolver);

        _resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
    }

    private void SetupEmployeeFound() =>
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns(EmployeeId);

    private static HistoricalPayrollImportRecord ValidRecord() => new()
    {
        EmployeeNumber = "EMP001",
        Month = 3,
        Year = 2024,
        GrossPay = 85000m,
        Deductions = 5000m,
        NetPay = 80000m
    };

    // ------------------------------------------------------------------ happy path

    [Fact]
    public async Task ValidateAsync_ValidRecord_ReturnsValid()
    {
        SetupEmployeeFound();

        var result = await _sut.ValidateAsync(ValidRecord());

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_NullGrossAndDeductions_StillValid()
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { GrossPay = null, Deductions = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public async Task ValidateAsync_ValidMonths_ReturnsValid(int month)
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { Month = month };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2024)]
    [InlineData(2100)]
    public async Task ValidateAsync_ValidYears_ReturnsValid(int year)
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { Year = year };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    // ------------------------------------------------------------------ required field errors

    [Fact]
    public async Task ValidateAsync_MissingEmployeeNumber_ReturnsError()
    {
        var record = ValidRecord() with { EmployeeNumber = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Employee Number is required.");
    }

    [Fact]
    public async Task ValidateAsync_WhitespaceEmployeeNumber_ReturnsError()
    {
        var record = ValidRecord() with { EmployeeNumber = "  " };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Employee Number is required.");
    }

    // ------------------------------------------------------------------ month validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ValidateAsync_MonthLessThan1_ReturnsError(int month)
    {
        var record = ValidRecord() with { Month = month };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Month must be between 1 and 12.");
    }

    [Theory]
    [InlineData(13)]
    [InlineData(99)]
    [InlineData(100)]
    public async Task ValidateAsync_MonthGreaterThan12_ReturnsError(int month)
    {
        var record = ValidRecord() with { Month = month };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Month must be between 1 and 12.");
    }

    [Fact]
    public async Task ValidateAsync_NullMonth_ReturnsError()
    {
        var record = ValidRecord() with { Month = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Month must be between 1 and 12.");
    }

    // ------------------------------------------------------------------ year validation

    [Fact]
    public async Task ValidateAsync_NullYear_ReturnsError()
    {
        var record = ValidRecord() with { Year = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Year must be a valid calendar year.");
    }

    [Fact]
    public async Task ValidateAsync_YearBefore2000_ReturnsError()
    {
        var record = ValidRecord() with { Year = 1999 };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Year must be a valid calendar year.");
    }

    [Fact]
    public async Task ValidateAsync_YearAfter2100_ReturnsError()
    {
        var record = ValidRecord() with { Year = 2101 };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Year must be a valid calendar year.");
    }

    // ------------------------------------------------------------------ NetPay validation

    [Fact]
    public async Task ValidateAsync_NullNetPay_ReturnsError()
    {
        var record = ValidRecord() with { NetPay = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Net Pay is required.");
    }

    // ------------------------------------------------------------------ reference resolution

    [Fact]
    public async Task ValidateAsync_EmployeeNotFound_ReturnsError()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.ValidateAsync(ValidRecord());

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EMP001");
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAsync_MissingEmployeeNumber_NeverCallsResolver()
    {
        var record = ValidRecord() with { EmployeeNumber = null };

        await _sut.ValidateAsync(record);

        await _resolver.DidNotReceive().ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
