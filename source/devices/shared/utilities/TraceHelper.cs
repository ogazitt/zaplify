using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using TaskStoreClientEntities;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Reflection;

namespace TaskStoreWinPhoneUtilities
{
    public class TraceHelper
    {
        // trace message list
        private static List<string> traceMessages = new List<string>();

        // start time
        private static DateTime startTime;

        /// <summary>
        /// Add a message to the list
        /// </summary>
        public static void AddMessage(string msg)
        {
            TimeSpan ts = DateTime.Now - startTime;
            string str = String.Format("  {0}: {1}", ts.TotalMilliseconds, msg);
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

        public static void SendMessages(User user)
        {
            string msgs = GetMessages();
            byte[] bytes = EncodeString(msgs);
            WebServiceHelper.SendTrace(user, bytes, null, null);
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

        #endregion
    }
}