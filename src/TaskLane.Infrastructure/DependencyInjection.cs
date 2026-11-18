using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TaskLane.Application.Identity.Ports;
using TaskLane.Infrastructure.Identity;
using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    /// <summary>Registers persistence and other infrastructure adapters for Application ports.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Authentication:Jwt:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Authentication:Jwt:Audience is required.")
            .Validate(
                options => options.HasValidSigningKey(),
                $"Authentication:Jwt:SigningKey must be a base64 key of at least {JwtOptions.MinimumSigningKeyBytes} bytes.")
            .Validate(
                options => options.LifetimeMinutes is >= 1 and <= 1440,
                "Authentication:Jwt:LifetimeMinutes must be between 1 and 1440.")
            .ValidateOnStart();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();

        return services;
    }
}
