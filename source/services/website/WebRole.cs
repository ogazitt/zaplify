using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using BuiltSteady.Zaplify.ServiceHost;

namespace Website
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
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
            TraceLog.TraceInfo("BuiltSteady.Zaplify.WebRole started");

            // initialize the Queue 
            MessageQueue.Initialize();

            return base.OnStart();
        }
    }
}
