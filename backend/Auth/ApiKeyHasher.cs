using System.Security.Cryptography;
using System.Text;

namespace llmmo.Auth;

public static class ApiKeyHasher
{
    public static string Hash(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static (string ApiKey, string Prefix) GenerateKeyPair()
    {
        var prefix = $"llmmo_{Guid.NewGuid():N}"[..12];
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .TrimEnd('=');
        var apiKey = $"{prefix}_{secret}";
        return (apiKey, prefix);
    }
}
