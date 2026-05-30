using FluentAssertions;
using Karamchari.DataMigration.Features.SalaryRevision;
using Karamchari.DataMigration.Services;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Validators;

public sealed class SalaryRevisionImportValidatorTests
{
    private readonly IReferenceResolver _resolver = Substitute.For<IReferenceResolver>();
    private readonly SalaryRevisionImportValidator _sut;

    private static readonly Guid EmployeeId = Guid.NewGuid();

    public SalaryRevisionImportValidatorTests()
    {
        _sut = new SalaryRevisionImportValidator(_resolver);

        _resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
    }

    private void SetupEmployeeFound() =>
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns(EmployeeId);

    private static SalaryRevisionImportRecord ValidRecord() => new()
    {
        EmployeeNumber = "EMP001",
        PreviousCTC = 800000m,
        NewCTC = 1000000m,
        EffectiveDate = "2024-04-01",
        RevisionType = "AnnualIncrement",
        Remarks = "Annual review"
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
    public async Task ValidateAsync_NullEffectiveDate_ReturnsValid()
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { EffectiveDate = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NullPreviousCTC_ReturnsValid()
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { PreviousCTC = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NullRemarks_ReturnsValid()
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { Remarks = null };

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
        var record = ValidRecord() with { EmployeeNumber = "   " };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Employee Number is required.");
    }

    // ------------------------------------------------------------------ NewCTC validation

    [Fact]
    public async Task ValidateAsync_NullNewCTC_ReturnsError()
    {
        var record = ValidRecord() with { NewCTC = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("New CTC must be a positive value.");
    }

    [Fact]
    public async Task ValidateAsync_ZeroNewCTC_ReturnsError()
    {
        var record = ValidRecord() with { NewCTC = 0m };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("New CTC must be a positive value.");
    }

    [Fact]
    public async Task ValidateAsync_NegativeNewCTC_ReturnsError()
    {
        var record = ValidRecord() with { NewCTC = -5000m };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("New CTC must be a positive value.");
    }

    // ------------------------------------------------------------------ date validation

    [Fact]
    public async Task ValidateAsync_InvalidEffectiveDate_ReturnsError()
    {
        var record = ValidRecord() with { EffectiveDate = "not-a-date" };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid Effective Date format");
        result.ErrorMessage.Should().Contain("not-a-date");
    }

    [Fact]
    public async Task ValidateAsync_EffectiveDateDDMMYYYY_ReturnsError()
    {
        // Validator specifically checks DateOnly.TryParse — this ambiguous format may or may not parse.
        // If it fails, the error message should mention the date.
        var record = ValidRecord() with { EffectiveDate = "32/13/2024" };

        var result = await _sut.ValidateAsync(record);

        // 32/13/2024 is definitively invalid so TryParse will return false.
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid Effective Date format");
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

    [Theory]
    [InlineData(1.0)]
    [InlineData(100000.0)]
    [InlineData(9999999.99)]
    public async Task ValidateAsync_PositiveNewCTC_WithResolvedEmployee_ReturnsValid(double ctcDouble)
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { NewCTC = (decimal)ctcDouble };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }
}
