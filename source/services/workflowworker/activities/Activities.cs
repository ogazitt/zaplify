using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class ActivityNames
    {
        public const string AddContactToPossibleSubjects = "AddContactToPossibleSubjects";
        public const string ConnectToActiveDirectory = "ConnectToActiveDirectory";
        public const string ConnectToFacebook = "ConnectToFacebook";
        public const string FakeGetPossibleSubjects = "FakeGetPossibleSubjects";
        public const string FakeGetSubjectLikes = "FakeGetSubjectLikes";
        public const string Foreach = "Foreach";
        public const string GenerateSubjectLikes = "GenerateSubjectLikes";
        public const string GetBingSuggestions = "GetBingSuggestions";
        public const string GetContactInfoFromFacebook = "GetContactInfoFromFacebook";
        public const string GetPossibleIntents = "GetPossibleIntents";
        public const string GetPossibleDates = "GetPossibleDates";
        public const string GetPossibleLocations = "GetPossibleLocations";
        public const string GetPossibleSubjects = "GetPossibleSubjects";
        public const string GetSubjectAttributes = "GetSubjectAttributes";
        public const string GetSubjectLikes = "GetSubjectLikes";
        public const string ImportFromFacebook = "ImportFromFacebook";
        public const string NoOp = "NoOp";
        public const string StartWorkflow = "StartWorkflow";
    }

    public class ActivityList
    {
        public static Dictionary<string, Type> Activities = new Dictionary<string, Type>()
        {
            { ActivityNames.AddContactToPossibleSubjects, typeof(AddContactToPossibleSubjects) },
            { ActivityNames.ConnectToActiveDirectory, typeof(ConnectToActiveDirectory) },
            { ActivityNames.ConnectToFacebook, typeof(ConnectToFacebook) },
            { ActivityNames.FakeGetPossibleSubjects, typeof(FakeGetPossibleSubjects) },
            { ActivityNames.FakeGetSubjectLikes, typeof(FakeGetSubjectLikes) },
            { ActivityNames.Foreach, typeof(Foreach) },
            { ActivityNames.GenerateSubjectLikes, typeof(GenerateSubjectLikes) },
            { ActivityNames.GetBingSuggestions, typeof(GetBingSuggestions) },
            { ActivityNames.GetContactInfoFromFacebook, typeof(GetContactInfoFromFacebook) },
            { ActivityNames.GetPossibleIntents, typeof(GetPossibleIntents) },
            { ActivityNames.GetPossibleDates, null },
            { ActivityNames.GetPossibleLocations, null },
            { ActivityNames.GetPossibleSubjects, typeof(GetPossibleSubjects) },
            { ActivityNames.GetSubjectAttributes, typeof(GetSubjectAttributes) },
            { ActivityNames.GetSubjectLikes, typeof(GetSubjectLikes) },
            { ActivityNames.ImportFromFacebook, typeof(ImportFromFacebook) },
            { ActivityNames.NoOp, typeof(NoOp) },
            { ActivityNames.StartWorkflow, typeof(StartWorkflow) },
        };
    }
    
    public class ActivityParameters
    {
        public const string Contact = "Contact";
        public const string FacebookID = "FacebookID";
        public const string ForeachArgument = "ForeachArgument";
        public const string ForeachBody = "ForeachBody";
        public const string ForeachOver = "ForeachOver";
        public const string ForeachSeparator = "ForeachSeparator";
        public const string Intent = "Intent";
        public const string LastStateData = "LastStateData";
        public const string Like = "Like";
        public const string Likes = "Likes";
        public const string LikeSuggestionList = "LikeSuggestionList";
        public const string ParentID = "ParentID";
        public const string SearchTemplate = "SearchTemplate";
        public const string SubjectHint = "SubjectHint";
        public const string WorkflowType = "WorkflowType";
    }
}
