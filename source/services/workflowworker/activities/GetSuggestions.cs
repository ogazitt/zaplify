using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetSuggestions : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetSuggestions; } }
        public override string TargetFieldName { get { return "Suggestions"; } }
        public override Func<WorkflowInstance, Item, object, List<Guid>> Function
        {
            get
            {
                return ((workflowInstance, item, state) =>
                {
                    try
                    {
                        return null;
                    }
                    catch (Exception ex)
                    {
                        LoggingHelper.TraceError("GetSuggestions Activity failed; ex: " + ex.Message);
                    }
                    return null;
                });
            }
        }
    }
}
