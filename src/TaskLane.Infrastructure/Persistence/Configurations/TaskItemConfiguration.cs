using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;

namespace TaskLane.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    private const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();

        // The only reference from Tasks to Identity: a foreign key so the database keeps owners valid
        // and removes tasks together with their account.
        builder.Property(task => task.OwnerId).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(TaskItem.TitleMaxLength);

        builder.Property(task => task.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength);

        builder.Property(task => task.DueDate);

        // Stored as UTC ticks so timestamps are sortable in SQLite and round-trip exactly.
        builder.Property(task => task.CreatedAt)
            .IsRequired()
            .HasConversion(new UtcTicksConverter());
        builder.Property(task => task.UpdatedAt)
            .IsRequired()
            .HasConversion(new UtcTicksConverter());
        builder.Property(task => task.DeletedAt)
            .HasConversion(new UtcTicksConverter());

        builder.Ignore(task => task.IsDeleted);

        // Cover the default order of both lists, scoped to the owner.
        builder.HasIndex(task => new { task.OwnerId, task.DeletedAt, task.DueDate })
            .HasDatabaseName("IX_Tasks_OwnerId_DeletedAt_DueDate");
        builder.HasIndex(task => new { task.OwnerId, task.DeletedAt, task.CreatedAt })
            .HasDatabaseName("IX_Tasks_OwnerId_DeletedAt_CreatedAt");
    }
}
