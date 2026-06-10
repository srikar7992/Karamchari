// -----------------------------------------------------------------------
// <copyright file="SalaryRevisionImportMapperTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.DataMigration.Features.SalaryRevision;
using Karamchari.DataMigration.Services;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Mappers;

public sealed class SalaryRevisionImportMapperTests
{
    private readonly SalaryRevisionImportMapper _sut = new();

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
            ("employee number", "EMP020"),
            ("previous ctc", "800000.00"),
            ("new ctc", "1000000.00"),
            ("effective date", "2024-04-01"),
            ("revision type", "Annual Appraisal"),
            ("remarks", "Promoted to Lead"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("EMP020");
        result.PreviousCTC.Should().Be(800000.00m);
        result.NewCTC.Should().Be(1000000.00m);
        result.EffectiveDate.Should().Be("2024-04-01");
        result.RevisionType.Should().Be("Annual Appraisal");
        result.Remarks.Should().Be("Promoted to Lead");
    }

    // ------------------------------------------------------------------ alternate column names

    [Fact]
    public void Map_AlternateEmpId_MapsEmployeeNumber()
    {
        var row = MakeRow(2, ("emp id", "EMP021"));
        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP021");
    }

    [Fact]
    public void Map_AlternateOldCtc_MapsPreviousCTC()
    {
        var row = MakeRow(3, ("old ctc", "600000"));
        var result = _sut.Map(row);
        result.PreviousCTC.Should().Be(600000m);
    }

    [Fact]
    public void Map_AlternateCtc_MapsNewCTC()
    {
        var row = MakeRow(4, ("ctc", "750000"));
        var result = _sut.Map(row);
        result.NewCTC.Should().Be(750000m);
    }

    [Fact]
    public void Map_AlternateDate_MapsEffectiveDate()
    {
        var row = MakeRow(5, ("date", "2024-07-01"));
        var result = _sut.Map(row);
        result.EffectiveDate.Should().Be("2024-07-01");
    }

    [Fact]
    public void Map_AlternateType_MapsRevisionType()
    {
        var row = MakeRow(6, ("type", "Promotion"));
        var result = _sut.Map(row);
        result.RevisionType.Should().Be("Promotion");
    }

    [Fact]
    public void Map_AlternateNotes_MapsRemarks()
    {
        var row = MakeRow(7, ("notes", "Off-cycle revision"));
        var result = _sut.Map(row);
        result.Remarks.Should().Be("Off-cycle revision");
    }

    // ------------------------------------------------------------------ primary columns take precedence

    [Fact]
    public void Map_PrimaryPreviousCtcTakesPrecedenceOverOldCtc()
    {
        var row = MakeRow(8,
            ("previous ctc", "900000"),
            ("old ctc", "800000"));

        var result = _sut.Map(row);
        result.PreviousCTC.Should().Be(900000m);
    }

    [Fact]
    public void Map_PrimaryNewCtcTakesPrecedenceOverCtc()
    {
        var row = MakeRow(9,
            ("new ctc", "1100000"),
            ("ctc", "1000000"));

        var result = _sut.Map(row);
        result.NewCTC.Should().Be(1100000m);
    }

    // ------------------------------------------------------------------ numeric parsing

    [Fact]
    public void Map_NonNumericPreviousCtc_ReturnsNull()
    {
        var row = MakeRow(10,
            ("employee number", "EMP022"),
            ("previous ctc", "NA"));

        var result = _sut.Map(row);
        result.PreviousCTC.Should().BeNull();
    }

    [Fact]
    public void Map_NonNumericNewCtc_ReturnsNull()
    {
        var row = MakeRow(11,
            ("employee number", "EMP023"),
            ("new ctc", "TBD"));

        var result = _sut.Map(row);
        result.NewCTC.Should().BeNull();
    }

    // ------------------------------------------------------------------ missing / empty handling

    [Fact]
    public void Map_MissingRemarksColumn_ReturnsNullRemarks()
    {
        var row = MakeRow(12,
            ("employee number", "EMP024"),
            ("new ctc", "500000"));

        var result = _sut.Map(row);
        result.Remarks.Should().BeNull();
    }

    [Fact]
    public void Map_AllEmptyRow_ReturnsRecordWithAllNulls_WithoutThrowing()
    {
        var row = new ImportRow(13, new Dictionary<string, string>());

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().BeNull();
        result.PreviousCTC.Should().BeNull();
        result.NewCTC.Should().BeNull();
        result.EffectiveDate.Should().BeNull();
        result.RevisionType.Should().BeNull();
        result.Remarks.Should().BeNull();
    }

    [Fact]
    public void Map_ExtraUnrecognisedColumns_AreIgnored()
    {
        var row = MakeRow(14,
            ("employee number", "EMP025"),
            ("new ctc", "600000"),
            ("unknown_field", "irrelevant"));

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP025");
    }
}
