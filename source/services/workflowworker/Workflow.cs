using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    // some well-known workflow names
    public class WorkflowNames
    {
        public const string NewContact = "New Contact";
        public const string NewFolder = "New Folder";
        public const string NewShoppingItem = "New ShoppingItem";
        public const string NewTask = "New Task";
        public const string NewUser = "New User";
    }

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
                //var activity = PrepareActivity(instance, state.Activity, UserContext, SuggestionsContext);
                
                WorkflowActivity activity = null;
                if (state.Activity != null)
                    activity = WorkflowActivity.CreateActivity(state.Activity);
                else if (state.ActivityDefinition != null)
                    activity = WorkflowActivity.CreateActivity(state.ActivityDefinition, instance);

                if (activity == null)
                {
                    TraceLog.TraceError("Process: could not find or prepare Activity");
                    return WorkflowActivity.Status.Error;
                }
                else
                {
                    activity.UserContext = UserContext;
                    activity.SuggestionsContext = SuggestionsContext;
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
    }
}
