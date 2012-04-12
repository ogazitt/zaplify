using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class NoOp : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    return Status.Complete;
                });
            }
        }
    }
}
