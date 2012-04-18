using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class ConnectToFacebook : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Get Connected"; } }
        //public override string OutputParameterName { get { return ActivityParameters.FacebookID; } }
        public override string SuggestionType { get { return SuggestionTypes.GetFBConsent; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    // check for user selection
                    if (data != null)
                        return ProcessActivityData(workflowInstance, data);

                    // find the People folder
                    Folder peopleFolder = null;
                    try
                    {
                        peopleFolder = UserContext.Folders.First(f => f.UserID == entity.ID && f.ItemTypeID == SystemItemTypes.Contact);
                        if (peopleFolder == null)
                        {
                            TraceLog.TraceError("ConnectToFacebook: cannot find People folder");
                            return Status.Error;
                        }
                    }
                    catch (Exception)
                    {
                        TraceLog.TraceError("ConnectToFacebook: cannot find People folder");
                        return Status.Error;
                    }

                    // create a single suggestion to connect to FB
                    var sugg = new Suggestion()
                    {
                        ID = Guid.NewGuid(),
                        EntityID = peopleFolder.ID,
                        EntityType = peopleFolder.GetType().Name,
                        WorkflowType = workflowInstance.WorkflowType,
                        WorkflowInstanceID = workflowInstance.ID,
                        State = workflowInstance.State,
                        SuggestionType = SuggestionType,
                        DisplayName = "Connect to Facebook",
                        GroupDisplayName = "Get Connected",
                        SortOrder = 1,
                    };
                    SuggestionsContext.Suggestions.Add(sugg);

                    // change the workflowInstance to point to the appropriate entity (people folder)
                    workflowInstance.EntityID = peopleFolder.ID;
                    workflowInstance.EntityName = peopleFolder.Name;

                    SuggestionsContext.SaveChanges();
                    return Status.Pending;
                });
            }
        }
    }
}
