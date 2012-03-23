using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class FakeBuyGift : Workflow
    {
        public override string Name { get { return IntentNames.FakeBuyGift; } }
        public override List<WorkflowState> States { get { return states; } }

        private static string DetermineSubject = "Who is this for?";
        private static string GetSubjectLikes = "Which kind of gift?";
        private static string GetSuggestions = "Helpful links";

        private static List<WorkflowState> states = new List<WorkflowState>()
        {
            new WorkflowState() { Name = DetermineSubject, Activity = ActivityNames.FakeGetPossibleSubjects, NextState = GetSubjectLikes },
            new WorkflowState() { Name = GetSubjectLikes, Activity = ActivityNames.FakeGetSubjectLikes, NextState = GetSuggestions },
            new WorkflowState() { Name = GetSuggestions, Activity = ActivityNames.GetBingSuggestions, NextState = null },
        };
    }
}
