using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class StartWorkflow : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.StartWorkflow; } }
        public override string TargetFieldName { get { return null; } }
        public override Func<WorkflowInstance, Item, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, item, state, list) =>
                {
                    try
                    {
                        string workflowName = (string)state;
                        Workflow wf = WorkflowList.Workflows[workflowName];
                        DateTime now = DateTime.Now;
                        WorkflowInstance instance = new WorkflowInstance()
                        {
                            ID = Guid.NewGuid(),
                            WorkflowType = wf.Name,
                            State = null,
                            ItemID = item.ID,
                            Name = item.Name,
                            Body = JsonSerializer.Serialize(item),
                            Created = now,
                            LastModified = now
                        };
                        WorkflowWorker.SuggestionsContext.WorkflowInstances.Add(instance);
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("StartWorkflow Activity failed; ex: " + ex.Message);
                    }
                    return true;
                });
            }
        }
    }
}
