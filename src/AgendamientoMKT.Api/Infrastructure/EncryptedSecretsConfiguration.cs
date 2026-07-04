using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgendamientoMKT.Api.Infrastructure;

public static class EncryptedSecretsConfiguration
{
    private const string MasterKeyVariable = "AGENDAMIENTO_MASTER_KEY";

    public static void AddEncryptedSecrets(this ConfigurationManager configuration, string contentRoot)
    {
        var configuredPath = Environment.GetEnvironmentVariable("ENCRYPTED_SECRETS_FILE") ?? configuration["EncryptedSecrets:File"] ?? "config/secrets.aes256.json";
        var path = Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(contentRoot, configuredPath);
        var required = configuration.GetValue("EncryptedSecrets:Required", false);
        if (!File.Exists(path))
        {
            if (required) throw new InvalidOperationException($"Encrypted secrets file was not found at '{path}'.");
            return;
        }

        var encodedKey = Environment.GetEnvironmentVariable(MasterKeyVariable);
        if (string.IsNullOrWhiteSpace(encodedKey)) throw new InvalidOperationException($"{MasterKeyVariable} is required to decrypt application secrets.");
        byte[] key;
        try { key = Convert.FromBase64String(encodedKey); }
        catch (FormatException exception) { throw new InvalidOperationException($"{MasterKeyVariable} must be a Base64-encoded 256-bit key.", exception); }
        if (key.Length != 32) throw new InvalidOperationException($"{MasterKeyVariable} must decode to exactly 32 bytes.");

        byte[]? plaintext = null;
        try
        {
            plaintext = Decrypt(File.ReadAllBytes(path), key);
            configuration.AddJsonStream(new MemoryStream(plaintext, writable: false));
            configuration.AddEnvironmentVariables();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key)
    {
        ValidateKey(key); var nonce = RandomNumberGenerator.GetBytes(12); var ciphertext = new byte[plaintext.Length]; var tag = new byte[16];
        using var aes = new AesGcm(key, tag.Length); aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return JsonSerializer.SerializeToUtf8Bytes(new Envelope(1, "AES-256-GCM", Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(ciphertext)));
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> envelopeBytes, ReadOnlySpan<byte> key)
    {
        ValidateKey(key); var envelope = JsonSerializer.Deserialize<Envelope>(envelopeBytes) ?? throw new InvalidDataException("Encrypted secrets envelope is invalid.");
        if (envelope.Version != 1 || !string.Equals(envelope.Algorithm, "AES-256-GCM", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported encrypted secrets format.");
        var nonce = Convert.FromBase64String(envelope.Nonce); var tag = Convert.FromBase64String(envelope.Tag); var ciphertext = Convert.FromBase64String(envelope.Ciphertext); var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, tag.Length); aes.Decrypt(nonce, ciphertext, tag, plaintext); return plaintext;
    }

    private static void ValidateKey(ReadOnlySpan<byte> key) { if (key.Length != 32) throw new ArgumentException("AES-256 requires a 32-byte key.", nameof(key)); }
    private sealed record Envelope(
        [property: JsonPropertyName("version")] int Version,
        [property: JsonPropertyName("algorithm")] string Algorithm,
        [property: JsonPropertyName("nonce")] string Nonce,
        [property: JsonPropertyName("tag")] string Tag,
        [property: JsonPropertyName("ciphertext")] string Ciphertext);
}
