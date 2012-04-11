using System;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        // references to various worker instances
        static MailWorker.MailWorker mailWorker = null;
        static WorkflowWorker.WorkflowWorker workflowWorker = null;
        const int timeout = 30000;  // 30 seconds

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            var config = DiagnosticMonitor.GetDefaultInitialConfiguration();
            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1.0);
            config.Logs.BufferQuotaInMB = 1000;
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            // don't need to start diagnostics since it's automatically started with the Import Diagnostics in in ServiceDefinition.csdef
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", config);

            // Log function entrance (must do this after DiagnosticsMonitor has been initialized)
            TraceLog.TraceFunction();

            return base.OnStart();
        }

        public override void Run()
        {
            TraceLog.TraceInfo("BuiltSteady.Zaplify.WorkerRole started");

            // run an infinite loop doing the following:
            //   check whether the worker services have stopped (indicated by a null reference)
            //   (re)start the service on a new thread if necessary
            //   sleep for the timeout period
            while (true)
            {
                if (workflowWorker == null)
                {
                    // start a thread for the workflow service
                    Thread workflowThread = new Thread(() =>
                    {
                        try
                        {
                            workflowWorker = new WorkflowWorker.WorkflowWorker();
                            workflowWorker.Start();
                        }
                        catch (Exception ex)
                        {
                            TraceLog.TraceFatal("WorkflowWorker died; ex: " + ex.Message);
                            workflowWorker = null;
                        }
                    }) { Name = "WorkflowWorker" };
                    
                    workflowThread.Start();
                    TraceLog.TraceInfo("WorkflowWorker started");
                }

                if (mailWorker == null && !HostEnvironment.IsAzureDevFabric)
                {
                    // start a thread for the workflow service
                    Thread mailThread = new Thread(() =>
                    {
                        try
                        {
                            mailWorker = new MailWorker.MailWorker();
                            mailWorker.Start();
                        }
                        catch (Exception ex)
                        {
                            TraceLog.TraceFatal("MailWorker died; ex: " + ex.Message);
                            mailWorker = null;
                        }
                    }) { Name = "MailWorker" };

                    mailThread.Start();
                    TraceLog.TraceInfo("MailWorker started");
                }

                // sleep for the timeout period
                Thread.Sleep(timeout);
            }
        }
    }
}
