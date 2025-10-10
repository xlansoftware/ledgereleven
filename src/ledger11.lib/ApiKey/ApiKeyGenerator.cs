using System.Security.Cryptography;

public static class ApiKeyGenerator
{
    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256-bit key
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')      // remove padding
            .Replace('+', '-') // URL-safe replacements
            .Replace('/', '_');
    }

    public static string ComputeHash(string apiKey)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes);
    }
}
