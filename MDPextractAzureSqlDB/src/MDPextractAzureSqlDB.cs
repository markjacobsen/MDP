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
            MDPLib.Log("Usage: MDPextractAzureSqlDB <connKey> <path-to-sql-files> <sql-files>");
            return;
        }

        string connKey = args[0];
        string sqlDir = args[1];
        string[] sqlFiles = args[2].Split(",");

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
        var (server, database) = MDPLib.GetAzureSqlDBConnInfo(connKey);

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
            string queryFile = Path.Combine(sqlDir, sqlFile);
            string logPath = Path.Combine(
                Path.GetDirectoryName(queryFile),
                Path.GetFileNameWithoutExtension(queryFile) + ".log"
            );
            string csvFilePath = Path.Combine(
                Path.GetDirectoryName(queryFile),
                Path.GetFileNameWithoutExtension(queryFile) + ".csv"
            );

            try
            {
                if (!File.Exists(queryFile))
                {
                    throw new Exception($"Query file not found: {queryFile}");
                }

                string sql = MDPLib.GetSqlFromFile(queryFile);
                if (string.IsNullOrEmpty(sql))
                {
                    throw new Exception($"No SQL found in file: {queryFile}");
                }

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

        MDPLib.Log("Processed "+successfullyProcessed+" of "+sqlFiles.Length+" file(s) successfully in: "+sqlDir, this.runGuid, true, true);
    }
}