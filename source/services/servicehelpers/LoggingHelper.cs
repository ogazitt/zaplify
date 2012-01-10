using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace ServiceHelpers
{
    public class LoggingHelper
    {
        public enum LogLevel
        {
            Fatal,
            Error,
            Info,
            Detail
        }

        public static void TraceDetail(string message)
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            string msg = String.Format(
                "Detail from {0} - {1}",
                StackInfoText(),
                message);
            TraceLine(msg, LogLevel.Detail);
        }

        public static void TraceError(string message)
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            string msg = String.Format(
                "Error in {0} - {1}",
                StackInfoText(),
                message);
            TraceLine(msg, LogLevel.Error);
        }

        public static void TraceFatal(string message)
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            string msg = String.Format(
                "***Fatal Error*** in {0} - {1}",
                StackInfoText(),
                message);
            TraceLine(msg, LogLevel.Fatal);
        }

        // do not compile this in unless this is a DEBUG build
        [Conditional("DEBUG")]
        public static void TraceFunction()
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            string msg = String.Format(
                "Entering {0}",
                StackInfoText());
            TraceLine(msg, LogLevel.Detail);
        }

        public static void TraceInfo(string message)
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            string msg = String.Format(
                "Info from {0} - {1}",
                StackInfoText(),
                message);
            TraceLine(msg, LogLevel.Info);
        }

        public static void TraceLine(string message, LogLevel level)
        {
            TraceLine(message, LevelText(level));
        }

        public static void TraceLine(string message, string level)
        {
            string msg = String.Format(
                    "{0}: {1}",
                    DateTime.Now.ToString(),
                    message);

            if (RoleEnvironment.IsAvailable)
            {
                Trace.WriteLine(msg, level);
                Trace.Flush();
            }
            else
                Console.WriteLine(msg);
        }

        #region Helpers

        private static string LevelText(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return "Fatal Error";
                case LogLevel.Error:
                    return "Error";
                case LogLevel.Info:
                    return "Information";
                case LogLevel.Detail:
                    return "Detail";
                default:
                    return "Unknown";
            }
        }

        private static string StackInfoText()
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(2);
            string fullFileName = sf.GetFileName();
            string[] lines = fullFileName.Split('\\');
            string filename = lines[lines.Length - 1];
            string msg = String.Format(
                "{0}() in {1}:{2}",
                sf.GetMethod().Name,
                filename,
                sf.GetFileLineNumber());
            return msg;
        }

        #endregion
    }
}