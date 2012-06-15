using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.Tools.UserDataExport
{
    class Program
    {
        static void Main(string[] args)
        {
            var conn = ConfigurationSettings.GetConnection("UsersConnection");
            var file = @"userdata.json";
            string user = null;

            // handle args
            foreach (string arg in args)
            {
                string value = arg.Trim().Trim('/');
                if (value.StartsWith("c:"))
                {
                    value = value.Substring(2);
                    try
                    {
                        conn = System.Configuration.ConfigurationManager.ConnectionStrings[value].ConnectionString;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(String.Format("Connection {0} not found", value));
                        return;
                    }
                    continue;
                }
                if (value.StartsWith("f:"))
                {
                    value = value.Substring(2);
                    file = value;
                    continue;
                }
                if (value.StartsWith("u:"))
                {
                    value = value.Substring(2);
                    user = value;
                    continue;
                }
                if (value.StartsWith("h") || value.StartsWith("?"))
                {
                    Usage();
                    return;
                }
            }

            if (file == null)
            {
                Console.WriteLine("Filename wasn't provided");
                Usage();
                return;
            }
            if (conn == null)
            {
                Console.WriteLine("Connection not found");
                Usage();
                return;
            }
            if (user == null)
            {
                Console.WriteLine("User name wasn't provided");
                Usage();
                return;
            }

            // load the grocery data
            bool success = DataExporter.Export(conn, file, user);
            if (success)
                Console.WriteLine("Succeeded in exporting data");
            else
                Console.WriteLine("Failed to export user data");
        }

        private static void Usage()
        {
            Console.WriteLine(
                "Usage: UserDataExport.exe\n" +
                "\t/c:<connection name>\t(defaults to UsersConnection in app.config)\n" +
                "\t/f:<userdata.json>\t(defaults to userdata.json)\n" +
                "\t/u:<user name>\t\t(must be supplied - in email format)");
        }
    }
}
