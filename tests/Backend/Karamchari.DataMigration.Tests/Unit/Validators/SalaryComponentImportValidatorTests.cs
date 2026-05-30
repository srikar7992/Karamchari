using FluentAssertions;
using Karamchari.DataMigration.Features.SalaryComponent;
using Karamchari.DataMigration.Services;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Validators;

public sealed class SalaryComponentImportValidatorTests
{
    private readonly IReferenceResolver _resolver = Substitute.For<IReferenceResolver>();
    private readonly SalaryComponentImportValidator _sut;

    private static readonly Guid EmployeeId = Guid.NewGuid();

    public SalaryComponentImportValidatorTests()
    {
        _sut = new SalaryComponentImportValidator(_resolver);

        _resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
    }

    private void SetupEmployeeFound() =>
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns(EmployeeId);

    private static SalaryComponentImportRecord ValidRecord() => new()
    {
        EmployeeNumber = "EMP001",
        ComponentName = "Basic Salary",
        Amount = 50000m,
        Frequency = "Monthly"
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
    public async Task ValidateAsync_NullFrequency_StillValid()
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { Frequency = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_EmployeeFound_ReturnsValid()
    {
        SetupEmployeeFound();

        var result = await _sut.ValidateAsync(ValidRecord());

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

    [Fact]
    public async Task ValidateAsync_MissingComponentName_ReturnsError()
    {
        var record = ValidRecord() with { ComponentName = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Component name is required.");
    }

    [Fact]
    public async Task ValidateAsync_WhitespaceComponentName_ReturnsError()
    {
        var record = ValidRecord() with { ComponentName = "  " };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Component name is required.");
    }

    // ------------------------------------------------------------------ amount validation

    [Fact]
    public async Task ValidateAsync_NullAmount_ReturnsError()
    {
        var record = ValidRecord() with { Amount = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be a positive value.");
    }

    [Fact]
    public async Task ValidateAsync_ZeroAmount_ReturnsError()
    {
        var record = ValidRecord() with { Amount = 0m };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be a positive value.");
    }

    [Fact]
    public async Task ValidateAsync_NegativeAmount_ReturnsError()
    {
        var record = ValidRecord() with { Amount = -100m };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be a positive value.");
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
    public async Task ValidateAsync_MissingRequiredFields_NeverCallsResolver()
    {
        var record = ValidRecord() with { EmployeeNumber = null };

        await _sut.ValidateAsync(record);

        await _resolver.DidNotReceive().ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.0)]
    [InlineData(1000000.0)]
    public async Task ValidateAsync_PositiveAmount_ReturnsValid(double amountDouble)
    {
        SetupEmployeeFound();
        var record = ValidRecord() with { Amount = (decimal)amountDouble };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }
}
