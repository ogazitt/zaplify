using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class ActivityNames
    {
        public const string GetPossibleTasks = "Get Possible Tasks";
        public const string GetPossibleDates = "Get Possible Dates";
        public const string GetPossibleLocations = "Get Possible Locations";
        public const string GetPossibleSubjects = "Get Possible Subjects";
        public const string GetSubjectAttributes = "Get Subject Attributes";
        public const string GetSuggestions = "Get Suggestions";
        public const string StartWorkflow = "Start Workflow";
    }

    public class ActivityList
    {
        public static Dictionary<string, WorkflowActivity> Activities = new Dictionary<string, WorkflowActivity>()
        {
            { ActivityNames.GetPossibleTasks, new GetSuggestions() },
            { ActivityNames.GetPossibleDates, new GetSuggestions() },
            { ActivityNames.GetPossibleLocations, new GetSuggestions() },
            { ActivityNames.GetPossibleSubjects, new GetSuggestions() },
            { ActivityNames.GetSubjectAttributes, new GetSuggestions() },
            { ActivityNames.GetSuggestions, new GetSuggestions() },
            { ActivityNames.StartWorkflow, new StartWorkflow() },
        };
    }
}
