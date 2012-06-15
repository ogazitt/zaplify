using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServiceHost;
using System.IO;

namespace BuiltSteady.Zaplify.Tools.UserDataExport
{
    public class DataExporter
    {
        public static bool Export(string connectionString, string filename, string username)
        {
            var context = new UserStorageContext(connectionString);
            var user = context.Users.FirstOrDefault(u => u.Name == username);
            if (user == null)
            {
                Console.WriteLine(String.Format("Export: user {0} not found", username));
                return false;
            }
            
            var userDataModel = new UserDataModel(context, user);
            try
            {
                filename = filename ?? @"userdata.json";
                using (var stream = File.Create(filename))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(userDataModel.JsonUserData);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Export: write failed; ex: ", ex.Message);
                return false;
            }
            return true;
        }
    }
}
