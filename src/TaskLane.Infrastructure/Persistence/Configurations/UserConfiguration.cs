using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TaskLane.Domain.Identity;

namespace TaskLane.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(User.EmailMaxLength);

        builder.Property(user => user.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(User.EmailMaxLength);

        // Uniqueness is enforced by the database as well, so concurrent registrations cannot both succeed.
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedEmail");

        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(User.DisplayNameMaxLength);

        builder.Property(user => user.PasswordHash).IsRequired();

        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
