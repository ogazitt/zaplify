﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.Bing;

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
                string intentString = GetInstanceData(workflowInstance, FieldNames.Intent);
                string searchFormatString = intentString;
                try
                {
                    Intent intent = WorkflowWorker.SuggestionsContext.Intents.First(i => i.Name == intentString);
                    searchFormatString = intent.SearchFormatString;
                }
                catch (Exception ex)
                {
                    TraceLog.TraceError(String.Format("GenerateSuggestions: intent name {0} not found; ex: {1}", intentString, ex.Message));
                }

                string query = String.Format("{0} {1}", searchFormatString.Trim(), searchTerm.Trim());

                // make a synchronous webservice call to bing 
                //
                // Async has the problem that the caller of this method assumes that a 
                // populated suggestionList will come out.  If it doesn't, the state will execute 
                // again and trigger a fresh set of suggestions to be generated.  Eventually all 
                // queries will return and populate the suggestions DB with duplicate data.
                // This can be fixed once we move to a "real" workflow system such as WF.
                var results = bingSearch.Query(query);
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