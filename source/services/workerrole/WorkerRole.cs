using System;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using BuiltSteady.Zaplify.ServiceHost;
using System.IO;
using System.Diagnostics;

namespace BuiltSteady.Zaplify.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        const int timeout = 30000;  // 30 seconds

        public static string Me
        {
            get { return String.Concat(Environment.MachineName.ToLower(), "-", Thread.CurrentThread.ManagedThreadId.ToString()); }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            // initialize azure logging
            if (HostEnvironment.IsAzureLoggingEnabled)
                TraceLog.InitializeAzureLogging();

            // Log function entrance (must do this after DiagnosticsMonitor has been initialized)
            //TraceLog.TraceFunction();

            return base.OnStart();
        }

        public override void Run()
        {
            // initialize splunk logging (we do this here instead of OnStart so that the Azure logger has time to really start)
            if (HostEnvironment.IsSplunkLoggingEnabled)
                TraceLog.InitializeSplunkLogging();

            TraceLog.TraceInfo("WorkerRole started");

            // check the database schema versions to make sure there is no version mismatch
            if (!Storage.NewUserContext.CheckSchemaVersion())
            {
                TraceLog.TraceFatal("User database schema is out of sync, unrecoverable error");
                return;
            }
            if (!Storage.NewSuggestionsContext.CheckSchemaVersion())
            {
                TraceLog.TraceFatal("Suggestions database schema is out of sync, unrecoverable error");
                return;
            }

            // (re)create the database constants if the code contains a newer version
            if (!Storage.NewUserContext.VersionConstants(Me))
            {
                TraceLog.TraceFatal("Cannot check and/or update the User database constants, unrecoverable error");
                return;
            }
            if (!Storage.NewSuggestionsContext.VersionConstants(Me))
            {
                TraceLog.TraceFatal("Cannot check and/or update the Suggestions database constants, unrecoverable error");
                return;
            }

            // get the number of workers (default is 1)
            int mailWorkerCount = ConfigurationSettings.GetAsNullableInt(HostEnvironment.MailWorkerCountConfigKey) ?? 1;
            int workflowWorkerCount = ConfigurationSettings.GetAsNullableInt(HostEnvironment.WorkflowWorkerCountConfigKey) ?? 1;

            var mailWorkerArray = new MailWorker.MailWorker[mailWorkerCount];
            var workflowWorkerArray = new WorkflowWorker.WorkflowWorker[workflowWorkerCount];

            // run an infinite loop doing the following:
            //   check whether the worker services have stopped (indicated by a null reference)
            //   (re)start the service on a new thread if necessary
            //   sleep for the timeout period
            while (true)
            {
                RestartWorkerThreads<WorkflowWorker.WorkflowWorker>(workflowWorkerArray);
                if (!HostEnvironment.IsAzureDevFabric)
                    RestartWorkerThreads<MailWorker.MailWorker>(mailWorkerArray);

                // sleep for the timeout period
                Thread.Sleep(timeout);
            }
        }

        #region Helpers

        void RestartWorkerThreads<T>(Array array) where T : IWorker, new() 
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array.GetValue(i) == null)
                {
                    int threadNum = i;
                    Thread thread = new Thread(() =>
                    {
                        try
                        {
                            T worker = new T();
                            array.SetValue(worker, threadNum);

                            // sleep for a fraction of the worker's Timeout that corresponds to the position in the array
                            // this is to spread out the workers relatively evenly across the entire Timeout interval
                            Thread.Sleep(worker.Timeout * threadNum / array.Length);
                            worker.Start();
                        }
                        catch (Exception ex)
                        {
                            TraceLog.TraceFatal(String.Format("{0}{1} died; ex: {2}", typeof(T).Name, threadNum.ToString(), ex.Message));
                            array.SetValue(null, threadNum);
                        }
                    }) { Name = typeof(T).Name + i.ToString() };

                    thread.Start();
                    TraceLog.TraceInfo(thread.Name + " started");
                }
            }
        }

        #endregion Helpers
    }
}
