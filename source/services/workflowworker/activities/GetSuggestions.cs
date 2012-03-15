using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetSuggestions : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetSuggestions; } }
        public override string TargetFieldName { get { return FieldNames.SuggestedLink; } }
        public override Func<WorkflowInstance, ServerEntity, object, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("GetSuggestions: non-Item passed in to Function");
                        return false;
                    }

                    try
                    {
                        foreach (var s in "golf club;sounders jersey;outliers".Split(';'))
                        {
                            var url = "http://www.bing.com/search?q=" + s.Replace(' ', '+');
                            var sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                EntityID = item.ID,
                                EntityType = entity.GetType().Name,
                                WorkflowName = workflowInstance.Name,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                FieldName = TargetFieldName, 
                                DisplayName = s,
                                Value = url,
                                //TimeSelected = DateTime.UtcNow,
                                // TODO: define Reasons and use to distinguish Chosen, Exclude, Like, etc.
                                // ReasonSelected = Reasons.Chosen;                            
                            };
                            WorkflowWorker.SuggestionsContext.Suggestions.Add(sugg);
                        }
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("GetSuggestions Activity failed; ex: " + ex.Message);
                    }
                    return true;
                });
            }
        }
    }
}
