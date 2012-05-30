using System;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class StartWorkflow : WorkflowActivity
    {
        public class ActivityParameters
        {
            public const string WorkflowType = "WorkflowType";
        }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    try
                    {
                        // if the WorkflowType parameter was omitted, get the last state's result as the workflow name
                        string workflowName = null;
                        if (InputParameters.TryGetValue(ActivityParameters.WorkflowType, out workflowName) == false)
                            workflowName = GetInstanceData(workflowInstance, ActivityVariables.LastStateData);
                        
                        // if the data passed in isn't null, use this instead
                        if (data != null)
                            workflowName = (string)data;

                        // process any parameters in the workflow name
                        workflowName = ExpandVariables(workflowInstance, workflowName);

                        if (workflowName != null)
                            WorkflowHost.StartWorkflow(UserContext, SuggestionsContext, workflowName, entity, workflowInstance.InstanceData);
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
