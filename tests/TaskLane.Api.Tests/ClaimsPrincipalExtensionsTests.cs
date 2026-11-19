using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

using TaskLane.Api.Common;

namespace TaskLane.Api.Tests;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsTheGuidInTheSubjectClaim()
    {
        var userId = Guid.CreateVersion7();

        PrincipalWith(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())).GetUserId().ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_WithoutSubjectClaim_ReturnsNull()
    {
        PrincipalWith(new Claim("name", "Ana")).GetUserId().ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void GetUserId_WithSubjectThatIsNotAGuid_ReturnsNull(string subject)
    {
        PrincipalWith(new Claim(JwtRegisteredClaimNames.Sub, subject)).GetUserId().ShouldBeNull();
    }

    private static ClaimsPrincipal PrincipalWith(Claim claim) => new(new ClaimsIdentity([claim], "Test"));
}
