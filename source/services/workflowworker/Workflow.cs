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
    public abstract class Workflow
    {
        public abstract string Name { get; }
        public abstract List<WorkflowState> States { get; }

        /// <summary>
        /// This is the typical way to execute a workflow.  This implementation
        /// will retrieve any data (e.g. user selection, or a result of a previous Activity)
        /// and pass it into the Process method.
        /// </summary>
        /// <param name="instance">Workflow instance</param>
        /// <param name="entity">Entity to process</param>
        /// <returns>true if completed, false if not</returns>
        public virtual bool Execute(WorkflowInstance instance, ServerEntity entity)
        {
            // get the data for the current state (if any is available)
            // this data will be fed into the Process method as its argument
            object data = null;
            try
            {
                data = WorkflowWorker.
                    SuggestionsContext.
                    Suggestions.
                    Single(sugg => sugg.WorkflowInstanceID == instance.ID && sugg.State == instance.State && sugg.TimeSelected != null).
                    Value;
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
        protected virtual bool Process(WorkflowInstance instance, ServerEntity entity, object data) 
        {
            try
            {
                // get current state, invoke action
                WorkflowState state = States.Single(s => s.Name == instance.State);
                var activity = ActivityList.Activities[state.Activity];
                List<Guid> results = new List<Guid>();
                bool completed = activity.Function.Invoke(instance, entity, data, results);
                instance.LastModified = DateTime.Now;

                // if the activity completed, advance the workflow state
                if (completed)
                {
                    // store next state and terminate the workflow if next state is null
                    instance.State = state.NextState;
                    if (instance.State == null)
                    {
                        WorkflowWorker.SuggestionsContext.WorkflowInstances.Remove(instance);
                        completed = false;
                    }
                    WorkflowWorker.SuggestionsContext.SaveChanges();
                }

                return completed;
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("Workflow.ProcessAction failed; ex: " + ex.Message);
                return false;
            }
        }

        public static void StartWorkflow(string type, ServerEntity entity)
        {
            try
            {
                // don't start a workflow with no states
                Workflow workflow = WorkflowList.Workflows[type];
                if (workflow.States.Count == 0)
                    return;

                // create the new workflow instance and store in the workflow DB
                DateTime now = DateTime.Now;
                var instance = new WorkflowInstance()
                {
                    ID = Guid.NewGuid(),
                    EntityID = entity.ID,
                    WorkflowType = type,
                    State = workflow.States[0].Name,
                    Name = entity.Name,
                    InstanceData = "",
                    Created = now,
                    LastModified = now,
                };
                WorkflowWorker.SuggestionsContext.WorkflowInstances.Add(instance);
                WorkflowWorker.SuggestionsContext.SaveChanges();

                // invoke the workflow and process steps until workflow is blocked for user input
                bool completed = true;
                while (completed)
                {
                    completed = workflow.Execute(instance, entity);
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("StartWorkflow failed; ex: " + ex.Message);
            }
        }
    }
}
