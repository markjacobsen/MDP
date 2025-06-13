namespace CFG2.MDP;

class Program
{
    static int Main(string[] args)
    {
        string loadGuid = Guid.NewGuid().ToString();

        if (args.Length != 1)
        {
            MDPLib.Log("Usage: MDPloadLog <full-path-to-file>", loadGuid);
            return 1;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            MDPLib.Log($"File not found: {filePath}", loadGuid);
            return 1;
        }

        MDPLib.Log(File.ReadAllText(filePath), loadGuid, false, true);
        File.Delete(filePath);

        MDPLib.Log($"Loaded {filePath}", loadGuid);

        return 0;
    }
}