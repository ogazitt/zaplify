using System;
using System.Net;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class TraceHelper
    {
        // trace message folder
        private static List<string> traceMessages = new List<string>();

        // start time
        private static DateTime startTime = DateTime.Now;

        // file to store messages in
        const string filename = "trace.txt";

        private static string sessionToken;
        public static string SessionToken 
        { 
            get 
            {
                if (sessionToken == null)
                {
#if IOS
                    var deviceName = Environment.MachineName;
#else
                    var deviceName = Microsoft.Phone.Info.DeviceStatus.DeviceName;
#endif
                    sessionToken = String.Format("{0} {1}", deviceName, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
                return sessionToken; 
            } 
        }

        /// <summary>
        /// Add a message to the folder
        /// </summary>
        public static void AddMessage(string msg)
        {
            if (traceMessages.Count == 0)
                traceMessages.Add("Session: " + SessionToken);

            TimeSpan ts = DateTime.Now - startTime;
            double ms = ts.TotalMilliseconds;
#if IOS     // IOS appears to express this in microseconds
            ms /= 1000d;
#endif
            string str = String.Format("  {0}: {1}", ms, msg);
            traceMessages.Add(str);
        }

        /// <summary>
        /// Clear all the messages
        /// </summary>
        public static void ClearMessages()
        {
            traceMessages.Clear();
        }

        public static void StartMessage(string msg)
        {
            if (traceMessages.Count == 0)
                traceMessages.Add("Session: " + SessionToken);

            // capture current time
            startTime = DateTime.Now;

            // trace app start
            traceMessages.Add(String.Format("  {0}: {1}", msg, startTime));
        }

        /// <summary>
        /// Retrieve all messages
        /// </summary>
        /// <returns>String of all the messages concatenated</returns>
        public static string GetMessages()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string msg in traceMessages)
                sb.AppendLine(msg);
            return sb.ToString();
        }

        public static void SendCrashReport(User user, Delegate del, Delegate networkDel)
        {
            string contents = StorageHelper.ReadCrashReport();
            if (contents != null)
            {
                contents = "Crash Report\n" + contents;
                Send(user, contents, del, networkDel);
            }
        }
 
        public static void SendMessages(User user)
        {
            string msgs = GetMessages();
            Send(user, msgs, null, null);
        }

        public static void StoreCrashReport()
        {
            StorageHelper.WriteCrashReport(GetMessages());
        }
 
        #region Helpers

        /// <summary>
        /// Encode a string in text/plain (ASCII) format 
        /// (unused at this time)
        /// </summary>
        /// <param name="str">String to encode</param>
        /// <returns>byte array with ASCII encoding</returns>
        private static byte[] EncodeString(string str)
        {
            char[] unicode = str.ToCharArray();
            byte[] buffer = new byte[unicode.Length];
            int i = 0;
            foreach (char c in unicode)
                buffer[i++] = (byte)c;
            return buffer;
        }

        private static void Send(User user, string msgs, Delegate del, Delegate networkDel)
        {
#if IOS
            WebServiceHelper.SendTrace(user, msgs, del, networkDel);
#else
            //byte[] bytes = Encoding.UTF8.GetBytes(msgs);
            byte[] bytes = EncodeString(msgs);
            WebServiceHelper.SendTrace(user, bytes, del, networkDel);
#endif
        }

        #endregion
    }
}