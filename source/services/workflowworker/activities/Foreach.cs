using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using Microsoft.IdentityModel.Protocols.OAuth;
using Microsoft.IdentityModel.Protocols.OAuth.Client;
using BuiltSteady.Zaplify.ServiceUtilities.ADGraph;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class Foreach : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    //string foreachArgument = GetInstanceData(workflowInstance, ActivityParameters.ForeachArgument);
                    string foreachOver = GetInstanceData(workflowInstance, ActivityParameters.ForeachOver);
                    string foreachBody = GetInstanceData(workflowInstance, ActivityParameters.ForeachBody);
                    if (foreachOver == null || foreachBody == null)
                    {
                        TraceLog.TraceError("Foreach: could not find ForeachOver or ForeachBody arguments");
                        return Status.Error;
                    }

                    // the ForeachOver will typically be a substitution variable - $(varname) - expand it now
                    string foreachList = FormatParameterString(workflowInstance, foreachOver);

                    // parse and iterate over the foreach string - it will be in the following format:
                    //   param1=val1,param2=val2;param1=val1,param2=val2;...
                    foreach (var item in foreachList.Split(';'))
                    {
                        // prepare the input parameters for the activity
                        foreach (var parameter in item.Split(','))
                            ProcessParameter(workflowInstance, parameter);

                        // prepare the activity itself by subtituting any input parameters
                        WorkflowActivity activity = Workflow.PrepareActivity(workflowInstance, foreachBody, UserContext, SuggestionsContext);
                        activity.Function.Invoke(workflowInstance, entity, null);
                    }

                    return Status.Complete;
                });
            }
        }

        #region Helpers

        /// <summary>
        /// Parses out a parameter definition in the form of 'param=value' and stores in InstanceData with the param as the key 
        /// </summary>
        /// <param name="instance">Workflow instance to operate over</param>
        /// <param name="parameter">Parameter in 'key=value' format</param>
        private void ProcessParameter(WorkflowInstance instance, string parameter)
        {
            if (parameter == null)
                return;
            int index = parameter.IndexOf('=');
            if (index < 0)
                return;
            var name = parameter.Substring(0, index).Trim();
            var value = parameter.Substring(index + 1).Trim();
            StoreInstanceData(instance, name, value);
        }

        #endregion Helpers
    }
}
