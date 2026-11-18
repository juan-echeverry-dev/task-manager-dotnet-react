using System.Net;

namespace TaskLane.Api.Tests;

public sealed class AuthenticationByDefaultTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task EndpointWithoutAuthorizationAttributes_RequiresAuthentication()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync(new Uri("/test-probes/protected", UriKind.Relative), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Health_IsAnonymous()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
