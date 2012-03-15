using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class FindIntent : Workflow
    {
        public override string Name { get { return WorkflowNames.FindIntent; } }
        public override List<WorkflowState> States { get { return states; } }

        private static string DetermineIntent = "Is this what you're trying to do?";
        
        private static List<WorkflowState> states = new List<WorkflowState>()
        {
            new WorkflowState() { Name = DetermineIntent, Activity = ActivityNames.GetPossibleIntents, NextState = null },
        };
    }
}
