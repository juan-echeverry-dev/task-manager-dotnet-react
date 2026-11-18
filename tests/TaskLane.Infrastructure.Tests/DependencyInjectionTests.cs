using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TaskLane.Infrastructure.Identity;
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

    [Fact]
    public void JwtOptions_WithValidSettings_AreBound()
    {
        var signingKey = Convert.ToBase64String(new byte[32]);

        var options = ResolveJwtOptions(signingKey);

        options.Issuer.ShouldBe("issuer");
        options.Audience.ShouldBe("audience");
        options.SigningKey.ShouldBe(signingKey);
        options.LifetimeMinutes.ShouldBe(60);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")] // 31 bytes
    public void JwtOptions_WithMissingOrWeakSigningKey_FailValidation(string? signingKey)
    {
        Should.Throw<OptionsValidationException>(() => ResolveJwtOptions(signingKey));
    }

    [Fact]
    public void JwtOptions_WithoutIssuer_FailValidation()
    {
        Should.Throw<OptionsValidationException>(
            () => ResolveJwtOptions(Convert.ToBase64String(new byte[32]), issuer: null));
    }

    private static JwtOptions ResolveJwtOptions(string? signingKey, string? issuer = "issuer")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Data Source=:memory:",
                ["Authentication:Jwt:Issuer"] = issuer,
                ["Authentication:Jwt:Audience"] = "audience",
                ["Authentication:Jwt:SigningKey"] = signingKey,
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();

        return provider.GetRequiredService<IOptions<JwtOptions>>().Value;
    }
}
