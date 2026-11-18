using System.Net;

namespace TaskLane.Api.Tests;

public sealed class PlatformEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task GetUnknownRoute_WhenAnonymous_IsRefusedWithProblemDetails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync(new Uri("/does-not-exist", UriKind.Relative), cancellationToken);

        // The fallback policy also covers unmatched routes, so anonymous callers cannot probe for routes.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
