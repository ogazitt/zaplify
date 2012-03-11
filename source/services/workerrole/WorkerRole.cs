using System;
using System.Net;
using System.Threading;
using BuiltSteady.Zaplify.ServiceHost;
using Microsoft.WindowsAzure.ServiceRuntime;
using BuiltSteady.Zaplify.MailWorker;
using BuiltSteady.Zaplify.WorkflowWorker;


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
            // Log function entrance
            LoggingHelper.TraceFunction();

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        public override void Run()
        {
            LoggingHelper.TraceInfo("BuiltSteady.Zaplify.WorkerRole started");

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
                            LoggingHelper.TraceFatal("WorkflowWorker died; ex: " + ex.Message);
                            workflowWorker = null;
                        }
                    }) { Name = "WorkflowWorker" };
                    workflowThread.Start();
                }

                if (mailWorker == null)
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
                            LoggingHelper.TraceFatal("MailWorker died; ex: " + ex.Message);
                            mailWorker = null;
                        }
                    }) { Name = "MailWorker" };
                    mailThread.Start();
                }

                // sleep for the timeout period
                Thread.Sleep(timeout);
            }
        }
    }
}
