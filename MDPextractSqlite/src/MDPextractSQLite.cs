using System.Data.SQLite;
using System.Text;

namespace CFG2.MDP;

class MDPextractSQLite
{
    private readonly string runGuid = Guid.NewGuid().ToString();

    static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            MDPLib.Log("Usage: MDPextractSQLite <queryFile> <connKey>");
            return 1;
        }

        string queryFile = args[0];
        string connKey = args[1];

        MDPextractSQLite extractor = new MDPextractSQLite();
        return extractor.Extract(queryFile, connKey);
    }

    int Extract(string queryFile, string connKey)
    {
        string dbFile = MDPLib.GetSQLiteConnInfo(connKey);

        // Validate files exist
        if (!File.Exists(dbFile))
        {
            MDPLib.Log($"Database file not found: {dbFile}", this.runGuid);
            return 1;
        }
        if (!File.Exists(queryFile))
        {
            MDPLib.Log($"Query file not found: {queryFile}", this.runGuid);
            return 1;
        }

        MDPLib.Log($"Executing query from {queryFile} against {dbFile}", this.runGuid);

        // Read and clean query file
        string[] lines = File.ReadAllLines(queryFile);
        var queryLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line =>
                !line.TrimStart().StartsWith("--") &&
                !line.TrimStart().StartsWith("#") &&
                !line.TrimStart().StartsWith("//"))
            .ToArray();

        string query = string.Join(Environment.NewLine, queryLines);

        if (string.IsNullOrWhiteSpace(query))
        {
            MDPLib.Log("Query file does not contain a valid SQL statement.", this.runGuid);
            return 1;
        }

        // Prepare output CSV path
        string csvFile = Path.Combine(
            Path.GetDirectoryName(queryFile),
            Path.GetFileNameWithoutExtension(queryFile) + ".csv"
        );

        try
        {
            int records = 0;
            using (var conn = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(query, conn))
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
            return 0;
        }
        catch (Exception ex)
        {
            MDPLib.Log("Error: " + ex.Message, this.runGuid);
            return 1;
        }
    }
}