using System.Text;
using System.Security.Cryptography;

namespace CFG2.MDP;

public class SecLib
{
    // Optional entropy for additional security. Should be the same for both encryption and decryption.
    private static readonly byte[] entropy = Encoding.UTF8.GetBytes("This!$someS3riouSenTropica+i0n");

    public static bool Store(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new Exception("Key cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(value))
        {
            throw new Exception("Value cannot be null or empty.");
        }

        string filePath = MDPLib.GetConnFile();if (!File.Exists(filePath))
        {
            throw new Exception($"Configuration file not found: {filePath}");
        }

        string encryptedValue = Encrypt(value);
        string lineToWrite = $"{key}={encryptedValue}";

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            bool keyFound = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith($"{key}="))
                {
                    lines[i] = lineToWrite;
                    keyFound = true;
                    break;
                }
            }

            if (!keyFound)
            {
                File.AppendAllText(filePath, lineToWrite + Environment.NewLine);
            }
            else
            {
                File.WriteAllLines(filePath, lines);
            }
            return true;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error storing data: {ex.Message}");
            return false;
        }
        catch (InvalidOperationException) // Propagated from Encrypt
        {
            return false;
        }
    }


    public static string Retrieve(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new Exception("key cannot be null or empty");
        }

        string filePath = MDPLib.GetConnFile();
        if (!File.Exists(filePath))
        {
            throw new Exception($"Configuration file not found: {filePath}");
        }

        try
        {
            foreach (string line in File.ReadLines(filePath))
            {
                if (line.StartsWith($"{key}="))
                {
                    string encryptedValue = line.Substring(key.Length + 1);
                    return Decrypt(encryptedValue);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error retrieving data: {ex.Message}");
        }
        catch (InvalidOperationException) // Propagated from Decrypt
        {
            // Decryption failed, likely due to corrupted data or wrong user/machine
        }
        catch (ArgumentException) // Propagated from Decrypt (invalid Base64)
        {
            // Input was not valid Base64
        }
        return null; // Key not found or an error occurred
    }

    private static string Encrypt(string decryptedValue)
    {
        if (string.IsNullOrEmpty(decryptedValue))
        {
            throw new ArgumentNullException(nameof(decryptedValue), "Password cannot be null or empty.");
        }

        try
        {
            byte[] decryptedBytes = Encoding.UTF8.GetBytes(decryptedValue);
            byte[] encryptedBytes = ProtectedData.Protect(decryptedBytes, entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (CryptographicException ex)
        {
            // Log the exception (e.g., using a logging framework)
            Console.WriteLine($"Encryption error: {ex.Message}");
            throw new InvalidOperationException("Failed to encrypt data.", ex);
        }
    }

    private static string Decrypt(string encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
        {
            return string.Empty;
        }

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedValue);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException ex)
        {
            // Log the exception
            Console.WriteLine($"Decryption error: {ex.Message}");
            throw new InvalidOperationException("Failed to decrypt data. Data might be corrupted or encrypted by a different user/machine.", ex);
        }
        catch (FormatException ex)
        {
            // Log the exception if the input is not a valid Base64 string
            Console.WriteLine($"Decryption error: Invalid Base64 string. {ex.Message}");
            throw new ArgumentException("Input string is not a valid Base64 format.", ex);
        }
    }
}