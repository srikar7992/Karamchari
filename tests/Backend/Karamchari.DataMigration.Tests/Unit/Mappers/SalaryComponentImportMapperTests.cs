using FluentAssertions;
using Karamchari.DataMigration.Features.SalaryComponent;
using Karamchari.DataMigration.Services;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Mappers;

public sealed class SalaryComponentImportMapperTests
{
    private readonly SalaryComponentImportMapper _sut = new();

    private static ImportRow MakeRow(int rowNum, params (string key, string value)[] columns)
    {
        var data = columns.ToDictionary(c => c.key, c => c.value);
        return new ImportRow(rowNum, data);
    }

    // ------------------------------------------------------------------ happy path

    [Fact]
    public void Map_AllPrimaryColumns_MapsAllFieldsCorrectly()
    {
        var row = MakeRow(1,
            ("employee number", "EMP010"),
            ("component", "Basic Pay"),
            ("amount", "50000.00"),
            ("frequency", "Monthly"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("EMP010");
        result.ComponentName.Should().Be("Basic Pay");
        result.Amount.Should().Be(50000.00m);
        result.Frequency.Should().Be("Monthly");
    }

    // ------------------------------------------------------------------ alternate column names

    [Fact]
    public void Map_AlternateEmpId_MapsEmployeeNumber()
    {
        var row = MakeRow(2, ("emp id", "EMP011"));
        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP011");
    }

    [Fact]
    public void Map_AlternateComponentName_MapsComponentName()
    {
        // The mapper tries "component" first, then "component name"
        var row = MakeRow(3, ("component name", "HRA"));
        var result = _sut.Map(row);
        result.ComponentName.Should().Be("HRA");
    }

    [Fact]
    public void Map_PrimaryComponentTakesPrecedenceOverComponentName()
    {
        var row = MakeRow(4,
            ("component", "Basic Pay"),
            ("component name", "HRA"));

        var result = _sut.Map(row);
        result.ComponentName.Should().Be("Basic Pay");
    }

    [Fact]
    public void Map_AlternatePayFrequency_MapsFrequency()
    {
        var row = MakeRow(5, ("pay frequency", "Quarterly"));
        var result = _sut.Map(row);
        result.Frequency.Should().Be("Quarterly");
    }

    [Fact]
    public void Map_PrimaryFrequencyTakesPrecedenceOverPayFrequency()
    {
        var row = MakeRow(6,
            ("frequency", "Annual"),
            ("pay frequency", "Monthly"));

        var result = _sut.Map(row);
        result.Frequency.Should().Be("Annual");
    }

    // ------------------------------------------------------------------ amount parsing

    [Fact]
    public void Map_AmountAsInteger_ParsesCorrectly()
    {
        var row = MakeRow(7, ("amount", "30000"));
        var result = _sut.Map(row);
        result.Amount.Should().Be(30000m);
    }

    [Fact]
    public void Map_AmountWithDecimals_ParsesCorrectly()
    {
        var row = MakeRow(8, ("amount", "1250.75"));
        var result = _sut.Map(row);
        result.Amount.Should().Be(1250.75m);
    }

    [Fact]
    public void Map_NonNumericAmount_ReturnsNull()
    {
        var row = MakeRow(9,
            ("employee number", "EMP012"),
            ("amount", "twenty thousand"));

        var result = _sut.Map(row);
        result.Amount.Should().BeNull();
    }

    [Fact]
    public void Map_EmptyAmount_ReturnsNull()
    {
        var row = MakeRow(10,
            ("employee number", "EMP013"),
            ("amount", ""));

        var result = _sut.Map(row);
        result.Amount.Should().BeNull();
    }

    // ------------------------------------------------------------------ missing fields

    [Fact]
    public void Map_MissingFrequency_ReturnsNullFrequency()
    {
        var row = MakeRow(11,
            ("employee number", "EMP014"),
            ("component", "LTA"),
            ("amount", "5000"));

        var result = _sut.Map(row);
        result.Frequency.Should().BeNull();
    }

    [Fact]
    public void Map_AllEmptyRow_ReturnsRecordWithAllNulls_WithoutThrowing()
    {
        var row = new ImportRow(12, new Dictionary<string, string>());

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().BeNull();
        result.ComponentName.Should().BeNull();
        result.Amount.Should().BeNull();
        result.Frequency.Should().BeNull();
    }

    [Fact]
    public void Map_ExtraUnrecognisedColumns_AreIgnored()
    {
        var row = MakeRow(13,
            ("employee number", "EMP015"),
            ("component", "Bonus"),
            ("amount", "10000"),
            ("irrelevant_column", "xyz"));

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP015");
    }
}
