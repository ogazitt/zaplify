﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetSubjectAttributes : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetSubjectAttributes; } }
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

            // TODO: get subject attributes from the Contacts folder, Facebook, and Cloud AD
            // these will hang off of the contact as well as in the workflow InstanceData

            // inexact match
            return false;
        }
    }
}
