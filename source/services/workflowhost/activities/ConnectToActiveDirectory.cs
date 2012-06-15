using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class ConnectToActiveDirectory : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Get Connected"; } }
        public override string SuggestionType { get { return SuggestionTypes.GetADConsent; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    // check for user selection
                    if (data != null)
                        return ProcessActivityData(workflowInstance, data);

                    // create a single suggestion to connect to AD
                    var sugg = new Suggestion()
                    {
                        ID = Guid.NewGuid(),
                        EntityID = entity.ID,
                        EntityType = entity.GetType().Name,
                        WorkflowType = workflowInstance.WorkflowType,
                        WorkflowInstanceID = workflowInstance.ID,
                        State = workflowInstance.State,
                        SuggestionType = SuggestionType,
                        DisplayName = "Connect to Active Directory",
                        GroupDisplayName = "Get Connected",
                        SortOrder = 3,
                    };
                    SuggestionsContext.Suggestions.Add(sugg);
                    SuggestionsContext.SaveChanges();
                    return Status.Pending;
                });
            }
        }
    }
}
