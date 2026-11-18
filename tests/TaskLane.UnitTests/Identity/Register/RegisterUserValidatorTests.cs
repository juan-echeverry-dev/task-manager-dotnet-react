using TaskLane.Application.Identity.Register;

namespace TaskLane.UnitTests.Identity.Register;

public sealed class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        _validator.Validate(new RegisterUserCommand(" Ana ", " ana@example.com ", "correct horse")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Display name is required.")]
    [InlineData("   ", "Display name is required.")]
    public void DisplayName_Blank_IsRejected(string displayName, string message)
    {
        ErrorsFor(Valid() with { DisplayName = displayName }, "DisplayName").ShouldBe([message]);
    }

    [Fact]
    public void DisplayName_LongerThan100AfterTrim_IsRejected()
    {
        ErrorsFor(Valid() with { DisplayName = new string('n', 101) }, "DisplayName")
            .ShouldBe(["Display name must be at most 100 characters."]);
    }

    [Fact]
    public void DisplayName_Of100PlusSurroundingSpaces_Passes()
    {
        ErrorsFor(Valid() with { DisplayName = $"  {new string('n', 100)}  " }, "DisplayName").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("", "Email is required.")]
    [InlineData("   ", "Email is required.")]
    [InlineData("ana", "Enter a valid email address.")]
    [InlineData("ana@", "Enter a valid email address.")]
    [InlineData("@example.com", "Enter a valid email address.")]
    [InlineData("ana@example", "Enter a valid email address.")]
    [InlineData("ana @example.com", "Enter a valid email address.")]
    public void Email_Invalid_IsRejected(string email, string message)
    {
        ErrorsFor(Valid() with { Email = email }, "Email").ShouldBe([message]);
    }

    [Fact]
    public void Email_LongerThan254AfterTrim_IsRejected()
    {
        var email = new string('a', 255 - "@x.io".Length) + "@x.io";

        ErrorsFor(Valid() with { Email = email }, "Email").ShouldBe(["Email must be at most 254 characters."]);
    }

    [Fact]
    public void Email_Of254PlusSurroundingSpaces_Passes()
    {
        var email = new string('a', 254 - "@x.io".Length) + "@x.io";

        ErrorsFor(Valid() with { Email = $"  {email}  " }, "Email").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Password_ShorterThan8_IsRejected(string password)
    {
        ErrorsFor(Valid() with { Password = password }, "Password")
            .ShouldBe(["Password must be between 8 and 128 characters."]);
    }

    [Fact]
    public void Password_LongerThan128_IsRejected()
    {
        ErrorsFor(Valid() with { Password = new string('p', 129) }, "Password")
            .ShouldBe(["Password must be between 8 and 128 characters."]);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Password_AtLengthBoundaries_Passes(int length)
    {
        ErrorsFor(Valid() with { Password = new string('p', length) }, "Password").ShouldBeEmpty();
    }

    [Fact]
    public void Password_OnlySpaces_IsRejected()
    {
        ErrorsFor(Valid() with { Password = new string(' ', 10) }, "Password")
            .ShouldBe(["Password cannot be only spaces."]);
    }

    [Fact]
    public void AllInvalidFields_AreReportedTogether()
    {
        var result = _validator.Validate(new RegisterUserCommand("", "ana", "short"));

        result.Errors.Select(error => error.PropertyName).Distinct()
            .ShouldBe(["DisplayName", "Email", "Password"], ignoreOrder: true);
    }

    private static RegisterUserCommand Valid() => new("Ana", "ana@example.com", "correct horse");

    private string[] ErrorsFor(RegisterUserCommand command, string propertyName) =>
        [.. _validator.Validate(command).Errors
            .Where(error => error.PropertyName == propertyName)
            .Select(error => error.ErrorMessage)];
}
