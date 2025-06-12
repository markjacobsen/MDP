using System.Data.SQLite;
using System.Diagnostics;

namespace CFG2.MDP;

public class MDPLib
{
    static string GetAppName()
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
        return Path.Combine(GetLogDir(), GetAppName()+".log");
    }

    public static string GetMDP()
    {
        string location = Path.Combine(Environment.GetEnvironmentVariable("SYNC_DRIVE_HOME"), "MDP.db");
        if (!File.Exists(location))
        {
            // Create an empty MDP database if it does not exist
            SQLiteConnection.CreateFile(location);
            using (var connection = new SQLiteConnection("Data Source=" + location + ";Version=3;"))
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
            }
        }
        return location;
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
                string dbPath = GetMDP();
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
}
