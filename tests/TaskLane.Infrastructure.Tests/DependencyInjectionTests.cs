using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WithoutConnectionString_ThrowsDescriptiveError()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Should.Throw<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(configuration));

        exception.Message.ShouldContain(DependencyInjection.ConnectionStringName);
    }

    [Fact]
    public void AddInfrastructure_RegistersAppDbContextUsingSqlite()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Data Source=:memory:",
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.IsSqlite().ShouldBeTrue();
    }
}
