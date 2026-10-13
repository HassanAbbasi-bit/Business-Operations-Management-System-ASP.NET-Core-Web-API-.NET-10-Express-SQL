using BusinessOperations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessOperations.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.ReviewNotes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(l => l.ReviewedAtUtc)
            .IsRequired(false);

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        builder.Property(l => l.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasOne(l => l.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ReviewedByEmployee)
            .WithMany()
            .HasForeignKey(l => l.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.EmployeeId, l.Status });
        builder.HasIndex(l => new { l.StartDate, l.EndDate });
    }
}
