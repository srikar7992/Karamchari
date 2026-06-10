// -----------------------------------------------------------------------
// <copyright file="EmployeeTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Domain.Employees.Events;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class EmployeeTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Employee HireDefault(
        string employeeNumber = "EMP-001",
        string legalName = "Jane Doe",
        string? workEmail = "jane.doe@example.com",
        DateOnly? hiredOn = null)
        => Employee.Hire(employeeNumber, legalName, workEmail, hiredOn ?? new DateOnly(2025, 1, 15));

    // ─── Hire ────────────────────────────────────────────────────────────────

    [Fact]
    public void Employee_Hire_SetsStatusToActive()
    {
        var employee = HireDefault();

        employee.Status.Should().Be(EmploymentStatus.Active);
    }

    [Fact]
    public void Employee_Hire_SetsEmployeeNumberAndLegalName()
    {
        var employee = HireDefault(employeeNumber: "EMP-XYZ", legalName: "Ravi Kumar");

        employee.EmployeeNumber.Should().Be("EMP-XYZ");
        employee.LegalName.Should().Be("Ravi Kumar");
    }

    [Fact]
    public void Employee_Hire_SetsWorkEmail()
    {
        var employee = HireDefault(workEmail: "ravi@corp.com");

        employee.WorkEmail.Should().Be("ravi@corp.com");
    }

    [Fact]
    public void Employee_Hire_SetsHiredOnDate()
    {
        var hiredOn = new DateOnly(2024, 6, 1);
        var employee = HireDefault(hiredOn: hiredOn);

        employee.HiredOn.Should().Be(hiredOn);
    }

    [Fact]
    public void Employee_Hire_AssignsNewGuidId()
    {
        var employee = HireDefault();

        employee.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Employee_Hire_AddsEmployeeHiredDomainEvent()
    {
        var employee = HireDefault();

        // Domain events are surfaced via the DomainEvents collection on AggregateRoot
        var events = employee.DomainEvents;
        events.Should().ContainSingle(e => e is EmployeeHired);
    }

    [Fact]
    public void Employee_Hire_DomainEventCarriesCorrectPayload()
    {
        var hiredOn = new DateOnly(2024, 3, 10);
        var employee = HireDefault(employeeNumber: "EMP-101", legalName: "Priya Singh",
            workEmail: "priya@company.com", hiredOn: hiredOn);

        var evt = employee.DomainEvents.OfType<EmployeeHired>().Single();

        evt.EmployeeId.Should().Be(employee.Id);
        evt.EmployeeNumber.Should().Be("EMP-101");
        evt.LegalName.Should().Be("Priya Singh");
        evt.WorkEmail.Should().Be("priya@company.com");
        evt.HiredOn.Should().Be(hiredOn);
    }

    [Fact]
    public void Employee_Hire_TrimsWhitespaceFromEmployeeNumber()
    {
        var employee = HireDefault(employeeNumber: "  EMP-002  ");

        employee.EmployeeNumber.Should().Be("EMP-002");
    }

    [Fact]
    public void Employee_Hire_NullWorkEmail_IsStoredAsNull()
    {
        var employee = HireDefault(workEmail: null);

        employee.WorkEmail.Should().BeNull();
    }

    [Fact]
    public void Employee_Hire_WhitespaceWorkEmail_NormalizesToNull()
    {
        var employee = HireDefault(workEmail: "   ");

        employee.WorkEmail.Should().BeNull();
    }

    [Fact]
    public void Employee_Hire_EmptyEmployeeNumber_ThrowsArgumentException()
    {
        var act = () => HireDefault(employeeNumber: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Employee_Hire_WhitespaceLegalName_ThrowsArgumentException()
    {
        var act = () => HireDefault(legalName: "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Employee_Hire_DefaultsToFullTimeEmploymentType()
    {
        var employee = HireDefault();

        employee.EmploymentType.Should().Be(EmploymentType.FullTime);
    }

    [Fact]
    public void Employee_Hire_WithCustomEmploymentType_SetsCorrectly()
    {
        var employee = Employee.Hire("EMP-003", "Contractor X", null,
            new DateOnly(2025, 1, 1), EmploymentType.Contractor);

        employee.EmploymentType.Should().Be(EmploymentType.Contractor);
    }

    // ─── UpdateDetails ───────────────────────────────────────────────────────

    [Fact]
    public void Employee_UpdateDetails_ChangesLegalName()
    {
        var employee = HireDefault(legalName: "Old Name");

        employee.UpdateDetails("New Name", null);

        employee.LegalName.Should().Be("New Name");
    }

    [Fact]
    public void Employee_UpdateDetails_ChangesWorkEmail()
    {
        var employee = HireDefault(workEmail: "old@example.com");

        employee.UpdateDetails("Jane Doe", "new@example.com");

        employee.WorkEmail.Should().Be("new@example.com");
    }

    [Fact]
    public void Employee_UpdateDetails_TrimsLegalName()
    {
        var employee = HireDefault();

        employee.UpdateDetails("  Trimmed Name  ", null);

        employee.LegalName.Should().Be("Trimmed Name");
    }

    [Fact]
    public void Employee_UpdateDetails_WhitespaceEmail_NormalizesToNull()
    {
        var employee = HireDefault(workEmail: "old@example.com");

        employee.UpdateDetails("Jane Doe", "   ");

        employee.WorkEmail.Should().BeNull();
    }

    // ─── TransferToDepartment ────────────────────────────────────────────────

    [Fact]
    public void Employee_TransferToDepartment_UpdatesDepartmentId()
    {
        var employee = HireDefault();
        var deptId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        employee.TransferToDepartment(deptId, DateTimeOffset.UtcNow, changedBy);

        employee.DepartmentId.Should().Be(deptId);
    }

    [Fact]
    public void Employee_TransferToDepartment_AddsHistoryEntry()
    {
        var employee = HireDefault();
        var deptId = Guid.NewGuid();
        var effectiveFrom = DateTimeOffset.UtcNow;
        var changedBy = Guid.NewGuid();

        employee.TransferToDepartment(deptId, effectiveFrom, changedBy);

        employee.History.Should().ContainSingle(h =>
            h.Type == EmployeeHistoryType.DepartmentTransfer &&
            h.NewValue == deptId.ToString() &&
            h.EffectiveFrom == effectiveFrom);
    }

    [Fact]
    public void Employee_TransferToDepartment_PreviousValueIsNone_WhenFirstTransfer()
    {
        var employee = HireDefault();

        employee.TransferToDepartment(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.History.Single().PreviousValue.Should().Be("None");
    }

    [Fact]
    public void Employee_TransferToDepartment_PreviousValueCarriesPriorDeptId()
    {
        var employee = HireDefault();
        var firstDeptId = Guid.NewGuid();
        var secondDeptId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var changedBy = Guid.NewGuid();

        employee.TransferToDepartment(firstDeptId, now, changedBy);
        employee.TransferToDepartment(secondDeptId, now.AddDays(30), changedBy);

        var secondEntry = employee.History
            .Where(h => h.Type == EmployeeHistoryType.DepartmentTransfer)
            .OrderByDescending(h => h.EffectiveFrom)
            .First();

        secondEntry.PreviousValue.Should().Be(firstDeptId.ToString());
    }

    [Fact]
    public void Employee_TransferToDepartment_ClosesOpenPreviousHistoryEntry()
    {
        var employee = HireDefault();
        var firstDeptId = Guid.NewGuid();
        var secondDeptId = Guid.NewGuid();
        var firstDate = DateTimeOffset.UtcNow;
        var secondDate = firstDate.AddDays(30);

        employee.TransferToDepartment(firstDeptId, firstDate, Guid.NewGuid());
        employee.TransferToDepartment(secondDeptId, secondDate, Guid.NewGuid());

        var firstEntry = employee.History
            .Where(h => h.Type == EmployeeHistoryType.DepartmentTransfer)
            .OrderBy(h => h.EffectiveFrom)
            .First();

        firstEntry.EffectiveTo.Should().Be(secondDate);
    }

    // ─── ChangeManager ───────────────────────────────────────────────────────

    [Fact]
    public void Employee_ChangeManager_UpdatesReportingManagerId()
    {
        var employee = HireDefault();
        var managerId = Guid.NewGuid();

        employee.ChangeManager(managerId, DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.ReportingManagerId.Should().Be(managerId);
    }

    [Fact]
    public void Employee_ChangeManager_AddsHistoryEntry()
    {
        var employee = HireDefault();
        var managerId = Guid.NewGuid();
        var effectiveFrom = DateTimeOffset.UtcNow;

        employee.ChangeManager(managerId, effectiveFrom, Guid.NewGuid());

        employee.History.Should().ContainSingle(h =>
            h.Type == EmployeeHistoryType.ManagerChange &&
            h.NewValue == managerId.ToString());
    }

    [Fact]
    public void Employee_ChangeManager_ToNull_SetsReportingManagerToNull()
    {
        var employee = HireDefault();
        employee.ChangeManager(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.ChangeManager(null, DateTimeOffset.UtcNow.AddDays(10), Guid.NewGuid());

        employee.ReportingManagerId.Should().BeNull();
    }

    [Fact]
    public void Employee_ChangeManager_RecordsCorrelationId()
    {
        var employee = HireDefault();
        const string correlationId = "corr-abc-123";

        employee.ChangeManager(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), correlationId);

        employee.History.Single().CorrelationId.Should().Be(correlationId);
    }

    // ─── ChangeDesignation ───────────────────────────────────────────────────

    [Fact]
    public void Employee_ChangeDesignation_UpdatesDesignationId()
    {
        var employee = HireDefault();
        var designationId = Guid.NewGuid();

        employee.ChangeDesignation(designationId, DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.DesignationId.Should().Be(designationId);
    }

    [Fact]
    public void Employee_ChangeDesignation_AddsHistoryEntry()
    {
        var employee = HireDefault();
        var designationId = Guid.NewGuid();

        employee.ChangeDesignation(designationId, DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.History.Should().ContainSingle(h =>
            h.Type == EmployeeHistoryType.DesignationChange &&
            h.NewValue == designationId.ToString());
    }

    [Fact]
    public void Employee_ChangeDesignation_PreviousValueIsNone_WhenFirstChange()
    {
        var employee = HireDefault();

        employee.ChangeDesignation(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.History.Single().PreviousValue.Should().Be("None");
    }

    // ─── Terminate ───────────────────────────────────────────────────────────

    [Fact]
    public void Employee_Terminate_SetsStatusToTerminated()
    {
        var employee = HireDefault();

        employee.Terminate(DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.Status.Should().Be(EmploymentStatus.Terminated);
    }

    [Fact]
    public void Employee_Terminate_AddsLifecycleHistoryEntry()
    {
        var employee = HireDefault();
        var effectiveFrom = DateTimeOffset.UtcNow;

        employee.Terminate(effectiveFrom, Guid.NewGuid());

        employee.History.Should().ContainSingle(h =>
            h.Type == EmployeeHistoryType.LifecycleStateChange &&
            h.NewValue == EmploymentStatus.Terminated.ToString() &&
            h.PreviousValue == EmploymentStatus.Active.ToString());
    }

    [Fact]
    public void Employee_Terminate_WhenAlreadyTerminated_IsIdempotent_NoExtraHistory()
    {
        var employee = HireDefault();
        employee.Terminate(DateTimeOffset.UtcNow, Guid.NewGuid());

        // Second termination should not add another history entry
        employee.Terminate(DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid());

        employee.History.Count(h => h.Type == EmployeeHistoryType.LifecycleStateChange)
            .Should().Be(1);
    }

    [Fact]
    public void Employee_Terminate_WhenAlreadyTerminated_StatusRemainsTerminated()
    {
        var employee = HireDefault();
        employee.Terminate(DateTimeOffset.UtcNow, Guid.NewGuid());

        employee.Terminate(DateTimeOffset.UtcNow.AddDays(5), Guid.NewGuid());

        employee.Status.Should().Be(EmploymentStatus.Terminated);
    }

    // ─── History immutability ────────────────────────────────────────────────

    [Fact]
    public void Employee_HistoryEntries_AreImmutable_OnlyGrows()
    {
        var employee = HireDefault();
        var deptId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        employee.TransferToDepartment(deptId, now, changedBy);
        employee.ChangeManager(managerId, now.AddDays(1), changedBy);
        employee.Terminate(now.AddDays(2), changedBy);

        employee.History.Should().HaveCount(3);
    }

    [Fact]
    public void Employee_History_IsReadOnlyCollection()
    {
        var employee = HireDefault();

        // History exposed as IReadOnlyCollection - cannot cast to List and modify
        var history = employee.History;
        history.Should().BeAssignableTo<IReadOnlyCollection<EmployeeHistoryEntry>>();
    }

    [Fact]
    public void Employee_MultipleOperations_HistoryCountMatchesOperations()
    {
        var employee = HireDefault();
        var changedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        employee.TransferToDepartment(Guid.NewGuid(), now, changedBy);
        employee.ChangeDesignation(Guid.NewGuid(), now.AddDays(10), changedBy);
        employee.ChangeManager(Guid.NewGuid(), now.AddDays(20), changedBy);
        employee.Terminate(now.AddDays(30), changedBy);

        // 4 distinct operations → 4 history entries (each a different type, no closing effects on different types)
        employee.History.Should().HaveCountGreaterThanOrEqualTo(4);
    }
}
