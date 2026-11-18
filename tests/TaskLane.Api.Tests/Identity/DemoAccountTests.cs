using System.Net;

namespace TaskLane.Api.Tests.Identity;

public sealed class DemoAccountTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DemoAccount_IsNotProvisionedOutsideDevelopment()
    {
        // The test host runs in the "Testing" environment, like any non-Development deployment.
        var response = await _client.SignInAsync("demo@tasklane.dev", "TaskLane-Demo-2026");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
