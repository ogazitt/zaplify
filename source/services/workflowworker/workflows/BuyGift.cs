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

        private static string DetermineSubject = "Who is this for?";
        private static string GetSubjectLikes = "Which kind of gift?";
        private static string GetSubjectAttributes = "Get the person's attributes";
        private static string DetermineDate = "When is this due?";
        private static string GetSuggestions = "Helpful links";

        private static List<WorkflowState> states = new List<WorkflowState>()
        {
            new WorkflowState() { Name = DetermineSubject, Activity = ActivityNames.GetPossibleSubjects, NextState = GetSubjectLikes },
            new WorkflowState() { Name = GetSubjectLikes, Activity = ActivityNames.GetSubjectLikes, NextState = GetSuggestions },
            //new WorkflowState() { Name = DetermineDate, Activity = ActivityNames.GetPossibleDates, NextState = GetSuggestions },
            new WorkflowState() { Name = GetSuggestions, Activity = ActivityNames.GetBingSuggestions, NextState = null },
        };
    }
}
