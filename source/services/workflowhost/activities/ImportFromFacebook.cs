﻿using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceHost.Helpers;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class ImportFromFacebook : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    User user = entity as User;
                    if (user == null)
                    {
                        TraceLog.TraceError("Entity is not a User");
                        return Status.Error;
                    }

                    user = UserContext.GetUser(user.ID, true);
                    if (user == null)
                    {
                        TraceLog.TraceError("User not found");
                        return Status.Error;
                    }

                    if (FacebookHelper.GetUserInfo(user, UserContext) == false)
                        return Status.Error;

                    if (FacebookHelper.ImportFriendsAsPossibleContacts(user, UserContext))
                        return Status.Complete;
                    else
                        return Status.Error;
                });
            }
        }
    }
}
