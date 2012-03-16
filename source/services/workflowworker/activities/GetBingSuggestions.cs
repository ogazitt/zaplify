using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using zaplify.bing;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetBingSuggestions : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetBingSuggestions; } }
        public override string TargetFieldName { get { return FieldNames.SuggestedLink; } }
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

            try
            {
                BingSearch bingSearch = new BingSearch();
                string searchTerm = GetInstanceData(workflowInstance, Workflow.LastStateData);
                string intent = GetInstanceData(workflowInstance, FieldNames.Intent);
                string query = String.Format("{0} {1}", intent.Trim(), searchTerm.Trim());

                IEnumerable<SearchResult> results = bingSearch.Query(query);
                foreach (var r in results)
                {
                    WebResult result = r as WebResult;
                    if (result != null)
                        suggestionList[result.Title] = result.Url;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("GenerateSuggestions: Bing query failed; ex: " + ex.Message);
            }

            // false indicates multiple suggestions returned
            return false;
        }
    }
}
