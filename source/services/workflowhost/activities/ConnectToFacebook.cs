using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
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

                    // create a single suggestion to connect to FB
                    var sugg = new Suggestion()
                    {
                        ID = Guid.NewGuid(),
                        EntityID = entity.ID,
                        EntityType = entity.GetType().Name,
                        WorkflowType = workflowInstance.WorkflowType,
                        WorkflowInstanceID = workflowInstance.ID,
                        State = workflowInstance.State,
                        SuggestionType = SuggestionType,
                        DisplayName = "Connect to Facebook",
                        GroupDisplayName = "Get Connected",
                        SortOrder = 1,
                    };
                    SuggestionsContext.Suggestions.Add(sugg);
                    SuggestionsContext.SaveChanges();
                    return Status.Pending;
                });
            }
        }
    }
}
