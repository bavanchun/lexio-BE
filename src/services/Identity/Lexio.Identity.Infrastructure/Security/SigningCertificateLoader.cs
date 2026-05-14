using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;

namespace Lexio.Identity.Infrastructure.Security;

/// <summary>
/// Resolves the X.509 signing certificate used for RS256 JWT issuance.
/// Development: in-memory ephemeral RSA cert (regenerated each run).
/// Other envs: PEM at <c>OPENIDDICT_SIGNING_CERT_PATH</c>; missing or unreadable fails fast.
/// </summary>
public sealed class SigningCertificateLoader
{
    public const string CertPathEnvVar = "OPENIDDICT_SIGNING_CERT_PATH";

    private readonly Lazy<X509Certificate2> _cert;

    public SigningCertificateLoader(IHostEnvironment env)
    {
        _cert = new Lazy<X509Certificate2>(() =>
            env.IsDevelopment() ? CreateEphemeral() : LoadFromEnv());
    }

    public X509Certificate2 Certificate => _cert.Value;

    private static X509Certificate2 CreateEphemeral()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=lexio-identity-dev",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));
    }

    private static X509Certificate2 LoadFromEnv()
    {
        var path = Environment.GetEnvironmentVariable(CertPathEnvVar)
            ?? throw new InvalidOperationException(
                $"Signing certificate path not set in env var '{CertPathEnvVar}'.");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Signing certificate not found at '{path}'.", path);
        }

        var keyPath = Environment.GetEnvironmentVariable("OPENIDDICT_SIGNING_KEY_PATH");
        return keyPath is { Length: > 0 } && File.Exists(keyPath)
            ? X509Certificate2.CreateFromPemFile(path, keyPath)
            : X509Certificate2.CreateFromPemFile(path);
    }
}
