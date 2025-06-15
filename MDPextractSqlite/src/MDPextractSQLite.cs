using System.Data.SQLite;
using System.Text;

namespace CFG2.MDP;

class MDPextractSQLite
{
    private readonly string runGuid = Guid.NewGuid().ToString();

    static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            MDPLib.Log($"ERROR: Incorrect number of arguments: {args.Length}");
            MDPLib.Log("Usage: MDPextractSQLite <connKey> <path-to-sql-files> <sql-files>");
            return 1;
        }

        string connKey = args[0];
        string sqlDir = args[1];
        string[] sqlFiles = args[2].Split(",");

        MDPextractSQLite extractor = new MDPextractSQLite();
        bool success = extractor.Extract(sqlDir, sqlFiles, connKey);
        return success ? 0 : 1;
    }

    bool Extract(string sqlDir, string[] sqlFiles, string connKey)
    {
        string dbFile = MDPLib.GetSQLiteConnInfo(connKey);

        // Validate files exist
        if (!File.Exists(dbFile))
        {
            MDPLib.Log($"Database file not found: {dbFile}", this.runGuid);
            return false;
        }

        int successfullyProcessed = 0;
        foreach (string sqlFile in sqlFiles)
        {
            string queryFile = Path.Combine(sqlDir, sqlFile);
            // Prepare output CSV path
            string csvFile = Path.Combine(
                Path.GetDirectoryName(queryFile),
                Path.GetFileNameWithoutExtension(queryFile) + ".csv"
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

                MDPLib.Log($"Executing query from {queryFile} against {dbFile}", this.runGuid);
                using (var conn = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    using (var writer = new StreamWriter(csvFile, false, Encoding.UTF8))
                    {
                        // Write header
                        string[] columnNames = Enumerable.Range(0, reader.FieldCount)
                            .Select(reader.GetName)
                            .ToArray();
                        writer.WriteLine(string.Join(",", columnNames.Select(MDPLib.EscapeCsvValue)));

                        // Write rows
                        while (reader.Read())
                        {
                            string[] fields = new string[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                fields[i] = MDPLib.EscapeCsvValue(reader[i]?.ToString() ?? "");
                            }
                            writer.WriteLine(string.Join(",", fields));
                            records++;
                        }
                    }
                }

                MDPLib.Log("Wrote " + records + " lines of data to " + csvFile, this.runGuid, true, true);
                successfullyProcessed++;
            }
            catch (Exception ex)
            {
                if (File.Exists(csvFile))
                {
                    File.Delete(csvFile);
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