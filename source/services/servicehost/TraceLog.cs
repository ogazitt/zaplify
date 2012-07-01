using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class TraceLog
    {
        const string SplunkLogFile = @"splunkinit.log";

        private static bool? isSplunkInitialized;
        private static object splunkLock = new object();
        private static object azureLock = new object();

        private static bool IsSplunkInitialized
        {
            get
            {
                if (!isSplunkInitialized.HasValue)
                {
                    var splunkLogFilePath = Path.Combine(HostEnvironment.TraceDirectory, SplunkLogFile);
                    isSplunkInitialized = File.Exists(splunkLogFilePath);
                }
                return isSplunkInitialized.Value;
            }
            set
            {
                isSplunkInitialized = value;
            }
        }

        // session name - stored in thread-local storage so it is available as a per-thread intrinsic
        [ThreadStatic]
        private static string session;
        public static string Session
        {
            get
            {
                // for the worker role, grab the session ID from thread-local storage
                if (HostEnvironment.AzureRoleName != HostEnvironment.Website)
                    return session;
                else
                {
                    // for the web role, grab the session ID from the request header and fall back to thread-local storage
                    if (HttpContext.Current != null)
                        return HttpContext.Current.Request.Headers[HttpApplicationHeaders.Session];
                    else
                        return session;
                }
            }
            set
            {
                session = value;
            }
        }

        public static void InitializeAzureLogging()
        {
            lock (azureLock)
            {
                // only run the initialization code at initialization time (and outside the w3wp workers)
                if (HttpContext.Current == null)
                {
                    var config = DiagnosticMonitor.GetDefaultInitialConfiguration();
                    config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1.0);
                    config.Logs.BufferQuotaInMB = 1000;
                    config.Logs.ScheduledTransferLogLevelFilter = Microsoft.WindowsAzure.Diagnostics.LogLevel.Verbose;
                    DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", config);
                }
            }
        }

        public static void InitializeSplunkLogging()
        {
            lock (splunkLock)
            {
                // splunk only needs to be initialized once - when trace directory is created
                if (!IsSplunkInitialized)
                {
                    // make sure this only runs in azure 
                    if (HostEnvironment.IsAzure)
                    {
                        // configure splunk to monitor trace directory
                        try
                        {
                            // construct the splunk executable path
                            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                            var splunkExe = Path.Combine(programFiles, @"SplunkUniversalForwarder", @"bin", @"Splunk.exe");
                            var traceDir = HostEnvironment.TraceDirectory;
                            string cmdline = String.Format("add monitor {0} -auth admin:changeme", traceDir);

                            // configure the environment for the splunk process
                            ProcessStartInfo startinfo = new ProcessStartInfo();
                            startinfo.CreateNoWindow = true;
                            startinfo.UseShellExecute = false;
                            startinfo.FileName = splunkExe;
                            startinfo.Arguments = cmdline;
                            startinfo.WorkingDirectory = traceDir;
                            startinfo.RedirectStandardOutput = true;
                            startinfo.RedirectStandardError = true;

                            // run the splunk process to start monitoring the directory
                            Process exeProcess = Process.Start(startinfo);
                            string stdout = exeProcess.StandardOutput.ReadToEnd();
                            string stderr = exeProcess.StandardError.ReadToEnd();
                            exeProcess.WaitForExit();

                            // write the log file and trace the result
                            string message = String.Format("Finished executing {0} {1}\nStdout: {2}Stderr: {3}", splunkExe, cmdline, stdout, stderr);
                            using (var fs = File.Create(Path.Combine(traceDir, SplunkLogFile)))
                            using (var writer = new StreamWriter(fs))
                            {
                                writer.WriteLine(message);
                            }
                            TraceLog.TraceInfo(message);
                            IsSplunkInitialized = true;
                        }
                        catch (Exception ex)
                        {
                            TraceException("Splunk initialization failed", ex);
                        }
                    }
                }
            }
        }

        public static string LevelText(LogLevel level)
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
            message = String.Format("{0}: {1}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
            if (RoleEnvironment.IsAvailable)
            {
                if (HostEnvironment.IsAzureLoggingEnabled)
                {
                    Trace.WriteLine(message, level);
                    Trace.Flush();
                }
                if (HostEnvironment.IsSplunkLoggingEnabled)
                {
                    if (!isSplunkInitialized.HasValue)
                        InitializeSplunkLogging();
                    SplunkTrace.WriteLine(message, level);
                }
            }
            else
            {
                Console.WriteLine(String.Format("{0}:{1}", level, message));
            }
        }

        #region Helpers

        private static string MethodInfoText()
        {
#if DEBUG
            StackTrace st = new StackTrace(2, true);
#else
            StackTrace st = new StackTrace(1, true);
#endif
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
#if DEBUG
            StackTrace st = new StackTrace(2, true);
#else
            StackTrace st = new StackTrace(1, true);
#endif
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

    public class SplunkTrace
    {
        static object writeLock = new object();

        private static Socket traceSocket;
        public static Socket TraceSocket
        {
            get
            {
                if (traceSocket == null)
                {
                    traceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);                    
                    IPEndPoint hostEndPoint = new IPEndPoint(new IPAddress(new byte[] {127,0,0,1}), HostEnvironment.SplunkLocalPort);
                    traceSocket.Connect(hostEndPoint);
                }
                return traceSocket;
            }
        }

        public static void WriteLine(string message, string level)
        {
            // create a json record
            var record = new TraceRecord()
            {
                Deployment = HostEnvironment.DeploymentName,
                Role = HostEnvironment.AzureRoleName,
                LogLevel = level,
                Session = TraceLog.Session,
                Message = message,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };
            var json = JsonSerializer.Serialize(record) + "\n";
            byte[] buffer = UTF8Encoding.UTF8.GetBytes(json);

            lock (writeLock)
            {
                // enter a retry loop for sending the buffer on the splunk socket
                int retryCount = 2;
                while (retryCount > 0)
                {
                    try
                    {
                        TraceSocket.Send(buffer);
                        // success - terminate the enclosing retry loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        TraceFile.WriteLine(ex.Message, "Error");
                        TraceFile.WriteLine(ex.StackTrace, "Error");

                        // the socket wasn't opened or written to correctly - try to start with a new socket in the next iteration of the retry loop
                        if (traceSocket != null)
                            traceSocket.Dispose();
                        traceSocket = null;
                        retryCount--;
                    }
                }
                
                // multiple socket failures - log to a file as a last resort
                if (retryCount == 0)
                    TraceFile.WriteLine(message, level);
            }
        }
    }

    public class TraceFile
    {
        const int MaxFileSize = 1024 * 1024; // 1MB max file size
        static object writeLock = new object();

        private static string traceFilename;
        public static string TraceFilename
        {
            get
            {
                if (traceFilename == null)
                {
                    // trace filename format: "trace-2012-06-16-23-12-45-123.json";
                    DateTime now = DateTime.UtcNow;
                    traceFilename = Path.Combine(
                        HostEnvironment.TraceDirectory,
                        String.Format("trace-{0}.json", now.ToString("yyyy-MM-dd-HH-mm-ss-fff")));
                }
                return traceFilename;
            }
        }
        
        public static void WriteLine(string message, string level)
        {
            // create a json record
            var record = new TraceRecord()
            { 
                Deployment = HostEnvironment.DeploymentName, 
                Role = HostEnvironment.AzureRoleName,
                LogLevel = level, 
                Message = message, 
                Session = TraceLog.Session,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") 
            };
            var json = JsonSerializer.Serialize(record);

            lock (writeLock)
            {
                // enter a retry loop writing the record to the trace file
                int retryCount = 2;
                while (retryCount > 0)
                {
                    try
                    {
                        if (traceFilename == null)
                        {
                            // create the file
                            using (var stream = File.Create(TraceFilename))
                            using (var writer = new StreamWriter(stream))
                            {
                                // log the file creation
                                var createRecord = new TraceRecord()
                                {
                                    Deployment = HostEnvironment.DeploymentName,
                                    Role = HostEnvironment.AzureRoleName,
                                    LogLevel = TraceLog.LevelText(TraceLog.LogLevel.Info),
                                    Message = "Created new trace file " + TraceFilename,
                                    Session = TraceLog.Session,
                                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                };
                                var createJson = JsonSerializer.Serialize(createRecord);
                                writer.WriteLine(createJson);
                                writer.Flush();
                            }
                        }

                        // open the file
                        using (var stream = File.Open(TraceFilename, FileMode.Append, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.WriteLine(json);
                            writer.Flush();

                            // reset the trace filename if it exceeds the maximum file size
                            if (writer.BaseStream.Position > MaxFileSize)
                                traceFilename = null;
                        }

                        // success - terminate the enclosing retry loop
                        break;
                    }
                    catch (Exception)
                    {
                        // the file wasn't opened or written to correctly - try to start with a new file in the next iteration of the retry loop
                        traceFilename = null;
                        retryCount--;
                    }
                }
            }
        }
    }

    class TraceRecord
    {
        public string Deployment { get; set; }
        public string Role { get; set; }
        public string LogLevel { get; set; }
        public string Timestamp { get; set; }
        public string Session { get; set; }
        public string Message { get; set; }
    }
}