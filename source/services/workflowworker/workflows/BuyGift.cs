using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class BuyGift : Workflow
    {
        public override string Name { get { return WorkflowNames.BuyGift; } }
        public override List<WorkflowState> States { get { return states; } }

        private static List<WorkflowState> states = new List<WorkflowState>()
        {
            new WorkflowState() { Name = "DetermineSubject", Activity = ActivityNames.GetPossibleSubjects, NextState = "GetSubjectAttributes" },
            new WorkflowState() { Name = "GetSubjectAttributes", Activity = ActivityNames.GetSubjectAttributes, NextState = "DetermineDate" },
            new WorkflowState() { Name = "DetermineDate", Activity = ActivityNames.GetPossibleDates, NextState = "GetSuggestions" },
            new WorkflowState() { Name = "GetSuggestions", Activity = ActivityNames.GetSuggestions, NextState = null },
        };
    }
}
