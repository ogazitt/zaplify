using System;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceHost.Helpers;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class AddContactToPossibleContacts : WorkflowActivity
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
                        TraceLog.TraceError("AddContactToPossibleContacts: non-Item passed in");
                        return Status.Error;
                    }

                    if (PossibleContactHelper.AddContact(UserContext, item))
                        return Status.Complete;
                    else
                        return Status.Error;
                });
            }
        }
    }
}
