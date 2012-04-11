﻿using System;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
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

            // get the number of workers
            int mailWorkerCount = ConfigurationSettings.GetAsInt("MailWorkerCount");
            int workflowWorkerCount = ConfigurationSettings.GetAsInt("WorkflowWorkerCount");

            var mailWorkerArray = new MailWorker.MailWorker[mailWorkerCount];
            var workflowWorkerArray = new WorkflowWorker.WorkflowWorker[workflowWorkerCount];

            // run an infinite loop doing the following:
            //   check whether the worker services have stopped (indicated by a null reference)
            //   (re)start the service on a new thread if necessary
            //   sleep for the timeout period
            while (true)
            {
                RestartWorkerThreads<MailWorker.MailWorker>(mailWorkerArray);
                RestartWorkerThreads<WorkflowWorker.WorkflowWorker>(workflowWorkerArray);

#if KILL
                for (int i = 0; i < workflowWorkerArray.Length; i++)
                {
                    if (workflowWorkerArray[i] == null)
                    {
                        Thread thread = new Thread(() =>
                        {
                            int threadNum = i;
                            try
                            {
                                workflowWorkerArray[threadNum] = new WorkflowWorker.WorkflowWorker();
                                workflowWorkerArray[threadNum].Start();
                            }
                            catch (Exception ex)
                            {
                                TraceLog.TraceFatal(String.Format("WorkflowWorker{0} died; ex: {1}", threadNum.ToString(), ex.Message));
                                workflowWorkerArray[threadNum] = null;
                            }
                        }) { Name = "WorkflowWorker" + i.ToString() };

                        thread.Start();
                        TraceLog.TraceInfo(thread.Name + " started");
                    }
                }

                for (int i = 0; i < mailWorkerArray.Length; i++)
                {
                    if (mailWorkerArray[i] == null)
                    {
                        Thread thread = new Thread(() =>
                        {
                            int threadNum = i;
                            try
                            {
                                mailWorkerArray[threadNum] = new MailWorker.MailWorker();
                                mailWorkerArray[threadNum].Start();
                            }
                            catch (Exception ex)
                            {
                                TraceLog.TraceFatal(String.Format("MailWorker{0} died; ex: {1}", threadNum.ToString(), ex.Message));
                                mailWorkerArray[threadNum] = null;
                            }
                        }) { Name = "MailWorker" + i.ToString() };

                        thread.Start();
                        TraceLog.TraceInfo(thread.Name + " started");
                    }
                }
#endif

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
