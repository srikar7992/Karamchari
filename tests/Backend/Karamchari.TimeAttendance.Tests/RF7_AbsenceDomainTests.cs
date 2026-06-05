namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Absence;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;

/// <summary>
/// RF#7 — Absence Domain: MedicalRestriction blocking, RTW phased return, NCNS workflow.
/// </summary>
public sealed class RF7_AbsenceDomainTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── MedicalRestriction ────────────────────────────────────────────────

    [Fact]
    public void MedicalRestriction_ActiveOnDate_BlocksRosterAssignment()
    {
        var restriction = MedicalRestriction.Create(
            "t1", Guid.NewGuid(), "No heavy lifting",
            effectiveFrom: Today.AddDays(-5), effectiveTo: Today.AddDays(20),
            issuedBy: "occupational-health", rosteringRestriction: true);

        restriction.IsActiveOn(Today).Should().BeTrue("restriction active today → rostering must check before assigning");
        restriction.RosteringRestriction.Should().BeTrue("rostering engine reads this flag");
    }

    [Fact]
    public void MedicalRestriction_Expired_DoesNotBlock()
    {
        var restriction = MedicalRestriction.Create(
            "t1", Guid.NewGuid(), "Post-surgery recovery",
            effectiveFrom: Today.AddDays(-30), effectiveTo: Today.AddDays(-1),
            issuedBy: "occupational-health", rosteringRestriction: true);

        restriction.IsActiveOn(Today).Should().BeFalse("restriction expired yesterday");
    }

    [Fact]
    public void MedicalRestriction_NoEndDate_ActiveIndefinitely()
    {
        var restriction = MedicalRestriction.Create(
            "t1", Guid.NewGuid(), "Permanent disability accommodation",
            effectiveFrom: Today.AddDays(-60), effectiveTo: null,
            issuedBy: "hr", rosteringRestriction: false);

        restriction.IsActiveOn(Today).Should().BeTrue("null effectiveTo = indefinite");
        restriction.IsActiveOn(Today.AddDays(365)).Should().BeTrue("still active next year");
    }

    [Fact]
    public void MedicalRestriction_Lift_IsActiveFalse()
    {
        var restriction = MedicalRestriction.Create(
            "t1", Guid.NewGuid(), "Light duties only",
            effectiveFrom: Today.AddDays(-10), effectiveTo: Today.AddDays(30),
            issuedBy: "doctor", rosteringRestriction: true);

        restriction.Lift();

        restriction.IsActive.Should().BeFalse("lifted restriction no longer blocks");
        restriction.IsActiveOn(Today).Should().BeFalse();
    }

    [Fact]
    public void MedicalRestriction_StartDateBeforeToday_NotYetRestricted()
    {
        var restriction = MedicalRestriction.Create(
            "t1", Guid.NewGuid(), "Future restriction",
            effectiveFrom: Today.AddDays(5), effectiveTo: Today.AddDays(30),
            issuedBy: "hr", rosteringRestriction: true);

        restriction.IsActiveOn(Today).Should().BeFalse("restriction not yet in effect");
    }

    // ── Roster blocking simulation ────────────────────────────────────────

    [Fact]
    public void RosterAssignment_MedicalRestriction_Active_RejectedByEngine()
    {
        // Simulate rostering engine check (roster engine is external — we test the guard interface)
        var employeeId = Guid.NewGuid();
        var restriction = MedicalRestriction.Create(
            "t1", employeeId, "No night shifts",
            effectiveFrom: Today.AddDays(-1), effectiveTo: Today.AddDays(14),
            issuedBy: "doctor", rosteringRestriction: true);

        var assignmentDate = Today;
        bool canAssign = !(restriction.IsActiveOn(assignmentDate) && restriction.RosteringRestriction);

        canAssign.Should().BeFalse("rostering engine must block night shift assignment when restriction active");
    }

    // ── ReturnToWorkAssessment ────────────────────────────────────────────

    [Fact]
    public void ReturnToWork_PhasedReturn_ShiftLimitationsDocumented()
    {
        var caseId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var rtw = ReturnToWorkAssessment.Conduct(
            "t1", caseId, employeeId,
            returnDate: Today,
            fitnessConfirmed: true,
            phasedReturnRequired: true,
            phasedReturnPlan: "4 hours/day for 2 weeks, then full time",
            workRestrictionsApply: true,
            workRestrictions: "No overtime, no night shifts for 14 days",
            conductedBy: "hr-manager");

        rtw.FitnessConfirmed.Should().BeTrue();
        rtw.PhasedReturnRequired.Should().BeTrue();
        rtw.PhasedReturnPlan.Should().Contain("4 hours");
        rtw.WorkRestrictionsApply.Should().BeTrue();
        rtw.WorkRestrictions.Should().Contain("No overtime");
    }

    [Fact]
    public void ReturnToWork_FullReturn_NoRestrictions()
    {
        var rtw = ReturnToWorkAssessment.Conduct(
            "t1", Guid.NewGuid(), Guid.NewGuid(),
            returnDate: Today,
            fitnessConfirmed: true,
            phasedReturnRequired: false,
            phasedReturnPlan: null,
            workRestrictionsApply: false,
            workRestrictions: null,
            conductedBy: "hr");

        rtw.PhasedReturnRequired.Should().BeFalse();
        rtw.WorkRestrictionsApply.Should().BeFalse();
    }

    // ── AbsenceCase: NCNS and unauthorized absence ───────────────────────

    [Fact]
    public void AbsenceCase_NoCallNoShow_ContactAttempted_RecordedCorrectly()
    {
        var c = AbsenceCase.Raise("t1", "NCNS-001", Guid.NewGuid(), Guid.NewGuid(),
            AbsenceType.NoCallNoShow, Today, 1m);

        c.RecordContactAttempt("line-manager");

        c.ContactAttempted.Should().BeTrue();
        c.ContactAttemptedBy.Should().Be("line-manager");
        c.ContactAttemptedAt.Should().NotBeNull();
    }

    [Fact]
    public void AbsenceCase_UnauthorizedAbsence_EscalateToHr()
    {
        var c = AbsenceCase.Raise("t1", "UA-001", Guid.NewGuid(), Guid.NewGuid(),
            AbsenceType.UnauthorizedAbsence, Today, 3m);

        c.EscalateToHr();

        c.Status.Should().Be(AbsenceCaseStatus.HrIntervention);
    }

    [Fact]
    public void AbsenceCase_Suspension_PayrollImpact_OnResolve()
    {
        var c = AbsenceCase.Raise("t1", "SUS-001", Guid.NewGuid(), null,
            AbsenceType.Suspension, Today, 5m);

        c.Resolve("hr-director", AbsenceResolution.DisciplinaryAction,
            "Written warning + 3-day LOP", payrollImpact: true);

        c.PayrollImpact.Should().BeTrue("suspension resolution triggers payroll deduction");
        c.Resolution.Should().Be(AbsenceResolution.DisciplinaryAction);
    }

    [Fact]
    public void AbsenceCase_MedicalRestrictionType_DifferentFromLeave()
    {
        // Medical restriction absence ≠ approved leave. Different lifecycle.
        var c = AbsenceCase.Raise("t1", "MR-001", Guid.NewGuid(), null,
            AbsenceType.MedicalRestriction, Today, 2m);

        c.AbsenceType.Should().Be(AbsenceType.MedicalRestriction);
        c.Status.Should().Be(AbsenceCaseStatus.Open, "separate lifecycle from leave request");
    }

    [Fact]
    public void AbsenceCase_DocumentationReceived_StatusChanges()
    {
        var c = AbsenceCase.Raise("t1", "NCNS-002", Guid.NewGuid(), null,
            AbsenceType.NoCallNoShow, Today, 1m);

        c.RequestDocumentation();
        c.Status.Should().Be(AbsenceCaseStatus.DocumentationPending);

        c.ReceiveDocumentation();
        c.Status.Should().Be(AbsenceCaseStatus.InReview);
        c.DocumentationReceived.Should().BeTrue();
    }

    [Fact]
    public void AbsenceCase_ExcusedWithPay_NoPayrollImpact()
    {
        var c = AbsenceCase.Raise("t1", "FA-001", Guid.NewGuid(), null,
            AbsenceType.UnplannedAbsence, Today, 1m);

        c.ReceiveExplanation("Medical emergency, documents provided");
        c.Resolve("hr", AbsenceResolution.ExcusedWithPay, "Family emergency, first time", payrollImpact: false);

        c.PayrollImpact.Should().BeFalse();
        c.Resolution.Should().Be(AbsenceResolution.ExcusedWithPay);
    }
}
