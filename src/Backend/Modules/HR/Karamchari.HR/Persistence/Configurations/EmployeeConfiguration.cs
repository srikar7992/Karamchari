// -----------------------------------------------------------------------
// <copyright file="EmployeeConfiguration.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.HR.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.HR.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <summary>
    /// Configures the employee aggregate persistence mapping.
    /// </summary>
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

        builder.Property(x => x.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TimeZoneId)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.EmployeeNumber })
            .IsUnique()
            .HasDatabaseName("UX_Employees_TenantId_EmployeeNumber");

        builder.OwnsMany(x => x.History, h =>
        {
            h.ToTable("EmployeeHistory");
            h.WithOwner().HasForeignKey("EmployeeId");
            h.HasKey(x => x.Id);
            h.Property(x => x.Type).HasConversion<string>().HasMaxLength(64);
            h.Property(x => x.PreviousValue).HasMaxLength(500);
            h.Property(x => x.NewValue).HasMaxLength(500);
            h.Property(x => x.CorrelationId).HasMaxLength(64);
            h.HasIndex(x => new { x.EmployeeId, x.Type });
        });
    }
}
