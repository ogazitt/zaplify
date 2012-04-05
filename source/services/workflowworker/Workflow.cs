using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class Workflow
    {
        public virtual List<WorkflowState> States { get; set; }

        public const string LastStateData = "LastStateData";

        /// <summary>
        /// This is the typical way to execute a workflow.  This implementation
        /// will retrieve any data (e.g. user selection, or a result of a previous Activity)
        /// and pass it into the Process method.
        /// </summary>
        /// <param name="instance">Workflow instance</param>
        /// <param name="entity">Entity to process</param>
        /// <returns>true if completed, false if not</returns>
        public virtual WorkflowActivity.Status Execute(WorkflowInstance instance, ServerEntity entity)
        {
            // get the data for the current state (if any is available)
            // this data will be fed into the Process method as its argument
            List<Suggestion> data = null;
            try
            {
                data = WorkflowWorker.
                    SuggestionsContext.
                    Suggestions.
                    Where(sugg => sugg.WorkflowInstanceID == instance.ID && sugg.State == instance.State).ToList();
                
                // if there is no suggestion data, indicate this with a null reference instead
                if (data.Count == 0)
                    data = null;
            }
            catch
            {
                // this is not an error state - the user may not have chosen a suggestion
            }

            // return the result of processing the state with the data
            return Process(instance, entity, data);
        }

        /// <summary>
        /// Default implementation for the Workflow's Process method.
        ///     Get the current state
        ///     Invoke the corresponding action (with the data object passed in)
        ///     Add any results to the Item's SuggestionsList
        ///     Move to the next state and terminate the workflow if it is at the end
        /// </summary>
        /// <param name="instance">Workflow instance</param>
        /// <param name="entity">Entity to process</param>
        /// <param name="data">Extra data to pass to Activity</param>
        /// <returns>true if completed, false if not</returns>
        protected virtual WorkflowActivity.Status Process(WorkflowInstance instance, ServerEntity entity, object data) 
        {
            try
            {
                // get current state and corresponding activity
                WorkflowState state = States.Single(s => s.Name == instance.State);
                var activity = PrepareActivity(instance, state.Activity);
                if (activity == null)
                {
                    TraceLog.TraceError("Process: could not find or prepare Activity");
                    return WorkflowActivity.Status.Error;
                }

                // invoke the activity
                var status = activity.Function.Invoke(instance, entity, data);
                instance.LastModified = DateTime.Now;

                // if the activity completed, advance the workflow state
                if (status == WorkflowActivity.Status.Complete)
                {
                    // store next state and terminate the workflow if next state is null
                    instance.State = state.NextState;
                    if (instance.State == null)
                        status = WorkflowActivity.Status.WorkflowDone;
                }
                WorkflowWorker.SuggestionsContext.SaveChanges();

                return status;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Workflow.Process failed", ex);
                return WorkflowActivity.Status.Error;
            }
        }

        /// <summary>
        /// Run a workflow until it is blocked for user input or is terminated
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        public void Run(WorkflowInstance instance, ServerEntity entity)
        {
            var status = WorkflowActivity.Status.Complete;
            try
            {
                // invoke the workflow and process steps until workflow is blocked for user input
                while (status == WorkflowActivity.Status.Complete)
                {
                    status = Execute(instance, entity);
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Run: workflow execution failed", ex);
            }

            // if the workflow is done or experienced a fatal error, terminate it
            if (status == WorkflowActivity.Status.WorkflowDone ||
                status == WorkflowActivity.Status.Error)
            {
                try
                {
                    WorkflowWorker.SuggestionsContext.WorkflowInstances.Remove(instance);
                    WorkflowWorker.SuggestionsContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Run: could not remove workflow instance", ex);
                }
            }
        }

        public static void StartWorkflow(string type, ServerEntity entity, string instanceData)
        {
            try
            {
                Workflow workflow = null;
                if (WorkflowList.Workflows.TryGetValue(type, out workflow) == false)
                {
                    // get the workflow definition out of the database
                    try
                    {
                        var wt = WorkflowWorker.SuggestionsContext.WorkflowTypes.Single(t => t.Type == type);
                        workflow = JsonSerializer.Deserialize<Workflow>(wt.Definition);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("StartWorkflow: could not find or deserialize workflow definition", ex);
                        return;
                    }
                }
                // don't start a workflow with no states
                if (workflow.States.Count == 0)
                    return;

                // create the new workflow instance and store in the workflow DB
                DateTime now = DateTime.Now;
                var instance = new WorkflowInstance()
                {
                    ID = Guid.NewGuid(),
                    EntityID = entity.ID,
                    EntityName = entity.Name,
                    WorkflowType = type,
                    State = workflow.States[0].Name,
                    InstanceData = instanceData ?? "{}",
                    Created = now,
                    LastModified = now,
                };
                WorkflowWorker.SuggestionsContext.WorkflowInstances.Add(instance);
                WorkflowWorker.SuggestionsContext.SaveChanges();

                // invoke the workflow and process steps until workflow is blocked for user input or is done
                workflow.Run(instance, entity);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("StartWorkflow failed", ex);
            }
        }

        #region Helpers

        private WorkflowActivity PrepareActivity(WorkflowInstance instance, string activityNameAndArguments)
        {
            string activityName = activityNameAndArguments;
            int paramIndex = activityNameAndArguments.IndexOf('(');
            if (paramIndex >= 0)
            {
                activityName = activityNameAndArguments.Substring(0, paramIndex);
                string args = activityNameAndArguments.Substring(paramIndex + 1);
                args = args.TrimEnd(')');
                
                // process each one of the parameters, adding the name and value to the InstanceData
                var parameters = args.Split(',');
                foreach (var param in parameters)
                    ProcessParameter(instance, param);

                // save the instance data bag 
                WorkflowWorker.SuggestionsContext.SaveChanges();
            }

            WorkflowActivity activity = null;
            if (ActivityList.Activities.TryGetValue(activityName, out activity))
                return activity;
            return null;
        }

        private void ProcessParameter(WorkflowInstance instance, string parameter)
        {
            if (parameter == null)
                return;
            var strs = parameter.Split('=');
            if (strs.Length != 2)
                return;
            var name = strs[0].Trim();
            var value = strs[1].Trim();
            StoreInstanceData(instance, name, value);
        }

        /// <summary>
        /// Store a value for a key on the instance data bag
        /// </summary>
        /// <param name="workflowInstance">Instance to retrieve the data from</param>
        /// <param name="key">Key to store under</param>
        /// <param name="data">Data to store under the key</param>
        private void StoreInstanceData(WorkflowInstance workflowInstance, string key, string data)
        {
            JsonValue dict = JsonValue.Parse(workflowInstance.InstanceData);
            dict[key] = data;
            workflowInstance.InstanceData = dict.ToString();
        }

        #endregion Helpers
    }
}
