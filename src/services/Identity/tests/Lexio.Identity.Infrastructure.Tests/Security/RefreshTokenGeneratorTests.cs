using Lexio.Identity.Infrastructure.Security;

namespace Lexio.Identity.Infrastructure.Tests.Security;

public class RefreshTokenGeneratorTests
{
    [Fact]
    public void Token_is_43_char_base64url_unpadded()
    {
        var token = RefreshTokenGenerator.NewRawToken();
        token.Should().HaveLength(43);
        token.Should().NotContain("=");
        token.Should().NotContain("+");
        token.Should().NotContain("/");
    }

    [Fact]
    public void Tokens_are_unique_across_invocations()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            seen.Add(RefreshTokenGenerator.NewRawToken());
        }
        seen.Should().HaveCount(100);
    }
}
