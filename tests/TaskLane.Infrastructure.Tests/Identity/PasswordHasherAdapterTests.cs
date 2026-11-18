using TaskLane.Infrastructure.Identity;

namespace TaskLane.Infrastructure.Tests.Identity;

public sealed class PasswordHasherAdapterTests
{
    private const string Password = "correct horse battery";

    private readonly PasswordHasherAdapter _hasher = new();

    [Fact]
    public void Hash_NeverContainsThePassword()
    {
        var hash = _hasher.Hash(Password);

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.ShouldNotContain(Password);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        _hasher.Hash(Password).ShouldNotBe(_hasher.Hash(Password));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        _hasher.Verify(_hasher.Hash(Password), Password).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        _hasher.Verify(_hasher.Hash(Password), "wrong password").ShouldBeFalse();
    }
}
