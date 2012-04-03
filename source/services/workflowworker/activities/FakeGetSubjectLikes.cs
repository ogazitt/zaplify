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
        public override string Name { get { return ActivityNames.GetSubjectLikes; } }
        public override string TargetFieldName { get { return SuggestionTypes.ChooseOne; } }
        public override Func<WorkflowInstance, ServerEntity, object, bool> Function
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

        private bool GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("GenerateSuggestions: non-Item passed in");
                return true;  // this will terminate the state
            }

            // TODO: get likes from the Contacts folder, Facebook, and Cloud AD
            // Generate a new contact for any non-matching FB or AD contact in the contacts list for this item
            
            // HACK: hardcode names for now until the graph queries are in place
            foreach (var like in "Golf;Seattle Sounders;Malcolm Gladwell".Split(';'))
            {
                suggestionList[like] = like;
            }
            
            // inexact match
            return false;
        }
    }
}
