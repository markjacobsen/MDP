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
        string unencryptedData = key + ".host=" + host + "\n" +
                                 key + ".port=" + port + "\n" +
                                 key + ".db=" + db + "\n";
        File.AppendAllText(configFile, unencryptedData);
        SecLib.Store(key + ".user", username);
        SecLib.Store(key + ".pass", password);
    }
}