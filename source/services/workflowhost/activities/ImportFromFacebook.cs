using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.ServiceHost;
using System.Collections.Generic;

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
                    Folder folder = entity as Folder;
                    if (folder == null)
                    {
                        TraceLog.TraceError("ImportFromFacebook: non-Folder passed in");
                        return Status.Error;
                    }

                    User user = null;
                    if (UserContext.Users.Any(u => u.ID == folder.UserID))
                        user = UserContext.Users.Include("UserCredentials").Single(u => u.ID == folder.UserID);
                    else
                    {
                        TraceLog.TraceError("ImportFromFacebook: User not found");
                        return Status.Error;
                    }

                    if (FacebookProcessor.GetUserInfo(UserContext, user) == false)
                        return Status.Error;

                    if (FacebookProcessor.ImportFriendsAsPossibleContacts(UserContext, user, folder))
                        return Status.Complete;
                    else
                        return Status.Error;
                });
            }
        }
    }
}
