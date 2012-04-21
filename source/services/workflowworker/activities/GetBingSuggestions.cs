using System;
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
        public class ActivityParameters
        {
            public const string SearchTemplate = "SearchTemplate";
        }
        public override string GroupDisplayName { get { return "Helpful links"; } }
        public override string SuggestionType { get { return SuggestionTypes.NavigateLink; } }
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

            try
            {
                BingSearch bingSearch = new BingSearch();

                // retrieve and format the search template, or if one doesn't exist, use $(Intent)

                string searchTemplate = null;
                if (InputParameters.TryGetValue(ActivityParameters.SearchTemplate, out searchTemplate) == false)
                    searchTemplate = String.Format("$({0})", ActivityVariables.Intent);
                string query = ExpandVariables(workflowInstance, searchTemplate);

                if (String.IsNullOrWhiteSpace(query))
                {
                    TraceLog.TraceInfo("GenerateSuggestions: no query to issue Bing");
                    return Status.Complete;
                }

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
                TraceLog.TraceException("GenerateSuggestions: Bing query failed", ex);
                return Status.Error;
            }

            // this activity is typically last and once links have been generated, no need 
            // to keep the workflow instance around
            return Status.Complete;
        }
    }
}
