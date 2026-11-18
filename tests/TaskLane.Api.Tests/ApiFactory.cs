using System.Security.Cryptography;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Api.Tests;

/// <summary>
/// Hosts the API in-process with a private, migrated in-memory SQLite database, a controllable
/// clock, a per-factory signing key, and captured logs.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "tasklane-tests";
    public const string Audience = "tasklane-tests";

    private readonly string _connectionString =
        $"Data Source=tasklane-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private readonly SqliteConnection _keepAliveConnection;

    public ApiFactory()
    {
        // A shared-cache in-memory database lives as long as one connection to it stays open.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));

    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public FakeLogCollector LogCollector => Services.GetFakeLogCollector();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Authentication:Jwt:Issuer", Issuer);
        builder.UseSetting("Authentication:Jwt:Audience", Audience);
        builder.UseSetting("Authentication:Jwt:SigningKey", SigningKey);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
            services.AddLogging(logging => logging.AddFakeLogging());
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

        return host;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _keepAliveConnection.DisposeAsync();
    }
}
