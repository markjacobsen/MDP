namespace CFG2.MDP;

class MDPconsole
{
    public static void Main(string[] args)
    {
        string menuChoice = "";
        while (!menuChoice.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
        {
            menuChoice = Menu();
            if (menuChoice.Equals("T", StringComparison.CurrentCultureIgnoreCase))
            {
                Test();
            }
            else if (menuChoice.Equals("S", StringComparison.CurrentCultureIgnoreCase))
            {
                StoreSecret();
            }
            else if (menuChoice.Equals("C", StringComparison.CurrentCultureIgnoreCase))
            {
                ConfigureDbConn();
            }
            else if (menuChoice.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Quitting");
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
        Console.WriteLine("S. Store Secret");
        Console.WriteLine("T. Test");
        Console.WriteLine("Q. Quit");

        return Console.ReadLine();
    }

    private static void ConfigureDbConn()
    {
        Console.WriteLine("Key:");
        string key = Console.ReadLine();

        if (MDPConfig.KeyExists(key))
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
        //Console.WriteLine("2. Azure SQL DB");
        //Console.WriteLine("3. SQLite");
        //Console.WriteLine("4. Dataverse");

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
