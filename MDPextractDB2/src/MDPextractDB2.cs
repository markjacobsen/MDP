﻿namespace CFG2.MDP;

using System.Data.SQLite;
using System.Text;
using IBM.Data.Db2;

class MDPextractDB2
{
    private readonly string runGuid = Guid.NewGuid().ToString();

    static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            MDPLib.Log("Usage: MDPextractDB2 <connKey> <path-to-sql-files> <sql-files>");
            return 1;
        }

        string connKey = args[0];
        string sqlDir = args[1];
        string[] sqlFiles = args[2].Split(",");
        string db2Username = SecLib.Retrieve(connKey+".user");
        string db2Password = SecLib.Retrieve(connKey+".pass");

        if (string.IsNullOrEmpty(db2Username) || string.IsNullOrEmpty(db2Password))
        {
            MDPLib.Log($"Empty username or password for connection key: {connKey}");
            return 1;
        }
        MDPextractDB2 app = new MDPextractDB2();
        bool success = app.Extract(sqlDir, sqlFiles, connKey, db2Username, db2Password);
        return success ? 0 : 1;
    }

    bool Extract(string sqlDir, string[] sqlFiles, string connKey, string db2Username, string db2Password)
    {
        if (!Directory.Exists(sqlDir))
        {
            MDPLib.Log($"ERROR: Directory not found: {sqlDir}", this.runGuid);
            return false;
        }

        // Get connection info
        var (host, port, db) = MDPLib.GetDB2ConnInfo(connKey);

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(db))
        {
            MDPLib.Log("Missing connection information.", this.runGuid);
            return false;
        }

        string connStr = $"Server={host}:{port};Database={db};UID={db2Username};PWD={db2Password};";
        MDPLib.Log($"Connecting to {db} on {host}:{port}", this.runGuid);

        int successfullyProcessed = 0;
        foreach (string sqlFile in sqlFiles)
        {
            string queryFile = Path.Combine(sqlDir, sqlFile);
            string csvFilePath = Path.Combine(
                    Path.GetDirectoryName(queryFile),
                    Path.GetFileNameWithoutExtension(queryFile) + ".csv"
                );
            string logPath = Path.Combine(
                    Path.GetDirectoryName(queryFile),
                    Path.GetFileNameWithoutExtension(queryFile) + ".log"
                );
            int records = 0;
            try
            {
                if (!File.Exists(queryFile))
                {
                    throw new Exception($"Query file not found: {queryFile}");
                }

                string sql = MDPLib.GetSqlFromFile(queryFile);
                if (string.IsNullOrWhiteSpace(sql))
                {
                    throw new Exception($"No SQL found in file: {queryFile}");
                }

                using (var conn = new DB2Connection(connStr))
                {
                    MDPLib.Log("Executing SQL: \n" + sql, this.runGuid);
                    conn.Open();
                    using (var cmd = new DB2Command(sql, conn))
                    {
                        cmd.CommandTimeout = 600; // 10minutes
                        MDPLib.Log("Starting: " + DateTime.Now.ToString("HH:mm:ss"));
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
                            writer.WriteLine(string.Join(",", Array.ConvertAll(columnNames, MDPLib.EscapeCsvValue)));

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
                                    sb.Append(MDPLib.EscapeCsvValue(value?.ToString()));
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
                successfullyProcessed++;
            }
            catch (Exception ex)
            {
                if (File.Exists(csvFilePath))
                {
                    File.Delete(csvFilePath);
                }
                MDPLib.Log("ERROR executing on rec " + records + ". Extract file deleted: " + ex.Message, this.runGuid, true, true);
            }
        }

        MDPLib.Log("Processed " + successfullyProcessed + " of " + sqlFiles.Length + " file(s) successfully in: " + sqlDir, this.runGuid, true, true);

        if (successfullyProcessed == sqlFiles.Length)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}