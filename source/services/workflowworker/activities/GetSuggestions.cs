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
        public override Func<WorkflowInstance, Item, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, item, state, list) =>
                {
                    try
                    {
                        foreach (var s in "golf club;sounders jersey;outliers".Split(';'))
                        {
                            var url = "http://www.bing.com/search?q=" + s.Replace(' ', '+');
                            var sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                ItemID = item.ID,
                                WorkflowName = workflowInstance.Name,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                FieldName = TargetFieldName, 
                                DisplayName = s,
                                Value = url,
                                TimeChosen = DateTime.Now
                            };
                            WorkflowWorker.StorageContext.Suggestions.Add(sugg);
                            list.Add(sugg.ID);
                        }
                        WorkflowWorker.StorageContext.SaveChanges();
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
