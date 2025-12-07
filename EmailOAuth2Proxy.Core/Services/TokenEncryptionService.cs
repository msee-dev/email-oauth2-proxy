using System.Security.Cryptography;
using System.Text;

namespace EmailOAuth2Proxy.Core.Services;

/// <summary>
/// Service for encrypting and decrypting OAuth 2.0 tokens
/// </summary>
public class TokenEncryptionService
{
    /// <summary>
    /// Encrypt a value using a password
    /// </summary>
    public EncryptedValue Encrypt(string value, string password)
    {
        // Generate a random salt
        var salt = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Derive encryption key from password using PBKDF2
        const int iterations = 100000;
        var key = DeriveKey(password, salt, iterations);

        // Generate IV
        var iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        // Encrypt the value
        byte[] encryptedBytes;
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var valueBytes = Encoding.UTF8.GetBytes(value);
            encryptedBytes = encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length);
        }

        return new EncryptedValue
        {
            Salt = Convert.ToBase64String(salt),
            Iterations = iterations,
            IV = Convert.ToBase64String(iv),
            CipherText = Convert.ToBase64String(encryptedBytes)
        };
    }

    /// <summary>
    /// Decrypt a value using a password
    /// </summary>
    public string? Decrypt(EncryptedValue encryptedValue, string password)
    {
        try
        {
            var salt = Convert.FromBase64String(encryptedValue.Salt);
            var iv = Convert.FromBase64String(encryptedValue.IV);
            var cipherText = Convert.FromBase64String(encryptedValue.CipherText);

            // Derive the same key from password and salt
            var key = DeriveKey(password, salt, encryptedValue.Iterations);

            // Decrypt the value
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Derive an encryption key from password and salt using PBKDF2
    /// </summary>
    private byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);
        
        return pbkdf2.GetBytes(32); // 256-bit key for AES-256
    }
}

/// <summary>
/// Represents an encrypted value with metadata
/// </summary>
public class EncryptedValue
{
    public string Salt { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public string IV { get; set; } = string.Empty;
    public string CipherText { get; set; } = string.Empty;
}
