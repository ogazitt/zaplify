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
        /// A workflow can override this method to supply extra state to the ProcessActivity method
        /// or replace with a completely different implementation
        /// </summary>
        /// <param name="instance">Workflow instance</param>
        /// <param name="item">Item to process</param>
        /// <returns>true for success, false for failure</returns>
        public virtual bool Process(WorkflowInstance instance, Item item)
        {
            return ProcessActivity(instance, item, null);
        }

        /// <summary>
        /// This is a helper routine that does all the work to:
        ///     Get the current state
        ///     Invoke the corresponding action (with the extra state object passed in)
        ///     Add any results to the Item's SuggestionsList
        ///     Move to the next state and terminate the workflow if it is at the end
        /// </summary>
        /// <param name="instance">Workflow instance</param>
        /// <param name="item">Item to process</param>
        /// <param name="obj">Extra state to pass to Activity</param>
        /// <returns>true for success, false for failure</returns>
        protected virtual bool ProcessActivity(WorkflowInstance instance, Item item, object obj)
        {
            try
            {
                // get current state, invoke action
                WorkflowState state = instance.State == null ? States[0] : States.Single(s => s.Name == instance.State);
                var action = ActivityList.Activities[state.Activity];
                List<Guid> result = action.Function.Invoke(instance, item, obj);

                if (result != null)
                {
                    // save the result in the suggestions sublist on the item
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
                        foreach (var suggestionID in result)
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
                                    new FieldValue() { ID = Guid.NewGuid(), FieldID = valueField.ID, ItemID = item.ID, Value = suggestionID.ToString() }
                                }
                            };
                        }

                        WorkflowWorker.StorageContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        LoggingHelper.TraceError("Workflow.ProcessAction: error adding results to Item; ex: " + ex.Message);
                        return false;
                    }
                }

                // store next state and terminate the workflow if next state is null
                instance.State = state.NextState;
                if (instance.State == null)
                    WorkflowWorker.StorageContext.WorkflowInstances.Remove(instance);

                WorkflowWorker.StorageContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.TraceError("Workflow.ProcessAction failed; ex: " + ex.Message);
                return false;
            }
        }
    }
}
