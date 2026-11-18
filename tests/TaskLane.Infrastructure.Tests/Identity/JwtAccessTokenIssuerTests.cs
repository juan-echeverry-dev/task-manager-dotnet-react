using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using TaskLane.Domain.Identity;
using TaskLane.Infrastructure.Identity;

namespace TaskLane.Infrastructure.Tests.Identity;

public sealed class JwtAccessTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly JwtOptions _options = new()
    {
        Issuer = "issuer",
        Audience = "audience",
        SigningKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()),
    };

    private readonly User _user = User.Register(Guid.CreateVersion7(), "ana@example.com", "Ana", "hash", Now);

    [Fact]
    public async Task Issue_ProducesTokenValidForConfiguredKeyIssuerAndAudience()
    {
        var token = CreateIssuer().Issue(_user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            IssuerSigningKey = new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = false,
        });

        validation.IsValid.ShouldBeTrue(validation.Exception?.Message);
    }

    [Fact]
    public void Issue_ContainsOnlyIdentifierClaims()
    {
        var jwt = new JsonWebToken(CreateIssuer().Issue(_user).Value);

        jwt.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
        jwt.Subject.ShouldBe(_user.Id.ToString());
        jwt.Id.ShouldNotBeNullOrWhiteSpace();
        jwt.Claims.Select(claim => claim.Type).Distinct()
            .ShouldBe(["sub", "jti", "iat", "nbf", "exp", "iss", "aud"], ignoreOrder: true);
        jwt.EncodedPayload.ShouldNotContain(Base64UrlEncoder.Encode("ana@example.com"));
    }

    [Fact]
    public void Issue_ExpiresSixtyMinutesAfterTheCurrentTime()
    {
        var token = CreateIssuer().Issue(_user);

        var jwt = new JsonWebToken(token.Value);
        token.ExpiresAt.ShouldBe(Now.AddMinutes(60));
        jwt.ValidTo.ShouldBe(Now.AddMinutes(60).UtcDateTime);
        jwt.ValidFrom.ShouldBe(Now.UtcDateTime);
        jwt.IssuedAt.ShouldBe(Now.UtcDateTime);
    }

    [Fact]
    public void Issue_EachTokenHasAUniqueId()
    {
        var issuer = CreateIssuer();

        new JsonWebToken(issuer.Issue(_user).Value).Id.ShouldNotBe(new JsonWebToken(issuer.Issue(_user).Value).Id);
    }

    private JwtAccessTokenIssuer CreateIssuer() => new(Options.Create(_options), new FakeTimeProvider(Now));
}
