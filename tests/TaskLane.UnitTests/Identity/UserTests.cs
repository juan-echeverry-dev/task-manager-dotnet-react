using TaskLane.Domain.Identity;

namespace TaskLane.UnitTests.Identity;

public sealed class UserTests
{
    private static readonly Guid Id = Guid.CreateVersion7();
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_TrimsAndNormalizes()
    {
        var user = User.Register(Id, "  Ana@Example.COM ", "  Ana  ", "hash", CreatedAt);

        user.Id.ShouldBe(Id);
        user.Email.ShouldBe("Ana@Example.COM");
        user.NormalizedEmail.ShouldBe("ana@example.com");
        user.DisplayName.ShouldBe("Ana");
        user.PasswordHash.ShouldBe("hash");
        user.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void NormalizeEmail_TrimsAndLowercasesInvariant()
    {
        User.NormalizeEmail(" ANA@Example.com ").ShouldBe("ana@example.com");
    }

    [Fact]
    public void Register_AcceptsMaximumLengths()
    {
        var email = new string('a', User.EmailMaxLength - "@x.io".Length) + "@x.io";
        var displayName = new string('n', User.DisplayNameMaxLength);

        var user = User.Register(Id, $" {email} ", $" {displayName} ", "hash", CreatedAt);

        user.Email.Length.ShouldBe(254);
        user.DisplayName.Length.ShouldBe(100);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankEmail_Throws(string email)
    {
        Should.Throw<ArgumentException>(() => User.Register(Id, email, "Ana", "hash", CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankDisplayName_Throws(string displayName)
    {
        Should.Throw<ArgumentException>(() => User.Register(Id, "ana@example.com", displayName, "hash", CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankPasswordHash_Throws(string passwordHash)
    {
        Should.Throw<ArgumentException>(() => User.Register(Id, "ana@example.com", "Ana", passwordHash, CreatedAt));
    }

    [Fact]
    public void Register_WithTooLongEmail_Throws()
    {
        var email = new string('a', 255 - "@x.io".Length) + "@x.io";

        Should.Throw<ArgumentException>(() => User.Register(Id, email, "Ana", "hash", CreatedAt));
    }

    [Fact]
    public void Register_WithTooLongDisplayName_Throws()
    {
        Should.Throw<ArgumentException>(
            () => User.Register(Id, "ana@example.com", new string('n', 101), "hash", CreatedAt));
    }

    [Fact]
    public void Register_WithNonUtcCreatedAt_Throws()
    {
        var local = new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.FromHours(-5));

        Should.Throw<ArgumentException>(() => User.Register(Id, "ana@example.com", "Ana", "hash", local));
    }

    [Fact]
    public void Register_WithEmptyId_Throws()
    {
        Should.Throw<ArgumentException>(() => User.Register(Guid.Empty, "ana@example.com", "Ana", "hash", CreatedAt));
    }
}
