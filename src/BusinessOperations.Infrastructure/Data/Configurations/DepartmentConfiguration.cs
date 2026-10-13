using BusinessOperations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessOperations.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(d => d.Name)
            .IsUnique();

        builder.Property(d => d.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(d => d.ManagerEmployeeId)
            .IsRequired(false);

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasMany(d => d.Employees)
            .WithOne(e => e.Department)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
