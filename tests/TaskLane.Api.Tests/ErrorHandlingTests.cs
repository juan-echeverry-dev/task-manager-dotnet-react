using System.Net;

namespace TaskLane.Api.Tests;

public sealed class ErrorHandlingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UnexpectedException_ReturnsProblemDetailsWithoutInternals()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync(new Uri("/test-probes/throws", UriKind.Relative), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        body.ShouldNotContain("Sensitive internal detail");
        body.ShouldNotContain("Data Source");
        body.ShouldNotContain("InvalidOperationException");
        body.ShouldNotContain("stackTrace", Case.Insensitive);
        body.ShouldNotContain("exception", Case.Insensitive);
    }
}
