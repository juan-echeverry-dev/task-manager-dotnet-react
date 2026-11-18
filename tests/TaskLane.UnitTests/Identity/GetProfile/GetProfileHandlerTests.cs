using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Identity.GetProfile;
using TaskLane.Application.Identity.Ports;
using TaskLane.Domain.Identity;

namespace TaskLane.UnitTests.Identity.GetProfile;

public sealed class GetProfileHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    [Fact]
    public async Task ExistingUser_ReturnsProfileWithoutCredentials()
    {
        var createdAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var user = User.Register(Guid.CreateVersion7(), "Ana@Example.com", "Ana", "hash", createdAt);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await new GetProfileHandler(_users).HandleAsync(
            new GetProfileQuery(user.Id), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new ProfileResponse(user.Id, "Ana", "Ana@Example.com", createdAt));
    }

    [Fact]
    public async Task MissingUser_IsTreatedAsAnInvalidSession()
    {
        var result = await new GetProfileHandler(_users).HandleAsync(
            new GetProfileQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.Unauthorized);
    }
}
