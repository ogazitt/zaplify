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
        public override Func<WorkflowInstance, ServerEntity, object, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    try
                    {
                        // get the last state's result as the workflow name
                        string workflowName = GetInstanceData(workflowInstance, Workflow.LastStateData);
                        
                        // if the data passed in isn't null, use this instead
                        if (data != null)
                            workflowName = (string)data;
                         
                        if (workflowName != null)
                            Workflow.StartWorkflow(workflowName, entity, workflowInstance.InstanceData);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("StartWorkflow Activity failed; ex: " + ex.Message);
                    }

                    // the state should always move forward
                    return true;
                });
            }
        }
    }
}
