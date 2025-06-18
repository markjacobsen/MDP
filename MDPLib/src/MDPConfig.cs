namespace CFG2.MDP;

public class MDPConfig
{
    private static string configFile = MDPLib.GetConnFile();

    public static bool KeyExists(string key)
    {
        foreach (string line in File.ReadLines(configFile))
        {
            if (line.StartsWith($"{key}."))
            {
                return true;
            }
        }

        return false;
    }

    public static void RemoveKeyEntries(string key)
    {
        List<string> newLines = new List<string>();

        string[] lines = File.ReadAllLines(configFile);

        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith($"{key}."))
            {
                newLines.Add(lines[i]);
            }
        }

        File.WriteAllLines(configFile, newLines);
    }

    public static void StoreDb2(string key, string host, string port, string db, string username, string password)
    {
        string unencryptedData = "# DB2\n" +
                                key + ".host=" + host + "\n" +
                                key + ".port=" + port + "\n" +
                                key + ".db=" + db + "\n";
        File.AppendAllText(configFile, unencryptedData);
        SecLib.Store(key + ".user", username);
        SecLib.Store(key + ".pass", password);
        File.AppendAllText(configFile, "\n");
    }

    public static void StoreAzureSqlDB(string key, string server, string db)
    {
        string unencryptedData = "# Azure SQL DB\n" +
                                key + ".server=" + server + "\n" +
                                key + ".db=" + db + "\n\n";
        File.AppendAllText(configFile, unencryptedData);
    }

    public static void StoreSQLite(string key, string file)
    {
        string unencryptedData = "# SQLite\n" +
                                key + ".file=" + file + "\n\n";
        File.AppendAllText(configFile, unencryptedData);
    }

    public static void StoreDataverse(string key, string server)
    {
        string unencryptedData = "# Dataverse\n" +
                                key + ".server=" + server + "\n\n";
        File.AppendAllText(configFile, unencryptedData);
    }

    public static List<string> GetKeys()
    {
        string filePath = MDPLib.GetConnFile();

        // Use a HashSet for efficient storage and automatic handling of uniqueness.
        HashSet<string> uniqueKeys = new HashSet<string>();

        try
        {
            // Read all lines from the file. File.ReadLines is more memory-efficient for large files
            // as it reads line by line, rather than loading the entire file into memory.
            foreach (string line in File.ReadLines(filePath))
            {
                // Check if the line matches the "KEY.name=value" format.
                // It must contain a '.' and an '='.
                int dotIndex = line.IndexOf('.');
                int equalsIndex = line.IndexOf('=');

                if (dotIndex > 0 && equalsIndex > dotIndex)
                {
                    // Extract the part before the first '.' as the KEY.
                    string key = line.Substring(0, dotIndex);
                    uniqueKeys.Add(key);
                }
            }
        }
        catch (Exception ex)
        {
            MDPLib.Log($"ERROR: {ex.Message}");
        }

        return uniqueKeys.ToList();
    }

    public static string GetKeyDisplay(string key)
    {
        string filePath = MDPLib.GetConnFile();
        string ret = "";

        try
        {
            foreach (string line in File.ReadLines(filePath))
            {
                // Check if the line matches the "KEY.name=value" format.
                // It must contain a '.' and an '='.
                int dotIndex = line.IndexOf('.');
                int equalsIndex = line.IndexOf('=');

                if (dotIndex > 0 && (equalsIndex > dotIndex) && line.StartsWith(key))
                {
                    if (line.StartsWith(key + ".user="))
                    {
                        string val = SecLib.Retrieve(line.Split("=")[0]);
                        ret += key + ".user=" + val + "\n";
                    }
                    else if (line.StartsWith(key + ".pass="))
                    {
                        ret += key + ".pass=<hidden>\n";
                    }
                    else
                    {
                        ret += line + "\n";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MDPLib.Log($"ERROR: {ex.Message}");
        }

        return ret;
    }
}