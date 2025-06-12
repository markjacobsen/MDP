namespace CFG2.MDP;

using System.Data.SqlClient;
using System.Text;
using Azure.Core;
using Azure.Identity;

class MDPextractAzureSqlDB
{
    private readonly string runGuid = Guid.NewGuid().ToString();

    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            MDPLib.Log("Usage: MDPextractAzureSqlDB <path-to-sql-files> <sql-files> <connKey>");
            return;
        }

        string sqlDir = args[0];
        string[] sqlFiles = args[1].Split(",");
        string connKey = args[2];

        MDPextractAzureSqlDB app = new MDPextractAzureSqlDB();
        app.Extract(sqlDir, sqlFiles, connKey);
    }

    void Extract(string sqlDir, string[] sqlFiles, string connKey)
    {
        if (!Directory.Exists(sqlDir))
        {
            MDPLib.Log($"ERROR: Directory not found: {sqlDir}", this.runGuid);
            return;
        }

        // Get connection info
        var (server, database) = GetConnection(connKey);

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database))
        {
            MDPLib.Log("ERROR: Missing connection information.", this.runGuid);
            return;
        }

        string connStr = $"Server=tcp:{server},1433;Database={database};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        MDPLib.Log("Connecting to: " + connStr, this.runGuid);

        // Acquire token using InteractiveBrowserCredential
        var credential = new InteractiveBrowserCredential(); // Necessary for login (opens a browser)
        var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net//.default" });
        AccessToken token = credential.GetToken(tokenRequestContext);

        // Process files
        int successfullyProcessed = 0;
        foreach (string sqlFile in sqlFiles)
        {
            string sqlFilePath = Path.Combine(sqlDir, sqlFile);

            string logPath = Path.Combine(
                Path.GetDirectoryName(sqlFilePath),
                Path.GetFileNameWithoutExtension(sqlFilePath) + ".log"
            );

            MDPLib.Log("Reading file: " + sqlFilePath, this.runGuid);
            StringBuilder sqlBuilder = new StringBuilder();

            foreach (var line in File.ReadLines(sqlFilePath))
            {
                if (!line.TrimStart().StartsWith("--"))
                    sqlBuilder.AppendLine(line);
            }

            string sql = sqlBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(sql))
            {
                string csvFilePath = Path.Combine(
                    Path.GetDirectoryName(sqlFilePath),
                    Path.GetFileNameWithoutExtension(sqlFilePath) + ".csv"
                );

                MDPLib.Log("Connecting to: " + connStr, this.runGuid);

                try
                {
                    int lines = 0;
                    using (var conn = new SqlConnection(connStr))
                    {
                        MDPLib.Log("Executing SQL: \n" + sql, this.runGuid);

                        // Attach the token
                        conn.AccessToken = token.Token;
                        conn.Open();
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.CommandTimeout = 600; // 10 minutes
                            using (var reader = cmd.ExecuteReader())
                            using (var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
                            {
                                // Write header
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (i > 0) writer.Write(",");
                                    writer.Write(reader.GetName(i));
                                }
                                writer.WriteLine();

                                // Write rows
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (i > 0) writer.Write(",");
                                        writer.Write(reader[i]?.ToString().Replace("\"", "\"\""));
                                    }
                                    writer.WriteLine();
                                    lines++;
                                }
                            }
                        }
                    }
                    MDPLib.Log($"Query complete. {lines} rows written to {csvFilePath}", this.runGuid);
                    successfullyProcessed++;
                }
                catch (Exception ex)
                {
                    MDPLib.Log("ERROR: " + ex.Message, this.runGuid);
                }
            }
            else
            {
                MDPLib.Log("WARN: No SQL query found in: " + sqlFilePath, this.runGuid);
            }
        }

        MDPLib.Log("Processed "+successfullyProcessed+" of "+sqlFiles.Length+" file(s) successfully in: "+sqlDir, this.runGuid, true, true);
    }

    // Dummy GetConnection method for demonstration
    // Replace with your actual logic to retrieve server and database from connKey
    static (string server, string db) GetConnection(string connKey)
    {
        string propertiesFilePath = MDPLib.GetConnFile();
        string server = null, db = null;

        foreach (var line in File.ReadLines(propertiesFilePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(connKey+".server="))
                server = trimmed.Substring((connKey+".server=").Length).Trim();
            else if (trimmed.StartsWith(connKey+".db="))
                db = trimmed.Substring((connKey+".db=").Length).Trim();
        }

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(db))
            throw new Exception("Missing connection information in db.properties.");

        return (server, db);
    }
}