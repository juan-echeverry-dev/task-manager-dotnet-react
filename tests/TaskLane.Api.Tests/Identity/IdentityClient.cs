using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskLane.Api.Tests.Identity;

/// <summary>Drives the Identity endpoints the way the SPA does.</summary>
internal static class IdentityClient
{
    public const string Password = "correct horse battery";

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync(
            "/api/auth/register",
            new { displayName = "Ana", email, password },
            TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> SignInAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/sign-in", new { email, password }, TestContext.Current.CancellationToken);

    /// <summary>Registers a new account and signs in; returns its email and access token.</summary>
    public static async Task<(string Email, string AccessToken)> RegisterAndSignInAsync(this HttpClient client)
    {
        var email = UniqueEmail();
        (await client.RegisterAsync(email)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await client.SignInAsync(email);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);

        return (email, body.RootElement.GetProperty("accessToken").GetString()!);
    }

    public static Task<HttpResponseMessage> GetProfileAsync(this HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
