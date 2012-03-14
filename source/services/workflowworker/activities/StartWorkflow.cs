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
        public override Func<WorkflowInstance, ServerEntity, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data, list) =>
                {
                    try
                    {
                        string workflowName = null;
                        if (data != null)
                            workflowName = (string)data;
                        else if (workflowInstance.Body != "")
                            workflowName = workflowInstance.Body;
                        
                        if (workflowName != null)
                            Workflow.StartWorkflow(workflowName, entity);
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
