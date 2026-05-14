using Lexio.Identity.Infrastructure.Security;

namespace Lexio.Identity.Infrastructure.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_Verify_roundtrips()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");
        _hasher.Verify("correct-horse-battery-staple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var hash = _hasher.Hash("right");
        _hasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-bcrypt-hash")]
    public void Verify_returns_false_for_missing_or_malformed_hash(string? hash)
    {
        _hasher.Verify("anything", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_returns_false_for_missing_plaintext()
    {
        var hash = _hasher.Hash("x");
        _hasher.Verify(null, hash).Should().BeFalse();
        _hasher.Verify("", hash).Should().BeFalse();
    }
}
