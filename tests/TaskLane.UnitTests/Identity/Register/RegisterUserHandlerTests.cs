using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Identity.Ports;
using TaskLane.Application.Identity.Register;
using TaskLane.Domain.Identity;

namespace TaskLane.UnitTests.Identity.Register;

public sealed class RegisterUserHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly FakeLogger<RegisterUserHandler> _logger = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _users.TryAddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(true);
        _handler = new RegisterUserHandler(new RegisterUserValidator(), _users, _hasher, new FakeTimeProvider(Now), _logger);
    }

    [Fact]
    public async Task ValidCommand_StoresUserWithHashedPassword()
    {
        User? stored = null;
        await _users.TryAddAsync(Arg.Do<User>(user => stored = user), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(
            new RegisterUserCommand(" Ana ", " Ana@Example.com ", "correct horse"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _hasher.Received(1).Hash("correct horse");
        stored.ShouldNotBeNull();
        stored.PasswordHash.ShouldBe("hashed-password");
        stored.Email.ShouldBe("Ana@Example.com");
        stored.NormalizedEmail.ShouldBe("ana@example.com");
        stored.DisplayName.ShouldBe("Ana");
        stored.CreatedAt.ShouldBe(Now);
        stored.Id.Version.ShouldBe(7);
        result.Value.ShouldBe(new RegisterUserResponse(stored.Id, "Ana", "Ana@Example.com", Now));
    }

    [Fact]
    public async Task ValidCommand_LogsRegistrationWithUserIdButWithoutEmail()
    {
        var result = await _handler.HandleAsync(
            new RegisterUserCommand("Ana", "ana@example.com", "correct horse"),
            TestContext.Current.CancellationToken);

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Name.ShouldBe("UserRegistered");
        record.Message.ShouldContain(result.Value.Id.ToString());
        record.Message.ShouldNotContain("ana@example.com", Case.Insensitive);
        record.Message.ShouldNotContain("correct horse");
    }

    [Fact]
    public async Task InvalidCommand_ReturnsValidationErrorWithoutHashing()
    {
        var result = await _handler.HandleAsync(
            new RegisterUserCommand("", "not-an-email", "short"),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.Keys.ShouldBe(["DisplayName", "Email", "Password"], ignoreOrder: true);
        _hasher.DidNotReceiveWithAnyArgs().Hash(default!);
        await _users.DidNotReceiveWithAnyArgs().TryAddAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        _users.TryAddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(
            new RegisterUserCommand("Ana", "ana@example.com", "correct horse"),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Conflict("Email already registered", "An account with this email already exists."));
        _logger.Collector.Count.ShouldBe(0);
    }
}
