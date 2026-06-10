// -----------------------------------------------------------------------
// <copyright file="EmployeeImportMapperTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.DataMigration.Services;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Mappers;

public sealed class EmployeeImportMapperTests
{
    private readonly EmployeeImportMapper _sut = new();

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
            ("employee number", "EMP001"),
            ("first name", "Arjun"),
            ("last name", "Sharma"),
            ("email", "arjun@example.com"),
            ("department", "Engineering"),
            ("designation", "Senior Engineer"),
            ("manager employee number", "EMP000"),
            ("date of joining", "2022-01-15"),
            ("employment type", "Full-Time"),
            ("branch", "Hyderabad"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("EMP001");
        result.FirstName.Should().Be("Arjun");
        result.LastName.Should().Be("Sharma");
        result.Email.Should().Be("arjun@example.com");
        result.DepartmentName.Should().Be("Engineering");
        result.DesignationName.Should().Be("Senior Engineer");
        result.ManagerEmployeeNumber.Should().Be("EMP000");
        result.DateOfJoining.Should().Be("2022-01-15");
        result.EmploymentType.Should().Be("Full-Time");
        result.BranchName.Should().Be("Hyderabad");
    }

    // ------------------------------------------------------------------ alternate column names

    [Fact]
    public void Map_AlternateEmpId_MapsEmployeeNumber()
    {
        var row = MakeRow(2, ("emp id", "EMP042"));
        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP042");
    }

    [Fact]
    public void Map_AlternateFName_MapsFirstName()
    {
        var row = MakeRow(3, ("fname", "Priya"));
        var result = _sut.Map(row);
        result.FirstName.Should().Be("Priya");
    }

    [Fact]
    public void Map_AlternateLName_MapsLastName()
    {
        var row = MakeRow(4, ("lname", "Reddy"));
        var result = _sut.Map(row);
        result.LastName.Should().Be("Reddy");
    }

    [Fact]
    public void Map_AlternateDept_MapsDepartmentName()
    {
        var row = MakeRow(5, ("dept", "Finance"));
        var result = _sut.Map(row);
        result.DepartmentName.Should().Be("Finance");
    }

    [Fact]
    public void Map_AlternateTitle_MapsDesignationName()
    {
        var row = MakeRow(6, ("title", "VP Engineering"));
        var result = _sut.Map(row);
        result.DesignationName.Should().Be("VP Engineering");
    }

    [Fact]
    public void Map_AlternateMgrId_MapsManagerEmployeeNumber()
    {
        var row = MakeRow(7, ("mgr id", "EMP099"));
        var result = _sut.Map(row);
        result.ManagerEmployeeNumber.Should().Be("EMP099");
    }

    [Fact]
    public void Map_AlternateDoj_MapsDateOfJoining()
    {
        var row = MakeRow(8, ("doj", "2020-06-01"));
        var result = _sut.Map(row);
        result.DateOfJoining.Should().Be("2020-06-01");
    }

    [Fact]
    public void Map_AlternateType_MapsEmploymentType()
    {
        var row = MakeRow(9, ("type", "Contract"));
        var result = _sut.Map(row);
        result.EmploymentType.Should().Be("Contract");
    }

    // ------------------------------------------------------------------ primary column wins over alternate

    [Fact]
    public void Map_PrimaryColumnTakesPrecedenceOverAlternate_ForEmployeeNumber()
    {
        var row = MakeRow(10,
            ("employee number", "PRIMARY"),
            ("emp id", "ALTERNATE"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("PRIMARY");
    }

    // ------------------------------------------------------------------ missing / null handling

    [Fact]
    public void Map_MissingOptionalColumns_ReturnsNullForMissingFields()
    {
        var row = MakeRow(11, ("employee number", "EMP003"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("EMP003");
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
        result.Email.Should().BeNull();
        result.DepartmentName.Should().BeNull();
        result.DesignationName.Should().BeNull();
        result.ManagerEmployeeNumber.Should().BeNull();
        result.DateOfJoining.Should().BeNull();
        result.EmploymentType.Should().BeNull();
        result.BranchName.Should().BeNull();
    }

    [Fact]
    public void Map_AllEmptyRow_ReturnsRecordWithAllNulls_WithoutThrowing()
    {
        var row = new ImportRow(12, new Dictionary<string, string>());

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().BeNull();
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
    }

    [Fact]
    public void Map_ExtraUnrecognisedColumns_AreIgnored()
    {
        var row = MakeRow(13,
            ("employee number", "EMP005"),
            ("unknown_column", "some_value"),
            ("another_extra", "data"));

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP005");
    }

    [Fact]
    public void Map_ValuePreservedAsIs_NoUnexpectedTrimming()
    {
        // The mapper does not trim — values come through as-is from the parser.
        // Verify the mapper faithfully passes whatever the parser provides.
        var row = MakeRow(14,
            ("first name", "  Raj  "),
            ("last name", "Kumar"));

        var result = _sut.Map(row);

        result.FirstName.Should().Be("  Raj  ");
    }

    [Fact]
    public void Map_RowNumber_DoesNotAffectMappedValues()
    {
        var row1 = MakeRow(1, ("employee number", "EMP001"));
        var row2 = MakeRow(999, ("employee number", "EMP001"));

        _sut.Map(row1).EmployeeNumber.Should().Be(_sut.Map(row2).EmployeeNumber);
    }
}
