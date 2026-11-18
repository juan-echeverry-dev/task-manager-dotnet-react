using Microsoft.Extensions.Logging.Testing;

using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Identity.Ports;
using TaskLane.Application.Identity.SignIn;
using TaskLane.Domain.Identity;

namespace TaskLane.UnitTests.Identity.SignIn;

public sealed class SignInHandlerTests
{
    private const string Password = "correct horse";
    private static readonly DateTimeOffset ExpiresAt = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly Error InvalidCredentials = Error.Unauthorized("Invalid credentials", "Invalid email or password.");

    private readonly User _ana = User.Register(
        Guid.CreateVersion7(), "Ana@Example.com", "Ana", "stored-hash", new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAccessTokenIssuer _tokens = Substitute.For<IAccessTokenIssuer>();
    private readonly FakeLogger<SignInHandler> _logger = new();
    private readonly SignInHandler _handler;

    public SignInHandlerTests()
    {
        _users.GetByNormalizedEmailAsync("ana@example.com", Arg.Any<CancellationToken>()).Returns(_ana);
        _hasher.Verify("stored-hash", Password).Returns(true);
        _tokens.Issue(_ana).Returns(new AccessToken("signed-token", ExpiresAt));
        _handler = new SignInHandler(new SignInValidator(), _users, _hasher, _tokens, _logger);
    }

    [Fact]
    public async Task CorrectCredentials_ReturnBearerToken()
    {
        var result = await SignInAsync(" ANA@example.com ", Password);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SignInResponse("signed-token", "Bearer", ExpiresAt));
    }

    [Fact]
    public async Task CorrectCredentials_LogSuccessWithUserIdOnly()
    {
        await SignInAsync("ana@example.com", Password);

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Name.ShouldBe("SignInSucceeded");
        record.Message.ShouldContain(_ana.Id.ToString());
        record.Message.ShouldNotContain("ana@example.com", Case.Insensitive);
    }

    [Fact]
    public async Task WrongPassword_ReturnsInvalidCredentials()
    {
        var result = await SignInAsync("ana@example.com", "wrong password");

        result.Error.ShouldBe(InvalidCredentials);
        _tokens.DidNotReceiveWithAnyArgs().Issue(default!);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsTheSameInvalidCredentials()
    {
        var result = await SignInAsync("nobody@example.com", Password);

        result.Error.ShouldBe(InvalidCredentials);
    }

    [Fact]
    public async Task UnknownEmail_StillVerifiesAPassword_SoTimingMatchesAWrongPassword()
    {
        await SignInAsync("nobody@example.com", Password);

        _hasher.Received(1).Verify(null, Password);
    }

    [Theory]
    [InlineData("ana@example.com", "wrong password")]
    [InlineData("nobody@example.com", Password)]
    public async Task FailedSignIn_LogsEventWithoutEmailOrPassword(string email, string password)
    {
        await SignInAsync(email, password);

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Name.ShouldBe("SignInFailed");
        record.Message.ShouldNotContain(email, Case.Insensitive);
        record.Message.ShouldNotContain(password);
        (record.StructuredState ?? []).ShouldAllBe(pair => pair.Key == "{OriginalFormat}");
    }

    [Fact]
    public async Task BlankFields_ReturnValidationErrorWithoutLookup()
    {
        var result = await SignInAsync("", "");

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.Keys.ShouldBe(["Email", "Password"], ignoreOrder: true);
        await _users.DidNotReceiveWithAnyArgs().GetByNormalizedEmailAsync(default!, TestContext.Current.CancellationToken);
    }

    private Task<Result<SignInResponse>> SignInAsync(string email, string password) =>
        _handler.HandleAsync(new SignInCommand(email, password), TestContext.Current.CancellationToken);
}
