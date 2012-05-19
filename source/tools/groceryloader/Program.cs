using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServiceHost;

namespace GroceryLoader
{
    class Program
    {
        static void Main(string[] arglist)
        {
            var conn = ConfigurationSettings.GetConnection("DataServicesConnection");
            var file = @"groceries.txt";

            // handle args
            string[] args = Environment.CommandLine.Split('/').Skip(1).ToArray();
            foreach (string arg in args)
            {
                string value = arg.Trim();
                if (value.StartsWith("c:"))
                {
                    value = value.Substring(2);
                    conn = System.Configuration.ConfigurationManager.ConnectionStrings[value].ConnectionString;
                    continue;
                }
                if (value.StartsWith("f:"))
                {
                    value = value.Substring(2);
                    file = value;
                    continue;
                }
                if (value.StartsWith("h") || value.StartsWith("?"))
                {
                    Console.WriteLine(
                        "Usage: GroceryLoader.exe\n" +
                        "\t/f:<groceryfile.txt>\t(defaults to groceries.txt)\n" +
                        "\t/c:<connection name>\t(defaults to SQLDataServicesDev1)");
                    return;
                }
            }

            if (file == null)
            {
                Console.WriteLine("Filename wasn't provided");
                return;
            }
            if (conn == null)
            {
                Console.WriteLine("Connection not found");
                return;
            }

            // load the grocery data
            bool success = GroceryLoader.ReloadGroceryData(conn, file);
            if (success)
                Console.WriteLine("Succeeded in loading groceries");
            else
                Console.WriteLine("Failed to load groceries");
        }
    }
}
