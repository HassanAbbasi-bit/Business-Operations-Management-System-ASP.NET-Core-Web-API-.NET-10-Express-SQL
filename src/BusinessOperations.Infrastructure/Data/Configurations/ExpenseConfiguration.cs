using BusinessOperations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessOperations.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(ex => ex.Id);

        builder.Property(ex => ex.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ex => ex.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ex => ex.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD")
            .IsRequired();

        builder.Property(ex => ex.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ex => ex.ExpenseDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(ex => ex.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ex => ex.ReviewNotes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(ex => ex.ReviewedAtUtc)
            .IsRequired(false);

        builder.Property(ex => ex.CreatedAtUtc)
            .IsRequired();

        builder.Property(ex => ex.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasOne(ex => ex.Employee)
            .WithMany(e => e.Expenses)
            .HasForeignKey(ex => ex.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ex => ex.ReviewedByEmployee)
            .WithMany()
            .HasForeignKey(ex => ex.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ex => new { ex.EmployeeId, ex.Status });
    }
}
