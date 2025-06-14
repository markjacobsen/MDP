namespace CFG2.MDP;

using System.Data.SQLite;
using System.Diagnostics;
using System.Text.RegularExpressions;

class MDPloadCSV
{
    private string tableName;
    private string csvFile;
    private readonly string runGuid = Guid.NewGuid().ToString();

    static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            MDPLib.Log("Usage: MDPloadCSV <full-path-to-csv>");
            return 1;
        }

        string csvPath = args[0];

        MDPloadCSV loader = new MDPloadCSV();
        return loader.Load(csvPath);
    }

    int Load(string file)
    {
        DateTime? begin = null;
        DateTime? end = null;
        string csvFile = file;
        string dbPath = MDPLib.GetSQLiteConnInfo("MDP");

        if (!File.Exists(csvFile))
        {
            MDPLib.Log($"CSV file not found: {csvFile}", this.runGuid);
            return 1;
        }
        if (!File.Exists(dbPath))
        {
            MDPLib.Log($"SQLite DB file not found: {dbPath}", this.runGuid);
            return 1;
        }

        string fileName = Path.GetFileName(csvFile);
        string dbName = Path.GetFileName(dbPath);
        tableName = Path.GetFileNameWithoutExtension(csvFile);

        bool lockAquired = Lock();
        if (!lockAquired)
        {
            MDPLib.Log("Terminating because unable to aquire lock", this.runGuid);
            return 0;
        }

        string startMsg = $"Loading {fileName} into table {tableName} in database {dbName}";
        MDPLib.Log(startMsg, this.runGuid);
        begin = DateTime.Now;

        Stopwatch sw = Stopwatch.StartNew();
        int rowCount = 0;
        bool success = false;

        // Read header and determine column types
        string[] headers;
        string[] types;
        try
        {
            using (var reader = new StreamReader(csvFile))
            {
                var headerLine = reader.ReadLine();
                if (headerLine == null)
                {
                    string msg = "CSV file is empty.";
                    MDPLib.Log(msg, this.runGuid);
                    return 1;
                }
                headers = ParseCsvLine(headerLine).ToArray();
                types = headers.Select(h =>
                    (h.EndsWith("_Q") || h.EndsWith("_NB") || h.EndsWith("_A")) ? "NUMERIC" : "TEXT"
                ).ToArray();
            }
        }
        catch (Exception ex)
        {
            string msg = $"Error reading CSV header: {ex.Message}";
            MDPLib.Log(msg, this.runGuid);
            return 1;
        }

        // Prepare SQL for dropping and creating table
        string dropTableSql = $"DROP TABLE IF EXISTS \"{tableName}\";";
        string createTableSql = $"CREATE TABLE \"{tableName}\" ({string.Join(", ", headers.Select((h, i) => $"\"{h}\" {types[i]}"))});";

        string loadResult = "";
        try
        {
            // Drop and create table
            int rc = RunSqliteCommand(dbPath, $"{dropTableSql} {createTableSql}", out string stdOut, out string stdErr);
            if (rc != 0)
            {
                string msg = $"Error creating table: {stdErr}";
                MDPLib.Log(msg, this.runGuid);
                return 1;
            }

            // Import CSV using sqlite3 .import
            // The .import command expects the first line to be the header
            //string importCmd = $".mode csv\n.import \"{csvPath.Replace("\"", "\"\"")}\" \"{tableName}\"";
            string absCsvPath = Path.GetFullPath(csvFile).Replace('\\', '/');
            string importCmd = $".timeout 600000\n.mode csv\n.import --skip 1 \"{absCsvPath}\" \"{tableName}\""; // attempt to wait up to 10 minutes to aquire a lock
            MDPLib.Log($"Running import to {tableName}: \n{importCmd}", this.runGuid);
            rc = RunSqliteImport(dbPath, importCmd, out stdOut, out stdErr);
            if (rc != 0 || !string.IsNullOrWhiteSpace(stdErr))
            {
                throw new Exception($"Import failed: {stdErr}");
            }

            // Count rows
            rc = RunSqliteCommand(dbPath, $"SELECT COUNT(*) FROM \"{tableName}\";", out stdOut, out stdErr);
            if (rc != 0)
            {
                throw new Exception($"Error counting rows: {stdErr}");
            }
            if (!int.TryParse(stdOut.Trim(), out rowCount))
            {
                rowCount = 0;
            }

            end = DateTime.Now;
            sw.Stop();
            loadResult = $"Wrote {rowCount} lines to {tableName} in {sw.Elapsed.Minutes} min {sw.Elapsed.Seconds} seconds from {csvFile}";

            // Rename input file to .done
            string doneFile = csvFile + ".done";
            if (File.Exists(doneFile))
                File.Delete(doneFile);
            File.Move(csvFile, doneFile);

            success = true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Drop table (intential so queries break)
            try
            {
                RunSqliteCommand(dbPath, $"DROP TABLE IF EXISTS \"{tableName}\";", out _, out _);
            }
            catch { /* ignore */ }

            loadResult = $"ERROR writing data: {ex.Message}. Table has been dropped. {sw.Elapsed.Minutes} min {sw.Elapsed.Seconds} seconds";

            // Rename input file to .err
            string errFile = csvFile + ".err";
            if (File.Exists(errFile))
                File.Delete(errFile);
            File.Move(csvFile, errFile);
        }

        LogLoad(dbPath, csvFile, tableName, loadResult, begin, end);

        return success ? 0 : 1;
    }

    // Helper to run sqlite3 command for normal SQL (not .import)
    int RunSqliteCommand(string dbPath, string sql, out string stdOut, out string stdErr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sqlite3",
            Arguments = $"\"{dbPath}\" \"{sql.Replace("\"", "\"\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        stdOut = proc.StandardOutput.ReadToEnd();
        stdErr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return proc.ExitCode;
    }

    // Helper to run sqlite3 command for .import (via stdin)
    int RunSqliteImport(string dbPath, string importCmd, out string stdOut, out string stdErr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sqlite3",
            Arguments = $"\"{dbPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        proc.StandardInput.WriteLine(importCmd);
        proc.StandardInput.Close();
        stdOut = proc.StandardOutput.ReadToEnd();
        stdErr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return proc.ExitCode;
    }

    // Simple CSV line parser (handles quoted fields)
    static IEnumerable<string> ParseCsvLine(string line)
    {
        if (line == null)
            yield break;
        var regex = new Regex("(?:^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)", RegexOptions.Compiled);
        foreach (Match match in regex.Matches(line))
        {
            string val = match.Value;
            if (val.StartsWith(","))
                val = val.Substring(1);
            if (val.StartsWith("\"") && val.EndsWith("\""))
                val = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
            yield return val;
        }
    }

    bool Lock()
    {
        return !MDPLib.IsMDPlocked(MDPLib.GetAppName(), 5);
    }

    void LogLoad(string dbPath, string file, string table, string debugMsg, DateTime? begin = null, DateTime? end = null)
    {
        MDPLib.Log(debugMsg, this.runGuid, true, true);

        // Update the connection string as needed
        string connectionString = "Data Source=" + dbPath + ";Version=3;";

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            // Insert the log record
            string insertSql = @"
                INSERT INTO MDP_LOAD (SRC_X, TABLE_X, DEBUG_X, BEGIN_TS, END_TS)
                VALUES (@file, @table, @debug, @begin, @end);
            ";

            using (var insertCmd = new SQLiteCommand(insertSql, connection))
            {
                insertCmd.Parameters.AddWithValue("@file", file);
                insertCmd.Parameters.AddWithValue("@table", table);
                insertCmd.Parameters.AddWithValue("@debug", debugMsg);
                insertCmd.Parameters.AddWithValue("@begin", begin);
                insertCmd.Parameters.AddWithValue("@end", end);

                insertCmd.ExecuteNonQuery();
            }
        }
    }
}