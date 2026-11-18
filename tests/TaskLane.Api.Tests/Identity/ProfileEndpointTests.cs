using System.Net;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Api.Tests.Identity;

public sealed class ProfileEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsProfileWithoutCredentials()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (email, accessToken) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetProfileAsync(accessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        body.ShouldNotContain("password", Case.Insensitive);
        var profile = JsonDocument.Parse(body).RootElement;
        profile.GetProperty("displayName").GetString().ShouldBe("Ana");
        profile.GetProperty("email").GetString().ShouldBe(email);
        profile.GetProperty("createdAt").GetDateTimeOffset().ShouldBe(factory.TimeProvider.GetUtcNow());
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        await ShouldBeRefusedAsync(await _client.GetProfileAsync(null));
    }

    [Fact]
    public async Task GetProfile_WithExpiredToken_Returns401()
    {
        var (_, accessToken) = await _client.RegisterAndSignInAsync();
        var subject = new JsonWebToken(accessToken).Subject;
        var expired = TestTokens.Create(factory, subject, issuedAt: factory.TimeProvider.GetUtcNow().AddMinutes(-60));

        await ShouldBeRefusedAsync(await _client.GetProfileAsync(expired));
    }

    [Fact]
    public async Task GetProfile_WithMalformedToken_Returns401()
    {
        await ShouldBeRefusedAsync(await _client.GetProfileAsync("not-a-token"));
    }

    [Fact]
    public async Task GetProfile_WithTamperedPayload_Returns401()
    {
        var (_, accessToken) = await _client.RegisterAndSignInAsync();
        var parts = accessToken.Split('.');
        var otherSubject = TestTokens.Create(factory, Guid.CreateVersion7().ToString()).Split('.')[1];

        await ShouldBeRefusedAsync(await _client.GetProfileAsync($"{parts[0]}.{otherSubject}.{parts[2]}"));
    }

    [Fact]
    public async Task GetProfile_WithTokenSignedByAnotherKey_Returns401()
    {
        var (_, accessToken) = await _client.RegisterAndSignInAsync();
        var forged = TestTokens.Create(factory, new JsonWebToken(accessToken).Subject, signingKey: TestTokens.ForeignKey());

        await ShouldBeRefusedAsync(await _client.GetProfileAsync(forged));
    }

    [Fact]
    public async Task GetProfile_WithWrongIssuer_Returns401()
    {
        var (_, accessToken) = await _client.RegisterAndSignInAsync();
        var token = TestTokens.Create(factory, new JsonWebToken(accessToken).Subject, issuer: "someone-else");

        await ShouldBeRefusedAsync(await _client.GetProfileAsync(token));
    }

    [Fact]
    public async Task GetProfile_WithWrongAudience_Returns401()
    {
        var (_, accessToken) = await _client.RegisterAndSignInAsync();
        var token = TestTokens.Create(factory, new JsonWebToken(accessToken).Subject, audience: "someone-else");

        await ShouldBeRefusedAsync(await _client.GetProfileAsync(token));
    }

    [Fact]
    public async Task GetProfile_ForAccountThatNoLongerExists_Returns401()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (email, accessToken) = await _client.RegisterAndSignInAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users
                .Where(user => user.NormalizedEmail == email)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await ShouldBeRefusedAsync(await _client.GetProfileAsync(accessToken));
    }

    [Fact]
    public async Task UnknownRoute_WhenAuthenticated_Returns404()
    {
        var (_, accessToken) = await _client.RegisterAndSignInAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/does-not-exist");
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task ShouldBeRefusedAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain("displayName");
    }
}
