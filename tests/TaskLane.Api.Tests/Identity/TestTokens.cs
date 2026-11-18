using System.Security.Cryptography;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskLane.Api.Tests.Identity;

/// <summary>Builds hand-crafted tokens to exercise every way a session credential can be invalid.</summary>
internal static class TestTokens
{
    public static string Create(
        ApiFactory factory,
        string subject,
        DateTimeOffset? issuedAt = null,
        TimeSpan? lifetime = null,
        string issuer = ApiFactory.Issuer,
        string audience = ApiFactory.Audience,
        string? signingKey = null)
    {
        var issued = issuedAt ?? factory.TimeProvider.GetUtcNow();
        var key = new SymmetricSecurityKey(Convert.FromBase64String(signingKey ?? factory.SigningKey));

        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { ["sub"] = subject, ["jti"] = Guid.NewGuid().ToString() },
            IssuedAt = issued.UtcDateTime,
            NotBefore = issued.UtcDateTime,
            Expires = issued.Add(lifetime ?? TimeSpan.FromMinutes(60)).UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
    }

    public static string ForeignKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
