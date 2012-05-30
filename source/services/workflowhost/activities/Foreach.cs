using System;
using Newtonsoft.Json.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class Foreach : WorkflowActivity
    {
        public class ActivityParameters
        {
            public const string List = "List";
            public const string Activity = "Activity";
        }

        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    string foreachList = null; 
                    string foreachBody = null; 
                    if (InputParameters.TryGetValue(ActivityParameters.List, out foreachList) == false ||
                        InputParameters.TryGetValue(ActivityParameters.Activity, out foreachBody) == false)
                    {
                        TraceLog.TraceError("Foreach: could not find ForeachOver or ForeachBody arguments");
                        return Status.Error;
                    }

                    // check for an empty foreach list
                    if (String.IsNullOrEmpty(foreachList))
                    {
                        TraceLog.TraceInfo("Foreach: no elements in list");
                        return Status.Complete;
                    }

                    try
                    {
                        // parse the body definition string into a JSON object containing the activity definition
                        JObject foreachBodyActivityDefinition = JObject.Parse(foreachBody);

                        // the ForeachOver will typically be a substitution variable - $(varname) - expand it now
                        //string foreachList = FormatParameterString(workflowInstance, foreachOver);

                        // parse and iterate over the foreach list - it will be in the following (array of objects) format:
                        //   [ { "param1": "val1", "param2": "val2" }, { ... } ]
                        var list = JArray.Parse(foreachList);
                        foreach (JObject item in list)
                        {
                            // prepare the current values of the variables for the activity (these will be picked up by input parameters)
                            foreach (var parameter in item)
                                StoreInstanceData(workflowInstance, parameter.Key, parameter.Value.ToString());

                            // prepare the activity itself by subtituting any input parameters
                            //WorkflowActivity activity = Workflow.PrepareActivity(workflowInstance, foreachBody, UserContext, SuggestionsContext);
                            // create and invoke the activity (the input parameters will be bound to the variables during this process)
                            var activity = WorkflowActivity.CreateActivity(foreachBodyActivityDefinition, workflowInstance);
                            activity.UserContext = UserContext;
                            activity.SuggestionsContext = SuggestionsContext;
                            activity.Function.Invoke(workflowInstance, entity, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("Foreach: processing failed", ex);
                        return Status.Error;
                    }

                    return Status.Complete;
                });
            }
        }
    }
}
