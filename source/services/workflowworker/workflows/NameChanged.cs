using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class NameChanged : Workflow
    {
        public override string Name { get { return WorkflowNames.NameChanged; } }
        public override List<WorkflowState> States { get { return states; } }

        private static List<WorkflowState> states = new List<WorkflowState>()
        {
            new WorkflowState() { Name = "FindIntent", Activity = ActivityNames.GetPossibleIntents, NextState = null },
        };
    }
}
