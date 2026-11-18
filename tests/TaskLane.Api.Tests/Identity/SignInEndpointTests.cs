using System.Net;
using System.Text.Json;

namespace TaskLane.Api.Tests.Identity;

public sealed class SignInEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SignIn_CorrectCredentials_ReturnsBearerToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = IdentityClient.UniqueEmail();
        await _client.RegisterAsync(email);

        var response = await _client.SignInAsync($" {email.ToUpperInvariant()} ");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)).RootElement;
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("tokenType").GetString().ShouldBe("Bearer");
        body.GetProperty("expiresAt").GetDateTimeOffset().ShouldBe(factory.TimeProvider.GetUtcNow().AddMinutes(60));
    }

    [Fact]
    public async Task SignIn_BlankFields_Returns400()
    {
        var response = await _client.SignInAsync("", "");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task SignIn_WrongPasswordAndUnknownEmail_AreIndistinguishable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = IdentityClient.UniqueEmail();
        await _client.RegisterAsync(email);

        var wrongPassword = await _client.SignInAsync(email, "wrong password");
        var unknownEmail = await _client.SignInAsync(IdentityClient.UniqueEmail());

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var wrongPasswordBody = await wrongPassword.Content.ReadAsStringAsync(cancellationToken);
        var unknownEmailBody = await unknownEmail.Content.ReadAsStringAsync(cancellationToken);
        wrongPasswordBody.ShouldBe(unknownEmailBody);
        Headers(wrongPassword).ShouldBe(Headers(unknownEmail));
        var problem = JsonDocument.Parse(wrongPasswordBody).RootElement;
        problem.GetProperty("title").GetString().ShouldBe("Invalid credentials");
        problem.GetProperty("detail").GetString().ShouldBe("Invalid email or password.");
    }

    private static string[] Headers(HttpResponseMessage response) =>
        [.. response.Headers.Concat(response.Content.Headers)
            .Where(header => header.Key != "Content-Length")
            .Select(header => $"{header.Key}: {string.Join(',', header.Value)}")
            .Order(StringComparer.Ordinal)];
}
