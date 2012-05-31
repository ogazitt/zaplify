﻿using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class GetContactInfoFromFacebook : WorkflowActivity
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
                        TraceLog.TraceError("GetContactInfoFromFacebook: non-Item passed in to Function");
                        return Status.Error;
                    }

                    if (VerifyItemType(item, SystemItemTypes.Contact) == false)
                        return Status.Error;

                    if (FacebookProcessor.AddContactInfo(UserContext, item))
                        return Status.Complete;
                    else
                        return Status.Error;
                });
            }
        }
    }
}
