﻿using System;
using System.Linq;
using System.Threading;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;
using BuiltSteady.Zaplify.ServerEntities;
using System.Reflection;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class WorkflowWorker
    {
        const int timeout = 5000;  // 5 seconds

        private static UserStorageContext userContext;
        public static UserStorageContext UserContext
        {
            get
            {
                if (userContext == null)
                {
                    userContext = Storage.NewUserContext;
                }
                return userContext;
            }
        }

        private static SuggestionsStorageContext suggestionsContext;
        public static SuggestionsStorageContext SuggestionsContext
        {
            get
            {
                if (suggestionsContext == null)
                {
                    suggestionsContext = Storage.NewSuggestionsContext;
                }
                return suggestionsContext;
            }
        }

        public void Start()
        {
            MessageQueue.Initialize();

            // run an infinite loop doing the following:
            //   read a message off the queue
            //   dispatch the message appropriately
            //   sleep for the timeout period
            while (true)
            {
                try
                {
                    // get a message from the queue.  note that the Dequeue call doesn't block
                    // on the availability of a message
                    MQMessage<Guid> msg = MessageQueue.DequeueMessage<Guid>();
                    while (msg != null)
                    {
                        // make sure we get fresh database contexts to avoid EF caching stale data
                        userContext = null;
                        suggestionsContext = null;

                        // get the operation ID passed in as the message content
                        Guid operationID = msg.Content;
                        Operation operation = null;
                        try
                        {
                            operation = UserContext.Operations.Single(o => o.ID == operationID);
                        }
                        catch (Exception ex)
                        {
                            // this shouldn't happen unless there is a weird race between the Operation getting saved and this message getting processed
                            TraceLog.TraceError("WorkflowWorker: could not retrieve operation; ex: " + ex.Message);
                            throw;  // caught by the outer try block, so as to execute the sleep call 
                        }

                        Guid entityID = operation.EntityID;
                        string entityType = operation.EntityType.Trim();
                        string operationType = operation.OperationType.Trim();

                        // try to get a strongly-typed entity (item, folder, user...)
                        ServerEntity entity = null, oldEntity = null;
                        if (operationType != "DELETE")
                        {
                            try
                            {
                                switch (entityType)
                                {
                                    case "Item":
                                        Item item = UserContext.Items.Include("FieldValues").Single(i => i.ID == entityID);
                                        Item oldItem = JsonSerializer.Deserialize<Item>(operation.OldBody);
                                        entity = item;
                                        oldEntity = oldItem;
                                        break;
                                    case "Folder":
                                        Folder folder = UserContext.Folders.Single(i => i.ID == entityID);
                                        entity = folder;
                                        break;
                                    case "User":
                                        User user = UserContext.Users.Single(i => i.ID == entityID);
                                        entity = user;
                                        break;
                                    case "Suggestion":
                                        // if the entity passed in is a suggestion, this is a "meta" request - get the underlying Entity's
                                        // ID and type
                                        Suggestion suggestion = SuggestionsContext.Suggestions.Single(s => s.ID == entityID);
                                        entityID = suggestion.EntityID;
                                        entityType = suggestion.EntityType;
                                        Item suggestionItem = UserContext.Items.Include("FieldValues").Single(i => i.ID == entityID);
                                        entity = suggestionItem;
                                        operationType = "SUGGESTION";
                                        break;
                                    default:
                                        TraceLog.TraceError("WorkflowWorker: invalid Entity Type " + entityType);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                TraceLog.TraceError(String.Format("WorkflowWorker: could not retrieve {0}; ex: {1}", entityType, ex.Message));
                            }
                        }

                        // launch new workflows based on the changes to the item
                        switch (operationType)
                        {
                            case "DELETE":
                                DeleteWorkflows(entityID);
                                break;
                            case "POST":
                                StartNewWorkflows(entity);
                                ExecuteWorkflows(entity);
                                break;
                            case "PUT":
                                StartTriggerWorkflows(entity, oldEntity);
                                ExecuteWorkflows(entity);
                                break;
                            case "SUGGESTION":
                                ExecuteWorkflows(entity);
                                break;
                            default:
                                TraceLog.TraceError("WorkflowWorker: invalid Operation Type " + operationType);
                                break;
                        }

                        // remove the message from the queue
                        MessageQueue.DeleteMessage(msg.MessageRef);

                        // dequeue the next message
                        msg = MessageQueue.DequeueMessage<Guid>();
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceError("WorkflowWorker: message processing failed; ex: " + ex.Message);
                }

                // sleep for the timeout period
                Thread.Sleep(timeout);
            }
        }

        #region Helpers

        object GetFieldValue(Item item, Field field)
        {
            PropertyInfo pi = null;
            object currentValue = null;

            // get the current field value.
            // the value can either be in a strongly-typed property on the item (e.g. Name),
            // or in one of the FieldValues 
            try
            {
                // get the strongly typed property
                pi = item.GetType().GetProperty(field.Name);
                if (pi != null)
                    currentValue = pi.GetValue(item, null);
            }
            catch (Exception)
            {
                // an exception indicates this isn't a strongly typed property on the Item
                // this is NOT an error condition
            }

            // if couldn't find a strongly typed property, this property could be stored as a 
            // FieldValue on the item
            if (pi == null)
            {
                FieldValue fieldValue = null;
                // get current item's value for this field
                try
                {
                    fieldValue = item.FieldValues.Single(fv => fv.FieldName == field.Name);
                    currentValue = fieldValue.Value;
                }
                catch (Exception)
                {
                    // we can't do anything with this property since we don't have it on this item
                    // but that's ok - we can keep going
                    return null;
                }
            }

            return currentValue;
        }

        void DeleteWorkflows(Guid entityID)
        {
            try
            {
                // get all the workflow instances for this Item
                var wis = SuggestionsContext.WorkflowInstances.Where(w => w.EntityID == entityID).ToList();
                if (wis.Count > 0)
                {
                    // loop over the workflow instances and dispatch the new message
                    foreach (var instance in wis)
                    {
                        SuggestionsContext.WorkflowInstances.Remove(instance);
                    }
                    SuggestionsContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("DeleteWorkflows failed; ex: " + ex.Message);
            }
        }

        void ExecuteWorkflows(ServerEntity entity)
        {
            if (entity == null)
                return;

            try
            {
                // get all the workflow instances for this Item
                var wis = SuggestionsContext.WorkflowInstances.Where(w => w.EntityID == entity.ID).ToList();
                if (wis.Count > 0)
                {
                    // loop over the workflow instances and dispatch the new message
                    foreach (var instance in wis)
                    {
                        Workflow workflow = null;
                        if (WorkflowList.Workflows.TryGetValue(instance.WorkflowType, out workflow) == false)
                        {
                            try
                            {
                                var wt = WorkflowWorker.SuggestionsContext.WorkflowTypes.Single(t => t.Type == instance.WorkflowType);
                                workflow = JsonSerializer.Deserialize<Workflow>(wt.Definition);
                            }
                            catch (Exception ex)
                            {
                                TraceLog.TraceException("ExecuteWorkflows: could not find or deserialize workflow definition", ex);
                                continue;
                            }

                            // execute each state of the workflow until workflow is blocked for input
                            bool completed = true;
                            while (completed)
                            {
                                completed = workflow.Execute(instance, entity);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("ExecuteWorkflows: failed; ex: " + ex.Message);
            }
        }

        void StartNewWorkflows(ServerEntity entity)
        {
            if (entity == null)
                return;

            // figure out what kind of entity this is
            Item item = entity as Item;
            Folder folder = entity as Folder;
            User user = entity as User;

            if (item != null)
            {
                if (item.ItemTypeID == SystemItemTypes.Task)
                    Workflow.StartWorkflow(WorkflowNames.NewTask, item, null);
            }

            if (folder != null)
            {
            }

            if (user != null)
            {
                Workflow.StartWorkflow(WorkflowNames.NewUser, user, null);
            }
        }

        void StartTriggerWorkflows(ServerEntity entity, ServerEntity oldEntity)
        {
            if (entity == null || oldEntity == null)
                return;

            // only Item property triggers are supported at this time
            Item item = entity as Item;
            Item oldItem = oldEntity as Item;
            if (item != null)
            {
                // go through field by field, and if a field has changed, trigger the appropriate workflow 
                ItemType itemType = UserContext.ItemTypes.Include("Fields").Single(it => it.ID == item.ItemTypeID);

                foreach (var field in itemType.Fields)
                {
                    object newValue = GetFieldValue(item, field);
                    object oldValue = GetFieldValue(oldItem, field);

                    // skip fields that haven't changed
                    if (newValue == null || newValue.Equals(oldValue))
                        continue;

                    // do field-specific processing for select fields
                    switch (field.Name)
                    {
                        case FieldNames.Name:
                            //disable for now
                            //RestartWorkflow(item, WorkflowNames.NewTask);
                            break;
                    }
                }
            }
        }

        void RestartWorkflow(ServerEntity entity, string workflowType)
        {
            if (entity == null || workflowType == null)
                return;

            try
            {
                // kill all existing workflows associated with this Item
                // TODO: also need to mark the suggestions associated with this workflow as stale so that they don't
                // show up for the item again.
                var runningWFs = SuggestionsContext.WorkflowInstances.Where(wi => wi.EntityID == entity.ID).ToList();
                if (runningWFs.Count > 0)
                {
                    foreach (var wf in runningWFs)
                        SuggestionsContext.WorkflowInstances.Remove(wf);
                    SuggestionsContext.SaveChanges();
                }
                Workflow.StartWorkflow(workflowType, entity, null);
            }
            catch (Exception)
            {
                Workflow.StartWorkflow(workflowType, entity, null);
            }
        }

        #endregion Helpers
    }
}
