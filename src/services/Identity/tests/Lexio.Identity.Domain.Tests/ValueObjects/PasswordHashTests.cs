using Lexio.Identity.Domain.ValueObjects;

namespace Lexio.Identity.Domain.Tests.ValueObjects;

public class PasswordHashTests
{
    [Theory]
    [InlineData("$2a$12$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234")]
    [InlineData("$2b$10$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234")]
    [InlineData("$2y$10$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234")]
    public void Create_accepts_valid_bcrypt_prefixes(string hash)
    {
        var result = PasswordHash.Create(hash);
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(hash);
    }

    [Theory]
    [InlineData("plaintext-password")]
    [InlineData("$1$abc$def")]              // md5-crypt
    [InlineData("$argon2id$v=19$...")]      // argon2
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_anything_not_bcrypt(string? hash)
    {
        var result = PasswordHash.Create(hash);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToString_does_not_leak_hash_material()
    {
        var hash = PasswordHash.From("$2a$12$abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmno1234");
        hash.ToString().Should().Be("[redacted password hash]");
    }
}
