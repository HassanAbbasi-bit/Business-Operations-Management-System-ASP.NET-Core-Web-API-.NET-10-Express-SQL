using BusinessOperations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessOperations.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.DueDateUtc)
            .IsRequired(false);

        builder.Property(t => t.CompletedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasOne(t => t.CreatedByEmployee)
            .WithMany(e => e.CreatedTasks)
            .HasForeignKey(t => t.CreatedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedToEmployee)
            .WithMany(e => e.AssignedTasks)
            .HasForeignKey(t => t.AssignedToEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.AssignedToEmployeeId);
        builder.HasIndex(t => t.Status);
    }
}
