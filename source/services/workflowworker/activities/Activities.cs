using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class ActivityNames
    {
        public const string GetPossibleIntents = "Get Possible Intents";
        public const string GetPossibleDates = "Get Possible Dates";
        public const string GetPossibleLocations = "Get Possible Locations";
        public const string GetPossibleSubjects = "Get Possible Subjects";
        public const string GetSubjectAttributes = "Get Subject Attributes";
        public const string GetSuggestions = "Get Suggestions";
        public const string NoOp = "NoOp";
        public const string StartWorkflow = "Start Workflow";
    }

    public class ActivityList
    {
        public static Dictionary<string, WorkflowActivity> Activities = new Dictionary<string, WorkflowActivity>()
        {
            { ActivityNames.GetPossibleIntents, new GetPossibleIntents() },
            { ActivityNames.GetPossibleDates, null },
            { ActivityNames.GetPossibleLocations, null },
            { ActivityNames.GetPossibleSubjects, new GetPossibleSubjects() },
            { ActivityNames.GetSubjectAttributes, null },
            { ActivityNames.GetSuggestions, new GetSuggestions() },
            { ActivityNames.NoOp, new NoOp() },
            { ActivityNames.StartWorkflow, new StartWorkflow() },
        };
    }
}
