using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class GetSubjectAttributes : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Contact attributes"; } }
        public override string OutputParameterName { get { return ActivityVariables.Contact; } }
        public override string SuggestionType { get { return SuggestionTypes.ChooseOne; } }
        public override string TargetFieldName { get { return FieldNames.Contacts; } }
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
                TraceLog.TraceError("Entity is not an Item");
                return Status.Error;  
            }

            // TODO: get subject attributes from the Contacts folder, Facebook, and Cloud AD
            // these will hang off of the contact as well as in the workflow InstanceData

            // inexact match
            return Status.Pending;
        }
    }
}
