using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskLane.Api.Tests.Identity;

public sealed class RegisterEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "correct horse battery";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_NewAccount_Returns201WithProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = UniqueEmail();

        var response = await RegisterAsync(" Ana ", $" {email} ", Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location?.OriginalString.ShouldBe("/api/users/me");
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        body.ShouldNotContain(Password);
        body.ShouldNotContain("passwordHash", Case.Insensitive);
        var profile = JsonDocument.Parse(body).RootElement;
        profile.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        profile.GetProperty("displayName").GetString().ShouldBe("Ana");
        profile.GetProperty("email").GetString().ShouldBe(email);
        profile.GetProperty("createdAt").GetDateTimeOffset().ShouldBe(factory.TimeProvider.GetUtcNow());
    }

    [Fact]
    public async Task Register_InvalidFields_Returns400WithFieldErrors()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await RegisterAsync("   ", "not-an-email", "short");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken))
            .RootElement.GetProperty("errors");
        errors.GetProperty("displayName")[0].GetString().ShouldBe("Display name is required.");
        errors.GetProperty("email")[0].GetString().ShouldBe("Enter a valid email address.");
        errors.GetProperty("password")[0].GetString().ShouldBe("Password must be between 8 and 128 characters.");
    }

    [Fact]
    public async Task Register_MissingFields_Returns400WithFieldErrors()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync("/api/auth/register", new { }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken))
            .RootElement.GetProperty("errors");
        errors.TryGetProperty("displayName", out _).ShouldBeTrue();
        errors.TryGetProperty("email", out _).ShouldBeTrue();
        errors.TryGetProperty("password", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Register_EmailAlreadyRegistered_Returns409()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        (await RegisterAsync("Ana", email, Password)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await RegisterAsync("Ana again", $" {email.ToUpperInvariant()} ", Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)).RootElement;
        problem.GetProperty("title").GetString().ShouldBe("Email already registered");
        problem.GetProperty("detail").GetString().ShouldBe("An account with this email already exists.");
    }

    private Task<HttpResponseMessage> RegisterAsync(string displayName, string email, string password) =>
        _client.PostAsJsonAsync(
            "/api/auth/register",
            new { displayName, email, password },
            TestContext.Current.CancellationToken);

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}
