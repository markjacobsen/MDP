using System.IO.Pipes;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using CFG2.MDP;

class MDPExtractDataverse
{
    private readonly string runGuid = Guid.NewGuid().ToString();

    static async Task<int> Main(string[] args)
    {
        if (args.Length != 3)
        {
            MDPLib.Log("Usage: MDPExtractDataverse <path-to-dv-files> <dv-files> <connKey>");
            return 1;
        }

        string dvDir = args[0];
        string[] dvFiles = args[1].Split(",");
        string connKey = args[2];

        MDPExtractDataverse extractor = new MDPExtractDataverse();
        Task<int> ret = extractor.Extract(dvDir, dvFiles, connKey);
        return ret.IsCompletedSuccessfully ? ret.Result : await ret;
    }

    async Task<int> Extract(string dvDir, string[] dvFiles, string connKey)
    {

        if (!Directory.Exists(dvDir))
        {
            MDPLib.Log($"ERROR: Directory not found: {dvDir}", this.runGuid);
            return -1;
        }

        // Get server
        string server = GetConnection(connKey);

        if (string.IsNullOrEmpty(server))
        {
            MDPLib.Log("ERROR: Missing connection information.", this.runGuid);
            return -1;
        }

        // Use InteractiveBrowserCredential for authentication
        var credential = new InteractiveBrowserCredential();

        // The scope for Dynamics 365 is the resource URL with "/.default"
        string[] scopes = new[] { $"https://{server}/.default" };

        string accessToken;
        try
        {
            var tokenRequestContext = new TokenRequestContext(scopes);
            var token = await credential.GetTokenAsync(tokenRequestContext);
            accessToken = token.Token;
        }
        catch (Exception ex)
        {
            MDPLib.Log($"ERROR: Failed to acquire token: {ex.Message}", this.runGuid);
            return 1;
        }

        // Process files
        int successfullyProcessed = 0;
        foreach (string dvFile in dvFiles)
        {
            string dvFilePath = Path.Combine(dvDir, dvFile);

            string logPath = Path.Combine(
                Path.GetDirectoryName(dvFilePath),
                Path.GetFileNameWithoutExtension(dvFilePath) + ".log"
            );

            // Parse query file
            var queryParams = File.ReadAllLines(dvFilePath)
                                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains('='))
                                .Select(line => line.Split('=', 2))
                                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

            if (!queryParams.TryGetValue("table", out string table) || string.IsNullOrWhiteSpace(table))
            {
                MDPLib.Log("ERROR: Missing or empty 'table' parameter in query file.", this.runGuid);
                return 1;
            }
            if (!queryParams.TryGetValue("fields", out string fields) || string.IsNullOrWhiteSpace(fields))
            {
                MDPLib.Log("ERROR: Missing or empty 'fields' parameter in query file.", this.runGuid);
                return 1;
            }

            string[] fieldList = fields.Split(',').Select(f => f.Trim()).ToArray();

            // Build URL
            string url = $"https://{server}/api/data/v9.1/{table}?$select={string.Join(",", fieldList)}";
            MDPLib.Log("Requesting: " + url, this.runGuid);

            // Make HTTP GET call
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MDPLib.Log($"ERROR: HTTP request failed: {ex.Message}", this.runGuid);
                return 1;
            }

            string json = await response.Content.ReadAsStringAsync();

            // Parse JSON
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("value", out JsonElement valueElement) || valueElement.ValueKind != JsonValueKind.Array)
            {
                MDPLib.Log("ERROR: Unexpected JSON format: missing 'value' array.", this.runGuid);
                return 1;
            }

            // Prepare CSV output
            string csvFile = Path.ChangeExtension(dvFilePath, ".csv");
            MDPLib.Log("Writing results to: " + csvFile, this.runGuid);
            using var writer = new StreamWriter(csvFile);

            // Write header 
            writer.WriteLine(string.Join(",", fieldList));

            // Write rows
            int records = 0;
            foreach (var item in valueElement.EnumerateArray())
            {
                var row = fieldList.Select(field =>
                {
                    if (item.TryGetProperty(field, out JsonElement fieldValue))
                    {
                        string val = fieldValue.ToString();
                        // Escape double quotes and commas
                        if (val.Contains('"') || val.Contains(','))
                            val = $"\"{val.Replace("\"", "\"\"")}\"";
                        return val;
                    }
                    else
                    {
                        return "";
                    }
                });
                writer.WriteLine(string.Join(",", row));
                records++;
            }

            MDPLib.Log($"Wrote {records} records to: {csvFile}", this.runGuid, true, true);
        }
        return 0;
    }

    // Stub for GetConnection
    static string GetConnection(string connKey)
    {
        string propertiesFilePath = Path.Combine(Environment.GetEnvironmentVariable("SYNC_DRIVE_HOME"), @"60 JNL Ref\60.7 - Databases\db.properties");
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
}