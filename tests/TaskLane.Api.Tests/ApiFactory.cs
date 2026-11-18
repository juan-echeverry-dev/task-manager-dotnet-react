using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskLane.Api.Tests;

/// <summary>
/// Hosts the API in-process with an isolated, in-memory SQLite database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Data Source=:memory:");
    }
}
