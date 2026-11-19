using Microsoft.EntityFrameworkCore;

using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;

namespace TaskLane.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the application. Entity configurations live next to it in
/// <c>Configurations/</c> and are discovered automatically.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
