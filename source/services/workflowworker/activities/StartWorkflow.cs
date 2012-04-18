using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class StartWorkflow : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    try
                    {
                        // get the last state's result as the workflow name
                        string workflowName = GetInstanceData(workflowInstance, ActivityParameters.WorkflowType);
                        if (workflowName == null)
                            workflowName = GetInstanceData(workflowInstance, ActivityParameters.LastStateData);
                        
                        // if the data passed in isn't null, use this instead
                        if (data != null)
                            workflowName = (string)data;
                         
                        if (workflowName != null)
                            Workflow.StartWorkflow(workflowName, entity, workflowInstance.InstanceData, UserContext, SuggestionsContext);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("StartWorkflow Activity failed", ex);
                        return Status.Error;
                    }

                    // the state should always move forward
                    return Status.Complete;
                });
            }
        }
    }
}
