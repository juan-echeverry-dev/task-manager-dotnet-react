using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using TaskLane.Infrastructure.Identity;

namespace TaskLane.Api.Identity;

internal static class AuthenticationSetup
{
    /// <summary>
    /// JWT bearer authentication (ADR 0006) and authentication required by default: endpoints opt out
    /// explicitly with <see cref="AllowAnonymousAttribute"/>.
    /// </summary>
    public static IServiceCollection AddTokenAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>(ConfigureBearer);

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
    {
        var jwt = jwtOptions.Value;

        // Keep "sub" as "sub" instead of mapping it to a long WS-* claim type.
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,

            // Lifetime is checked against the injected clock so tests can control time.
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;

                return expires is not null && now < expires.Value && (notBefore is null || notBefore.Value <= now);
            },
        };
    }
}
