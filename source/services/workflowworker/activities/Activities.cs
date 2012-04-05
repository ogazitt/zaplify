using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class ActivityNames
    {
        public const string FakeGetPossibleSubjects = "FakeGetPossibleSubjects";
        public const string FakeGetSubjectLikes = "FakeGetSubjectLikes";
        public const string GetPossibleIntents = "GetPossibleIntents";
        public const string GetPossibleDates = "GetPossibleDates";
        public const string GetPossibleLocations = "GetPossibleLocations";
        public const string GetPossibleSubjects = "GetPossibleSubjects";
        public const string GetSubjectAttributes = "GetSubjectAttributes";
        public const string GetSubjectLikes = "GetSubjectLikes";
        public const string GetBingSuggestions = "GetBingSuggestions";
        public const string NoOp = "NoOp";
        public const string StartWorkflow = "StartWorkflow";
    }

    public class ActivityList
    {
        public static Dictionary<string, WorkflowActivity> Activities = new Dictionary<string, WorkflowActivity>()
        {
            { ActivityNames.FakeGetPossibleSubjects, new FakeGetPossibleSubjects() },
            { ActivityNames.FakeGetSubjectLikes, new FakeGetSubjectLikes() },
            { ActivityNames.GetBingSuggestions, new GetBingSuggestions() },
            { ActivityNames.GetPossibleIntents, new GetPossibleIntents() },
            { ActivityNames.GetPossibleDates, null },
            { ActivityNames.GetPossibleLocations, null },
            { ActivityNames.GetPossibleSubjects, new GetPossibleSubjects() },
            { ActivityNames.GetSubjectAttributes, new GetSubjectAttributes() },
            { ActivityNames.GetSubjectLikes, new GetSubjectLikes() },
            { ActivityNames.NoOp, new NoOp() },
            { ActivityNames.StartWorkflow, new StartWorkflow() },
        };
    }
}
