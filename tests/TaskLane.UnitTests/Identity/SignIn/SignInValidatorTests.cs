using TaskLane.Application.Identity.SignIn;

namespace TaskLane.UnitTests.Identity.SignIn;

public sealed class SignInValidatorTests
{
    private readonly SignInValidator _validator = new();

    [Fact]
    public void FilledFields_Pass()
    {
        // Registration password rules do not apply: a non-matching password just fails to sign in.
        _validator.Validate(new SignInCommand("ana@example.com", "x")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Email is required.")]
    [InlineData("   ", "Email is required.")]
    public void Email_Blank_IsRejected(string email, string message)
    {
        _validator.Validate(new SignInCommand(email, "password")).Errors
            .ShouldHaveSingleItem().ErrorMessage.ShouldBe(message);
    }

    [Fact]
    public void Password_Empty_IsRejected()
    {
        _validator.Validate(new SignInCommand("ana@example.com", "")).Errors
            .ShouldHaveSingleItem().ErrorMessage.ShouldBe("Password is required.");
    }
}
