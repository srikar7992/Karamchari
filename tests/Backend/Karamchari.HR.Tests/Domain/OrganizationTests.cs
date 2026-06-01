using FluentAssertions;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Relationships;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class OrganizationTests
{
    // ─── LegalEntity ─────────────────────────────────────────────────────────

    [Fact]
    public void LegalEntity_Create_SetsActiveTrue()
    {
        var entity = LegalEntity.Create("tenant-a", "Acme Corp", "TAX-001", "REG-001");

        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void LegalEntity_Create_SetsName()
    {
        var entity = LegalEntity.Create("tenant-a", "  Acme Corp  ", "TAX-001", "REG-001");

        entity.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public void LegalEntity_Create_SetsTenantId()
    {
        var entity = LegalEntity.Create("tenant-xyz", "Acme Corp", "TAX-001", "REG-001");

        entity.TenantId.Should().Be("tenant-xyz");
    }

    [Fact]
    public void LegalEntity_Create_SetsTaxId()
    {
        var entity = LegalEntity.Create("tenant-a", "Acme Corp", "  TAX-999  ", "REG-001");

        entity.TaxId.Should().Be("TAX-999");
    }

    [Fact]
    public void LegalEntity_Create_SetsRegistrationNumber()
    {
        var entity = LegalEntity.Create("tenant-a", "Acme Corp", "TAX-001", "  REG-XYZ  ");

        entity.RegistrationNumber.Should().Be("REG-XYZ");
    }

    [Fact]
    public void LegalEntity_Create_AssignsNewGuidId()
    {
        var entity = LegalEntity.Create("tenant-a", "Acme Corp", "TAX-001", "REG-001");

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void LegalEntity_Create_TwoEntities_HaveDifferentIds()
    {
        var e1 = LegalEntity.Create("tenant-a", "Corp A", "TAX-001", "REG-001");
        var e2 = LegalEntity.Create("tenant-a", "Corp B", "TAX-002", "REG-002");

        e1.Id.Should().NotBe(e2.Id);
    }

    // ─── Branch ───────────────────────────────────────────────────────────────

    [Fact]
    public void Branch_Create_SetsActiveTrue()
    {
        var branch = Branch.Create("tenant-a", "Headquarters", "HQ");

        branch.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Branch_Create_SetsName()
    {
        var branch = Branch.Create("tenant-a", "  Mumbai Office  ", "MUM");

        branch.Name.Should().Be("Mumbai Office");
    }

    [Fact]
    public void Branch_Create_SetsCodeUppercased()
    {
        var branch = Branch.Create("tenant-a", "Bangalore", "blr");

        branch.Code.Should().Be("BLR");
    }

    [Fact]
    public void Branch_Create_SetsTenantId()
    {
        var branch = Branch.Create("tenant-xyz", "Delhi", "DEL");

        branch.TenantId.Should().Be("tenant-xyz");
    }

    [Fact]
    public void Branch_Create_DefaultsToUtcTimeZone()
    {
        var branch = Branch.Create("tenant-a", "London", "LON");

        branch.TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public void Branch_Create_WithCustomTimeZone_SetsTimeZoneId()
    {
        var branch = Branch.Create("tenant-a", "Mumbai", "MUM", "Asia/Kolkata");

        branch.TimeZoneId.Should().Be("Asia/Kolkata");
    }

    [Fact]
    public void Branch_Create_AssignsNewGuidId()
    {
        var branch = Branch.Create("tenant-a", "HQ", "HQ");

        branch.Id.Should().NotBe(Guid.Empty);
    }

    // ─── Designation ─────────────────────────────────────────────────────────

    [Fact]
    public void Designation_Create_StoresNameAndLevel()
    {
        var designation = Designation.Create("tenant-a", "Senior Engineer", "SE", 5);

        designation.Name.Should().Be("Senior Engineer");
        designation.Level.Should().Be(5);
    }

    [Fact]
    public void Designation_Create_SetsCodeUppercased()
    {
        var designation = Designation.Create("tenant-a", "Manager", "mgr", 6);

        designation.Code.Should().Be("MGR");
    }

    [Fact]
    public void Designation_Create_SetsActiveTrue()
    {
        var designation = Designation.Create("tenant-a", "Intern", "INT", 1);

        designation.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Designation_Create_SetsTenantId()
    {
        var designation = Designation.Create("tenant-beta", "Director", "DIR", 8);

        designation.TenantId.Should().Be("tenant-beta");
    }

    [Fact]
    public void Designation_Create_AssignsNewGuidId()
    {
        var designation = Designation.Create("tenant-a", "VP", "VP", 9);

        designation.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Designation_Create_TrimsNameWhitespace()
    {
        var designation = Designation.Create("tenant-a", "  Software Architect  ", "SA", 7);

        designation.Name.Should().Be("Software Architect");
    }

    // ─── EmployeeRelationship ─────────────────────────────────────────────────

    [Fact]
    public void EmployeeRelationship_SelfRelationship_Throws()
    {
        var selfId = Guid.NewGuid();

        var act = () => EmployeeRelationship.Create(
            "tenant-a", selfId, selfId, RelationshipType.DirectManager,
            new DateOnly(2025, 1, 1));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*relationship with themselves*");
    }

    [Fact]
    public void EmployeeRelationship_InvalidEffectiveDates_Throws_WhenEffectiveToBeforeEffectiveFrom()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var effectiveFrom = new DateOnly(2025, 6, 1);
        var effectiveTo = new DateOnly(2025, 5, 1); // before effectiveFrom

        var act = () => EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.Mentor,
            effectiveFrom, effectiveTo);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*EffectiveTo*before*EffectiveFrom*");
    }

    [Fact]
    public void EmployeeRelationship_ValidDates_Succeeds()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var effectiveFrom = new DateOnly(2025, 1, 1);
        var effectiveTo = new DateOnly(2025, 12, 31);

        var relationship = EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.DirectManager,
            effectiveFrom, effectiveTo);

        relationship.Should().NotBeNull();
        relationship.FromEmployeeId.Should().Be(from);
        relationship.ToEmployeeId.Should().Be(to);
        relationship.EffectiveFrom.Should().Be(effectiveFrom);
        relationship.EffectiveTo.Should().Be(effectiveTo);
    }

    [Fact]
    public void EmployeeRelationship_ValidDates_SameDateEffectiveFromAndTo_Succeeds()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var sameDate = new DateOnly(2025, 3, 15);

        var act = () => EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.ActingManager,
            sameDate, sameDate);

        act.Should().NotThrow();
    }

    [Fact]
    public void EmployeeRelationship_NullEffectiveTo_IsActive()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var relationship = EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.Mentor,
            new DateOnly(2024, 1, 1), null);

        relationship.IsActive.Should().BeTrue();
        relationship.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void EmployeeRelationship_Create_SetsTenantId()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var relationship = EmployeeRelationship.Create(
            "tenant-demo", from, to, RelationshipType.DirectManager,
            new DateOnly(2025, 1, 1));

        relationship.TenantId.Should().Be("tenant-demo");
    }

    [Fact]
    public void EmployeeRelationship_Create_SetsRelationshipType()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var relationship = EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.SkipLevelManager,
            new DateOnly(2025, 1, 1));

        relationship.RelationshipType.Should().Be(RelationshipType.SkipLevelManager);
    }

    [Fact]
    public void EmployeeRelationship_Create_EmptyTenantId_ThrowsArgumentException()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var act = () => EmployeeRelationship.Create(
            "", from, to, RelationshipType.Mentor,
            new DateOnly(2025, 1, 1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EmployeeRelationship_Create_WithContext_TrimsContext()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var relationship = EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.ProjectManager,
            new DateOnly(2025, 1, 1), null, "  Project Alpha  ");

        relationship.Context.Should().Be("Project Alpha");
    }

    [Fact]
    public void EmployeeRelationship_Terminate_SetsEffectiveTo()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var effectiveFrom = new DateOnly(2024, 1, 1);
        var relationship = EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.Mentor, effectiveFrom);

        var terminatedOn = new DateOnly(2025, 6, 1);
        relationship.Terminate(terminatedOn);

        relationship.EffectiveTo.Should().Be(terminatedOn);
        relationship.IsActive.Should().BeFalse();
    }

    [Fact]
    public void EmployeeRelationship_Terminate_AlreadyTerminated_ThrowsInvalidOperationException()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var relationship = EmployeeRelationship.Create(
            "tenant-a", from, to, RelationshipType.Mentor,
            new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)); // already terminated

        var act = () => relationship.Terminate(new DateOnly(2025, 1, 1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already terminated*");
    }
}
