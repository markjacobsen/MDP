using System.Text;
using System.Security.Cryptography;

namespace CFG2.MDP;

public class SecLib
{
    // Optional entropy for additional security. Should be the same for both encryption and decryption.
    private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("This!$someS3riouSenTropica+i0n");

    public static bool Store(string key, string value)
    {
        bool success = false;
        string curVal = GetValue(key);

        return success;
    }

    public static string Retrieve(string key)
    {
        return Decrypt(Encoding.UTF8.GetBytes(GetValue(key)));
    }

    private static string GetValue(string key)
    {
        string propertiesFilePath = MDPLib.GetConnFile();
        string val = null;

        foreach (var line in File.ReadLines(propertiesFilePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(key + "="))
                val = trimmed.Substring((key + "=").Length).Trim();
        }

        if (string.IsNullOrEmpty(val))
            throw new Exception($"Missing key \"{key}\" in db.properties.");

        return val;
    }

    /// <summary>
    /// Protects a value using DPAPI with User scope.
    /// </summary>
    /// <param name="plainTextValue">The string to encrypt.</param>
    /// <returns>A byte array representing the encrypted value, or null if protection fails.</returns>
    private static byte[] Encrypt(string plainTextValue)
    {
        if (string.IsNullOrEmpty(plainTextValue))
        {
            throw new ArgumentNullException(nameof(plainTextValue), "Password cannot be null or empty.");
        }

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainTextValue);

            // Encrypt the data using DPAPI.
            // DataProtectionScope.CurrentUser ensures that only the current user can decrypt it.
            // s_entropy is optional but highly recommended for an extra layer of security.
            byte[] protectedBytes = ProtectedData.Protect(
                plainBytes,
                s_entropy, // Use the same entropy as for decryption
                DataProtectionScope.CurrentUser);

            return protectedBytes;
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Error protecting password: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Decrypts a value previously protected with DPAPI.
    /// </summary>
    /// <param name="protectedBytes">The byte array representing the protected password.</param>
    /// <returns>The original plain text value, or null if retrieval fails.</returns>
    private static string Decrypt(byte[] protectedBytes)
    {
        if (protectedBytes == null || protectedBytes.Length == 0)
        {
            throw new ArgumentNullException(nameof(protectedBytes), "Protected data cannot be null or empty.");
        }

        try
        {
            // Decrypt the data using DPAPI.
            // You must use the same scope (CurrentUser) and entropy used during encryption.
            byte[] plainBytes = ProtectedData.Unprotect(
                protectedBytes,
                s_entropy, // Use the same entropy as for encryption
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Error decrypting: {ex.Message}");
            Console.WriteLine("This can happen if:");
            Console.WriteLine("  - You are trying to decrypt data encrypted by a different user.");
            Console.WriteLine("  - The entropy string used for decryption does not match the one used for encryption.");
            return null;
        }
    }
}