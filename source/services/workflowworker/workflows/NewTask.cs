using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class NewTask : Workflow
    {
        public override string Name { get { return WorkflowNames.NewTask; } }
        public override List<WorkflowState> States { get { return states; } }

        private static string DetermineIntent = "Are you trying to";
        private static string InvokeWorkflow = "Invoke Workflow";

        private static List<WorkflowState> states = new List<WorkflowState>()
        {
            new WorkflowState() { Name = DetermineIntent, Activity = ActivityNames.GetPossibleIntents, NextState = InvokeWorkflow },
            new WorkflowState() { Name = InvokeWorkflow, Activity = ActivityNames.StartWorkflow, NextState = null },
        };
    }
}
