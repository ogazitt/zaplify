using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class FakeGetSubjectLikes : WorkflowActivity
    {
        private const string Likes = "Likes";
        public override string SuggestionType { get { return SuggestionTypes.ChooseOne; } }
        public override string OutputParameterName { get { return Likes; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    return Execute(
                        workflowInstance, 
                        entity,
                        data,
                        SystemItemTypes.Task,
                        (instance, e, dict) => { return GenerateSuggestions(instance, e, dict); });
                });
            }
        }

        private Status GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("GenerateSuggestions: non-Item passed in");
                return Status.Error;
            }

            // HACK: hardcode names for now until the graph queries are in place
            foreach (var like in "Golf;Seattle Sounders;Malcolm Gladwell".Split(';'))
            {
                suggestionList[like] = like;
            }
            
            return Status.Pending;
        }
    }
}
