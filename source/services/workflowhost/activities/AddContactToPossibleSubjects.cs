using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class AddContactToPossibleSubjects : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("AddContactToPossibleSubject: non-Item passed in");
                        return Status.Error;
                    }

                    if (PossibleContactProcessor.AddContact(UserContext, item))
                        return Status.Complete;
                    else
                        return Status.Error;
                });
            }
        }
    }
}
