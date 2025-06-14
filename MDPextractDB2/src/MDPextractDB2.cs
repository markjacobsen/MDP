namespace CFG2.MDP;

using System.Text;
using IBM.Data.Db2;

class MDPextractDB2
{
    private readonly string runGuid = Guid.NewGuid().ToString();

    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            MDPLib.Log("Usage: MDPextractDB2 <path-to-sql-file> <connKey> <username> <password>");
            return;
        }

        string sqlFilePath = args[0];
        string connKey = args[1];
        string db2Username = args[2];
        string db2Password = args[3];

        MDPextractDB2 app = new MDPextractDB2();
        app.Export(sqlFilePath, connKey, db2Username, db2Password);
    }

    void Export(string sqlFilePath, string connKey, string db2Username, string db2Password)
    {
        if (!File.Exists(sqlFilePath))
        {
            MDPLib.Log($"File not found: {sqlFilePath}", this.runGuid);
            return;
        }

        string logPath = Path.Combine(
            Path.GetDirectoryName(sqlFilePath),
            Path.GetFileNameWithoutExtension(sqlFilePath) + ".log"
        );

        MDPLib.Log("Reading file: " + sqlFilePath, this.runGuid);

        // Read file and extract connection info and SQL
        var (host, port, db) = MDPLib.GetDB2ConnInfo(connKey);

        StringBuilder sqlBuilder = new StringBuilder();

        foreach (var line in File.ReadLines(sqlFilePath))
        {
            if (!line.TrimStart().StartsWith("--"))
                sqlBuilder.AppendLine(line);
        }

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(db))
        {
            MDPLib.Log("Missing connection information.", this.runGuid);
            return;
        }

        string sql = sqlBuilder.ToString().Trim();
        if (string.IsNullOrEmpty(sql))
        {
            MDPLib.Log("No SQL query found in the file.", this.runGuid);
            return;
        }

        string csvFilePath = Path.Combine(
            Path.GetDirectoryName(sqlFilePath),
            Path.GetFileNameWithoutExtension(sqlFilePath) + ".csv"
        );

        int records = 0;
        try
        {
            string connStr = $"Server={host}:{port};Database={db};UID={db2Username};PWD={db2Password};";
            MDPLib.Log($"Connecting to {db} on {host}:{port}", this.runGuid);
            
            using (var conn = new DB2Connection(connStr))
            {
                MDPLib.Log("Executing SQL: \n" + sql, this.runGuid);
                conn.Open();
                using (var cmd = new DB2Command(sql, conn))
                {
                    cmd.CommandTimeout = 600; // 10minutes
                    MDPLib.Log("Starting: "+DateTime.Now.ToString("HH:mm:ss"));
                    using (var reader = cmd.ExecuteReader())
                    //using (var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))  // false overwrites the CSV file if it already exists
                    using (var fileStream = new FileStream(csvFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8, 65536))
                    {
                        // Write header
                        MDPLib.Log("Writing header to: " + csvFilePath, this.runGuid);
                        string[] columnNames = new string[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                            columnNames[i] = reader.GetName(i);
                        writer.WriteLine(string.Join(",", Array.ConvertAll(columnNames, CsvEscape)));

                        // Write rows
                        MDPLib.Log("Writing results to: " + csvFilePath, this.runGuid);
                        string[] fields = new string[reader.FieldCount]; // Avoid repeated definition
                        var sb = new StringBuilder(1024);
                        while (reader.Read())
                        {
                            sb.Clear();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (i > 0) sb.Append(',');
                                object value = reader.GetValue(i);
                                sb.Append(CsvEscape(value?.ToString()));
                            }
                            writer.WriteLine(sb.ToString());
                            records++;

                            if (records % 10000 == 0)
                            {
                                writer.Flush();
                                MDPLib.Log(records + " records " + DateTime.Now.ToString("HH:mm:ss"));
                            }
                        }
                        writer.Flush();
                    }
                }
            }
            
            MDPLib.Log("Wrote " + records + " lines of data to " + csvFilePath, this.runGuid, true, true);
        }
        catch (Exception ex)
        {
            if (File.Exists(csvFilePath))
            {
                File.Delete(csvFilePath);
            }
            MDPLib.Log("ERROR executing on rec "+records+" and extract file deleted: "+ex.Message, this.runGuid, true, true);
        }
    }

    void AppendToCommandScript(string command, string scriptFile)
    {
        File.AppendAllText(scriptFile, "db2 "+command + "\n\n");
    }

    // Escapes a value for CSV output
    static string CsvEscape(string s)
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
}