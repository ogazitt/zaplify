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

        public UserStorageContext UserContext { get; set; }
        public SuggestionsStorageContext SuggestionsContext { get; set; }

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
                data = SuggestionsContext.Suggestions.Where(sugg => sugg.WorkflowInstanceID == instance.ID && sugg.State == instance.State).ToList();
                
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
                TraceLog.TraceInfo(String.Format("Workflow.Process: workflow {0} entering state {1}", instance.WorkflowType, instance.State));
                WorkflowState state = States.Single(s => s.Name == instance.State);
                var activity = PrepareActivity(instance, state.Activity, UserContext, SuggestionsContext);
                if (activity == null)
                {
                    TraceLog.TraceError("Process: could not find or prepare Activity");
                    return WorkflowActivity.Status.Error;
                }

                // invoke the activity
                TraceLog.TraceInfo(String.Format("Workflow.Process: workflow {0} invoking activity {1}", instance.WorkflowType, activity.Name));
                var status = activity.Function.Invoke(instance, entity, data);
                TraceLog.TraceInfo(String.Format("Workflow.Process: workflow {0}: activity {1} returned status {2}", instance.WorkflowType, activity.Name, status.ToString()));
                instance.LastModified = DateTime.Now;

                // if the activity completed, advance the workflow state
                if (status == WorkflowActivity.Status.Complete)
                {
                    // store next state and terminate the workflow if next state is null
                    instance.State = state.NextState;
                    if (instance.State == null)
                    {
                        status = WorkflowActivity.Status.WorkflowDone;
                        TraceLog.TraceInfo(String.Format("Workflow.Process: workflow {0} is done", instance.WorkflowType));
                    }
                }
                SuggestionsContext.SaveChanges();

                return status;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Workflow.Process failed", ex);
                return WorkflowActivity.Status.Error;
            }
        }

        /// <summary>
        /// Prepare an activity for execution
        /// 1. Parse out the activity name
        /// 2. Parse out each of the arguments and store them as instance data
        /// </summary>
        /// <param name="instance">workflow instance to operate over</param>
        /// <param name="activityNameAndArguments">string containing activity names and arguments</param>
        /// <returns>Activity ready to execute</returns>
        public static WorkflowActivity PrepareActivity(WorkflowInstance instance, string activityNameAndArguments, UserStorageContext userContext, SuggestionsStorageContext suggestionsContext)
        {
            string activityName = activityNameAndArguments;
            int paramIndex = activityNameAndArguments.IndexOf('(');
            if (paramIndex >= 0)
            {
                activityName = activityNameAndArguments.Substring(0, paramIndex);
                string args = activityNameAndArguments.Substring(paramIndex + 1);
                // trim exactly one right paren if it is there
                if (args.LastIndexOf(')') == args.Length - 1)
                    args = args.Substring(0, args.Length - 1);

                // process each one of the parameters, adding the name and value to the InstanceData
                var parameters = args.Split(',');
                foreach (var param in parameters)
                    ProcessParameter(instance, param);

                // save the instance data bag 
                suggestionsContext.SaveChanges();
            }

            Type activityType;
            if (ActivityList.Activities.TryGetValue(activityName, out activityType))
            {
                try
                {
                    WorkflowActivity activity = Activator.CreateInstance(activityType) as WorkflowActivity;
                    activity.UserContext = userContext;
                    activity.SuggestionsContext = suggestionsContext;
                    return activity;
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException(String.Format("PrepareActivity: Could not create instance of {0} activity", activityName), ex);
                    return null;
                }
            }

            TraceLog.TraceError(String.Format("PrepareActivity: Activity {0} not found", activityName));
            return null;
        }

        /// <summary>
        /// Run a workflow until it is blocked for user input or is terminated
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        public void Run(WorkflowInstance instance, ServerEntity entity)
        {
            TraceLog.TraceInfo("Workflow.Run: running workflow " + instance.WorkflowType);
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
                TraceLog.TraceException("Workflow.Run: workflow execution failed", ex);
            }

            // if the workflow is done or experienced a fatal error, terminate it
            if (status == WorkflowActivity.Status.WorkflowDone ||
                status == WorkflowActivity.Status.Error)
            {
                try
                {
                    SuggestionsContext.WorkflowInstances.Remove(instance);
                    SuggestionsContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Workflow.Run: could not remove workflow instance", ex);
                }
            }
        }

        public static void StartWorkflow(string type, ServerEntity entity, string instanceData, UserStorageContext userContext, SuggestionsStorageContext suggestionsContext)
        {
            WorkflowInstance instance = null;
            try
            {
                Workflow workflow = null;
                if (WorkflowList.Workflows.TryGetValue(type, out workflow) == false)
                {
                    // get the workflow definition out of the database
                    try
                    {
                        var wt = suggestionsContext.WorkflowTypes.Single(t => t.Type == type);
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

                // store the database contexts
                workflow.UserContext = userContext;
                workflow.SuggestionsContext = suggestionsContext;

                // create the new workflow instance and store in the workflow DB
                DateTime now = DateTime.Now;
                instance = new WorkflowInstance()
                {
                    ID = Guid.NewGuid(),
                    EntityID = entity.ID,
                    EntityName = entity.Name,
                    WorkflowType = type,
                    State = workflow.States[0].Name,
                    InstanceData = instanceData ?? "{}",
                    Created = now,
                    LastModified = now,
                    LockedBy = WorkflowWorker.Me,
                };
                suggestionsContext.WorkflowInstances.Add(instance);
                suggestionsContext.SaveChanges();

                TraceLog.TraceInfo("Workflow.StartWorkflow: starting workflow " + type);

                // invoke the workflow and process steps until workflow is blocked for user input or is done
                workflow.Run(instance, entity);

                // unlock the workflowinstance
                instance.LockedBy = null;
                suggestionsContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("StartWorkflow failed", ex);
                if (instance != null && instance.LockedBy == WorkflowWorker.Me)
                {
                    // unlock the workflowinstance
                    instance.LockedBy = null;
                    suggestionsContext.SaveChanges();
                }
            }
        }

        #region Helpers

        private static void ProcessParameter(WorkflowInstance instance, string parameter)
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

        /// <summary>
        /// Store a value for a key on the instance data bag
        /// </summary>
        /// <param name="workflowInstance">Instance to retrieve the data from</param>
        /// <param name="key">Key to store under</param>
        /// <param name="data">Data to store under the key</param>
        private static void StoreInstanceData(WorkflowInstance workflowInstance, string key, string data)
        {
            JsonValue dict = JsonValue.Parse(workflowInstance.InstanceData);
            dict[key] = data;
            workflowInstance.InstanceData = dict.ToString();
        }

        #endregion Helpers
    }
}
