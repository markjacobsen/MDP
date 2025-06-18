namespace CFG2.MDP;

class MDPconsole
{
    public static void Main(string[] args)
    {
        string menuChoice = "";
        while (!menuChoice.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
        {
            menuChoice = Menu();
            if (menuChoice.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Quitting");
            }
            else if (menuChoice.Equals("D", StringComparison.CurrentCultureIgnoreCase))
            {
                DisplayConfig();
            }
            else if (menuChoice.Equals("S", StringComparison.CurrentCultureIgnoreCase))
            {
                StoreSecret();
            }
            else if (menuChoice.Equals("L", StringComparison.CurrentCultureIgnoreCase))
            {
                ListKeys();
            }
            else if (menuChoice.Equals("C", StringComparison.CurrentCultureIgnoreCase))
            {
                ConfigureDbConn();
            }
            else if (menuChoice.Equals("T", StringComparison.CurrentCultureIgnoreCase))
            {
                Test();
            }
            else
            {
                Console.WriteLine($"Invalid selection: {menuChoice}");
            }
        }
    }

    private static string Menu()
    {
        Console.WriteLine("");
        Console.WriteLine("=====================================");
        Console.WriteLine("MDP Console - Please make a selection");
        Console.WriteLine("=====================================");
        Console.WriteLine("C. Configure DB Connection");
        Console.WriteLine("D. Display DB Config");
        Console.WriteLine("L. List DB Connection Keys");
        Console.WriteLine("S. Store Secret");
        //Console.WriteLine("T. Test");
        Console.WriteLine("Q. Quit");

        return Console.ReadLine();
    }

    private static void DisplayConfig()
    {
        ListKeys();
        Console.WriteLine("Key:");
        string key = Console.ReadLine();
        Console.WriteLine(MDPConfig.GetKeyDisplay(key));
    }

    private static void ListKeys()
    {
        List<string> keys = MDPConfig.GetKeys();
        foreach (string key in keys)
        {
            Console.WriteLine(key);
        }
    }

    private static void UpdatePassword()
    {
        Console.WriteLine("Key:");
        string key = Console.ReadLine();
    }

    private static void ConfigureDbConn()
    {
        Console.WriteLine("Key:");
        string key = Console.ReadLine();

        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("Key is empty");
            return;
        }
        else if (key.Equals("MDP", StringComparison.CurrentCultureIgnoreCase))
        {
            Console.WriteLine("Not allowed to overwrite MDP key");
            return;
        }
        else if (MDPConfig.KeyExists(key))
        {
            Console.WriteLine($"Configuration already exists for {key}. Would you like to continue and overwrite the settings? [Y/N]");
            string opt = Console.ReadLine();
            if (opt.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
            {
                MDPConfig.RemoveKeyEntries(key);
            }
            else
            {
                return;
            }
        }

        Console.WriteLine("DB Type");
        Console.WriteLine("---------------");
        Console.WriteLine("1. DB2");
        Console.WriteLine("2. Azure SQL DB");
        Console.WriteLine("3. SQLite");
        Console.WriteLine("4. Dataverse");

        string menuChoice = Console.ReadLine();
        if (menuChoice.Equals("1"))
        {
            Console.WriteLine("Host:");
            string host = Console.ReadLine();

            Console.WriteLine("Port:");
            string port = Console.ReadLine();

            Console.WriteLine("DB:");
            string db = Console.ReadLine();

            Console.WriteLine("Username");
            string user = Console.ReadLine();

            Console.WriteLine("Password");
            string pass = Console.ReadLine();

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(db) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine("Error. Invalid configuration info");
            }
            else
            {
                MDPConfig.StoreDb2(key, host, port, db, user, pass);
            }
        }
        else if (menuChoice.Equals("2"))
        {
            Console.WriteLine("Server (ex: mdb-sql.database.windows.net):");
            string server = Console.ReadLine();

            Console.WriteLine("DB:");
            string db = Console.ReadLine();

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(db))
            {
                Console.WriteLine("Error. Invalid configuration info");
            }
            else
            {
                MDPConfig.StoreAzureSqlDB(key, server, db);
            }
        }
        else if (menuChoice.Equals("3"))
        {
            Console.WriteLine("File (full path):");
            string file = Console.ReadLine();

            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                Console.WriteLine("Error. Invalid configuration info, or DB does not exist");
            }
            else
            {
                MDPConfig.StoreSQLite(key, file);
            }
        }
        else if (menuChoice.Equals("4"))
        {
            Console.WriteLine("Server (ex: our-d365-dev.crm.dynamics.com):");
            string server = Console.ReadLine();

            if (string.IsNullOrEmpty(server))
            {
                Console.WriteLine("Error. Invalid configuration info");
            }
            else
            {
                MDPConfig.StoreDataverse(key, server);
            }
        }
        else
        {
            Console.WriteLine($"Invalid selection: {menuChoice}");
        }
    }

    private static void StoreSecret()
    {
        Console.WriteLine("Key:");
        string key = Console.ReadLine();

        Console.WriteLine("Secret Value:");
        string val = Console.ReadLine();

        if (SecLib.Store(key, val))
        {
            Console.WriteLine($"Stored your secret to {key}");
        }
        else
        {
            Console.WriteLine("Error attempting to store your secret");
        }
    }

    private static void Test()
    {

    }
}
