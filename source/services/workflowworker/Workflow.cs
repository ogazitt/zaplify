using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Activities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public abstract class Workflow
    {
        public abstract string Name { get; }
        public abstract List<WorkflowState> States { get; }

        /// <summary>
        /// Default implementation for the Workflow's Process method.
        ///     Get the current state
        ///     Invoke the corresponding action (with the data object passed in)
        ///     Add any results to the Item's SuggestionsList
        ///     Move to the next state and terminate the workflow if it is at the end
        /// </summary>
        /// <param name="instance">Workflow instance</param>
        /// <param name="item">Item to process</param>
        /// <param name="data">Extra data to pass to Activity</param>
        /// <returns>true for success, false for failure</returns>
        public virtual bool Process(WorkflowInstance instance, Item item, object data)
        {
            try
            {
                // get current state, invoke action
                WorkflowState state = instance.State == null ? States[0] : States.Single(s => s.Name == instance.State);
                var activity = ActivityList.Activities[state.Activity];
                List<Guid> results = new List<Guid>();
                bool completed = activity.Function.Invoke(instance, item, data, results);

                /*
                if (results.Count > 0)
                {
                    // save the results in the suggestions sublist on the item
                    Item suggestionsList = null;
                    try
                    {
                        suggestionsList = WorkflowWorker.StorageContext.Items.Single(i => i.ParentID == item.ID && i.Name == "SuggestionsList");
                    }
                    catch (Exception)
                    {
                        suggestionsList = new Item()
                        {
                            ID = Guid.NewGuid(),
                            Name = "SuggestionsList",
                            ParentID = item.ID,
                            ItemTypeID = SystemItemTypes.NameValue,
                        };
                    }

                    try
                    {
                        Field valueField = WorkflowWorker.StorageContext.Fields.Single(f => f.ItemTypeID == SystemItemTypes.NameValue && f.Name == FieldNames.Value);
                        foreach (var suggestionID in results)
                        {
                            // create a new NameValue item with the Name being the target field for the suggestion, 
                            // and the Value being the suggestionID in the Suggesions table
                            var i = new Item()
                            {
                                ID = Guid.NewGuid(),
                                Name = action.TargetFieldName,
                                ParentID = suggestionsList.ID,
                                ItemTypeID = SystemItemTypes.NameValue,
                                FieldValues = new List<FieldValue>()
                                {
                                    new FieldValue() { FieldID = valueField.ID, ItemID = item.ID, Value = suggestionID.ToString() }
                                }
                            };
                        }

                        WorkflowWorker.StorageContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("Workflow.ProcessAction: error adding results to Item; ex: " + ex.Message);
                        return false;
                    }
                }
                 */

                // if the activity completed, advance the workflow state
                if (completed)
                {
                    // store next state and terminate the workflow if next state is null
                    instance.State = state.NextState;
                    if (instance.State == null)
                    {
                        WorkflowWorker.SuggestionsContext.WorkflowInstances.Remove(instance);
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        completed = false;
                    }
                }

                return completed;
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("Workflow.ProcessAction failed; ex: " + ex.Message);
                return false;
            }
        }
    }
}
