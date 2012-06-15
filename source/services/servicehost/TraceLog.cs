using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Text;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class TraceLog
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
            string msg = String.Format(
                "{0}\n{1}",
                MethodInfoText(),
                message);
            TraceLine(msg, LogLevel.Detail);
        }

        public static void TraceInfo(string message)
        {
            string msg = String.Format(
                "{0}\n{1}",
                MethodInfoText(),
                message);
            TraceLine(msg, LogLevel.Info);
        }

        public static void TraceError(string message)
        {
            string msg = String.Format(
                "{0}\n{1}",
                MethodInfoText(),
                message);
            TraceLine(msg, LogLevel.Error);
        }

        public static void TraceException(string message, Exception e)
        {
            StringBuilder sb = new StringBuilder(); 
            int level = 0;
            while (e != null)
            {
                sb.Append(String.Format("[{0}] {1}\n", level++, e.Message));
                e = e.InnerException;
            }

            string msg = String.Format(
                "{0}\n{1}\nExceptions:\n{2}\nStackTrace:\n{3}",
                MethodInfoText(),
                message,
                sb.ToString(),
                StackTraceText(5));
            TraceLine(msg, LogLevel.Error);        
        }

        public static void TraceFatal(string message)
        {
            string msg = String.Format(
                "{0} ***FATAL ERROR***\n{1}",
                MethodInfoText(),
                message);
            TraceLine(msg, LogLevel.Fatal);
        }

        // do not compile this in unless this is a DEBUG build
        [Conditional("DEBUG")]
        public static void TraceFunction()
        {
            string msg = String.Format(
                "Entering {0}",
                MethodInfoText());
            TraceLine(msg, LogLevel.Detail);
        }

        public static void TraceLine(string message, LogLevel level)
        {
            TraceLine(message, LevelText(level));
        }

        public static void TraceLine(string message, string level)
        {
            message = String.Format("{0}: {1}", DateTime.Now.ToString(), message);
            if (RoleEnvironment.IsAvailable)
            {
                Trace.WriteLine(message, level);
                Trace.Flush();
            }
            else
            {
                Console.WriteLine(String.Format("{0}:{1}", level, message));
            }
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

        private static string MethodInfoText()
        {
            StackTrace st = new StackTrace(2, true);
            StackFrame sf = st.GetFrame(0);
            return StackFrameText(sf);
        }

        private static string StackFrameText(StackFrame sf)
        {
            string fullFileName = sf.GetFileName();
            string filename = "UnknownFile";
            if (!string.IsNullOrEmpty(fullFileName))
            {
                string[] parts = fullFileName.Split('\\');
                filename = parts[parts.Length - 1];
            }
            string msg = String.Format(
                "{0}() in {1}:{2}",
                sf.GetMethod().Name,
                filename,
                sf.GetFileLineNumber().ToString());
            return msg;
        }

        private static string StackTraceText(int depth)
        {
            StackTrace st = new StackTrace(2, true);
            StackFrame[] frames = st.GetFrames();
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < frames.Length)
            {
                sb.Append(String.Format("({0}) {1}\n", i, StackFrameText(frames[i])));
                if (++i >= depth) break;
            }
            return sb.ToString();
        }
        #endregion
    }
}