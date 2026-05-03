using Karamchari.HR.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.HR.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.EmployeeNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.WorkEmail)
            .HasMaxLength(320);

        builder.Property(x => x.HiredOn)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.EmployeeNumber })
            .IsUnique()
            .HasDatabaseName("UX_Employees_TenantId_EmployeeNumber");
    }
}
