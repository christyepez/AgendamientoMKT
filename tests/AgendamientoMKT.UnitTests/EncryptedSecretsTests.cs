using System.Security.Cryptography;
using System.Text;
using AgendamientoMKT.Api.Infrastructure;
using Xunit;

namespace AgendamientoMKT.UnitTests;

public sealed class EncryptedSecretsTests
{
    [Fact]
    public void EncryptAndDecrypt_WithAes256Gcm_RestoresOriginalConfiguration()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("{\"Jwt\":{\"Key\":\"sensitive-value\"}}");

        var envelope = EncryptedSecretsConfiguration.Encrypt(plaintext, key);
        var decrypted = EncryptedSecretsConfiguration.Decrypt(envelope, key);

        Assert.Equal(plaintext, decrypted);
        Assert.DoesNotContain("sensitive-value", Encoding.UTF8.GetString(envelope), StringComparison.Ordinal);
    }

    [Fact]
    public void Decrypt_WhenCiphertextWasModified_RejectsFile()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var envelope = EncryptedSecretsConfiguration.Encrypt("secret"u8, key);
        envelope[^3] ^= 1;

        Assert.ThrowsAny<Exception>(() => EncryptedSecretsConfiguration.Decrypt(envelope, key));
    }
}
