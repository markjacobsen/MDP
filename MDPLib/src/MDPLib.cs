using System.Data.SQLite;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace CFG2.MDP;

public class MDPLib
{
    private static readonly string defaultMDPfile = Path.Combine(Environment.GetEnvironmentVariable("SYNC_DRIVE_HOME"), "MDP.db");

    internal static string GetConnFile()
    {
        string path = Path.Combine(Environment.GetEnvironmentVariable("SYNC_DRIVE_HOME"), @"Apps\CFG2\MDP\db.properties");
        if (!File.Exists(path))
        {
            // Create a default db.properties file
            File.WriteAllText(path, "# Do NOT delete the MDP.file entry\n" +
                                    "MDP.file=" + defaultMDPfile + "\n" +
                                    "\n" +
                                    "# The rest are just example entries...\n" +
                                    "\n" +
                                    "# Example DB2 LUW\n" +
                                    "MYDB2.host=host132\n" +
                                    "MYDB2.port=46000\n" +
                                    "MYDB2.db=MYDB2\n" +
                                    "\n" +
                                    "# Example Dynamics 365 Dataverse\n" +
                                    "D365D.server=our-d365-dev.crm.dynamics.com\n" +
                                    "\n" +
                                    "# Example Azure SQL DB\n" +
                                    "MYASDB.server=mdb-sql.database.windows.net\n" +
                                    "MYASDB.db=MY_DB\n");
        }

        return path;
    }

    private static string CreateDefaultMDPifNeeded()
    {
        if (!File.Exists(defaultMDPfile))
        {
            Log($"Creating default MDP: {defaultMDPfile}");

            // Create an empty MDP database if it does not exist
            SQLiteConnection.CreateFile(defaultMDPfile);
            using (var connection = new SQLiteConnection("Data Source=" + defaultMDPfile + ";Version=3;"))
            {
                connection.Open();
                // Create the AUD_LOG table
                string createTableSql = @"CREATE TABLE IF NOT EXISTS MDP_LOG (SRC_X TEXT, GROUP_C TEXT, LOG_X TEXT, CREATED_TS TIMESTAMP DEFAULT CURRENT_TIMESTAMP);";
                using (var command = new SQLiteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                createTableSql = @"CREATE TABLE IF NOT EXISTS MDP_LOAD (SRC_X TEXT, TABLE_X TEXT, DEBUG_X TEXT, BEGIN_TS TIMESTAMP, END_TS TIMESTAMP, CREATED_TS TIMESTAMP DEFAULT CURRENT_TIMESTAMP);";
                using (var command = new SQLiteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                createTableSql = @"CREATE TABLE IF NOT EXISTS MDP_MISC_VALUE (VALUE_C TEXT PRIMARY KEY, VALUE_X TEXT, VALUE_NB NUMERIC, VALUE_TS TIMESTAMP, CREATED_TS TIMESTAMP DEFAULT CURRENT_TIMESTAMP, MODIFIED_TS TIMESTAMP DEFAULT CURRENT_TIMESTAMP);";
                using (var command = new SQLiteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        
        return defaultMDPfile;
    }

    public static string GetAppName()
    {
        return Process.GetCurrentProcess().ProcessName;
    }

    public static string GetAppDir()
    {
        string dir = Path.Combine(Environment.GetEnvironmentVariable("SYNC_DRIVE_HOME"), @"Apps");
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

        dir = Path.Combine(dir, @"CFG2");
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

        dir = Path.Combine(dir, @"MDP");
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

        return dir;
    }

    public static string GetLogDir()
    {
        string dir = Path.Combine(GetAppDir(), @"Logs");
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
        return dir;
    }

    public static string GetLogFile()
    {
        return Path.Combine(GetLogDir(), GetAppName() + ".log");
    }

    public static void Log(string message, string group = null, bool toFile = true, bool toMDP = false)
    {
        string groupPrefix = string.IsNullOrEmpty(group) ? "" : $"[{group}] ";

        if (string.IsNullOrEmpty(group))
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"{groupPrefix}{message}");
        }

        if (toFile)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logLine = $"{timestamp} - {groupPrefix}{message}";
            try
            {
                File.AppendAllText(GetLogFile(), logLine + Environment.NewLine);
            }
            catch { /* Ignore to not block main process */ }
        }

        if (toMDP)
        {
            try
            {
                string dbPath = GetSQLiteConnInfo("MDP");
                string connectionString = "Data Source=" + dbPath + ";Version=3;";

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Insert the log record
                    string insertSql = @"INSERT INTO MDP_LOG (SRC_X, GROUP_C, LOG_X, CREATED_TS) VALUES (@src, @group, @logMsg, @createdTs);";

                    using (var insertCmd = new SQLiteCommand(insertSql, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@src", GetAppName());
                        insertCmd.Parameters.AddWithValue("@group", group);
                        insertCmd.Parameters.AddWithValue("@logMsg", message);
                        insertCmd.Parameters.AddWithValue("@createdTs", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* Ignore to not block main process */ }
        }
    }

    public static string GetSqlFromFile(string fullFilePath)
    {
        Log($"Reading SQL from {fullFilePath}");
        string[] lines = File.ReadAllLines(fullFilePath);
        var queryLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line =>
                !line.TrimStart().StartsWith("--") &&
                !line.TrimStart().StartsWith("#") &&
                !line.TrimStart().StartsWith("//"))
            .ToArray();

        return string.Join(Environment.NewLine, queryLines);
    }

    public static string EscapeCsvValue(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        bool mustQuote = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"' || c == ',' || c == '\n' || c == '\r')
            {
                mustQuote = true;
                break;
            }
        }

        if (!mustQuote)
            return s;

        // Use StringBuilder for efficiency
        var sb = new System.Text.StringBuilder();
        sb.Append('"');
        foreach (char c in s)
        {
            if (c == '"')
                sb.Append("\"\"");
            else
                sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }

    public static bool Lock(string name, int timeoutInMinutes = 5)
    {
        DateTime start = DateTime.Now;
        while (!SaveMiscValue("MDP_LOCK", "Locked By: " + name, null, DateTime.Now))
        {
            TimeSpan difference = DateTime.Now - start;
            if (difference.TotalMinutes >= timeoutInMinutes)
            {
                Log("MDP is locked by another process. Timeout reached.");
                return false;
            }
            else
            {
                Log("MDP is locked by another process. Waiting for 30 seconds before retrying...");
            }
            Thread.Sleep(30000);
        }
        return true;
    }

    public static bool Unlock(string name, int timeoutInMinutes = 5)
    {
        DateTime start = DateTime.Now;
        while (!SaveMiscValue("MDP_LOCK", "Unlocked By: " + name, null, DateTime.Now))
        {
            TimeSpan difference = DateTime.Now - start;
            if (difference.TotalMinutes >= timeoutInMinutes)
            {
                Log("MDP is locked by another process. Timeout reached.");
                return false;
            }
            else
            {
                Log("MDP is locked by another process. Waiting for 30 seconds before retrying...");
            }
            Thread.Sleep(30000);
        }
        return true;
    }

    public static bool SaveMiscValue(string code, string valString, double? number = null, DateTime? timestamp = null)
    {
        bool success = false;
        try
        {
            string dbPath = GetSQLiteConnInfo("MDP");
            string connectionString = "Data Source=" + dbPath + ";Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Insert or update the misc value
                string upsertSql = @"
                    INSERT INTO MDP_MISC_VALUE (VALUE_C, VALUE_X, VALUE_NB, VALUE_TS)
                    VALUES (@code, @valString, @number, @timestamp)
                    ON CONFLICT(VALUE_C) DO UPDATE SET
                        VALUE_X = @valString,
                        VALUE_NB = @number,
                        VALUE_TS = @timestamp,
                        MODIFIED_TS = CURRENT_TIMESTAMP;";

                using (var command = new SQLiteCommand(upsertSql, connection))
                {
                    command.Parameters.AddWithValue("@code", code);
                    command.Parameters.AddWithValue("@valString", valString);
                    command.Parameters.AddWithValue("@number", number.HasValue ? (object)number.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@timestamp", timestamp.HasValue ? (object)timestamp.Value : DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }

            success = true;
        }
        catch (Exception ex)
        {
            Log("Failed to save misc value: " + code + ": " + ex.Message);
            success = false;
        }

        return success;
    }

    public static string GetSQLiteConnInfo(string connKey)
    {
        string propertiesFilePath = GetConnFile();
        string? file = null;

        foreach (var line in File.ReadLines(propertiesFilePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(connKey + ".file="))
                file = trimmed.Substring((connKey + ".file=").Length).Trim();
        }

        if (connKey.Equals("MDP") && !File.Exists(file))
        {
            file = CreateDefaultMDPifNeeded();
        }

        if (string.IsNullOrEmpty(file))
        {
            throw new Exception("Missing connection information for " + connKey + " in " + propertiesFilePath);
        }

        return file;
    }

    public static (string server, string db) GetAzureSqlDBConnInfo(string connKey)
    {
        string propertiesFilePath = GetConnFile();
        string server = null, db = null;

        foreach (var line in File.ReadLines(propertiesFilePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(connKey + ".server="))
                server = trimmed.Substring((connKey + ".server=").Length).Trim();
            else if (trimmed.StartsWith(connKey + ".db="))
                db = trimmed.Substring((connKey + ".db=").Length).Trim();
        }

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(db))
            throw new Exception("Missing connection information in db.properties.");

        return (server, db);
    }

    public static string GetDataverseConnInfo(string connKey)
    {
        string propertiesFilePath = GetConnFile();
        string server = null;

        Console.WriteLine("Getting connection for " + connKey + " from " + propertiesFilePath);

        foreach (var line in File.ReadLines(propertiesFilePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(connKey + ".server="))
                server = trimmed.Substring((connKey + ".server=").Length).Trim();
        }

        if (string.IsNullOrEmpty(server))
            throw new Exception("Missing connection information in db.properties.");

        return server;
    }
    
    public static (string host, string port, string db) GetDB2ConnInfo(string connection)
    {
        string propertiesFilePath = GetConnFile();
        string host = null, port = null, db = null;

        foreach (var line in File.ReadLines(propertiesFilePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(connection+".host="))
                host = trimmed.Substring((connection+".host=").Length).Trim();
            else if (trimmed.StartsWith(connection+".port="))
                port = trimmed.Substring((connection+".port=").Length).Trim();
            else if (trimmed.StartsWith(connection+".db="))
                db = trimmed.Substring((connection+".db=").Length).Trim();
        }

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(db))
            throw new Exception("Missing connection information in db.properties.");

        return (host, port, db);
    }
}
