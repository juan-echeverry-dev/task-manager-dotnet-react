namespace TaskLane.Api.Tests.Identity;

public sealed class AuthLoggingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AuthenticationFlows_LogEventsWithoutCredentialsOrEmails()
    {
        var (email, accessToken) = await _client.RegisterAndSignInAsync();
        await _client.SignInAsync(email, "a wrong password");
        await _client.GetProfileAsync(accessToken);

        var records = factory.LogCollector.GetSnapshot();
        var eventNames = records.Select(record => record.Id.Name).ToHashSet();
        eventNames.ShouldContain("UserRegistered");
        eventNames.ShouldContain("SignInSucceeded");
        eventNames.ShouldContain("SignInFailed");

        var forbidden = new[] { IdentityClient.Password, "a wrong password", email, accessToken };
        foreach (var record in records)
        {
            var text = string.Join(
                '\n',
                record.Message,
                record.Exception?.ToString() ?? string.Empty,
                string.Join(',', (record.StructuredState ?? []).Select(pair => pair.Value)));
            foreach (var value in forbidden)
            {
                text.ShouldNotContain(value, Case.Insensitive, $"Log record '{record.Category}' leaked a credential or email.");
            }
        }
    }
}
