using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowHost
{
    public class WorkflowHost 
    {
        public static string Me
        {
            get { return String.Concat(Environment.MachineName.ToLower(), "-", Thread.CurrentThread.ManagedThreadId.ToString()); }
        }

        /// <summary>
        /// Delete all workflow instances associated with this entity ID (the entity is already deleted)
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        public static void DeleteWorkflows(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, Guid entityID)
        {
            try
            {
                // get all the workflow instances for this Item
                var wis = suggestionsContext.WorkflowInstances.Where(w => w.EntityID == entityID).ToList();
                if (wis.Count > 0)
                {
                    // loop over the workflow instances and dispatch the new message
                    foreach (var instance in wis)
                    {
                        suggestionsContext.WorkflowInstances.Remove(instance);
                    }
                    suggestionsContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("DeleteWorkflows failed", ex);
            }
        }

        /// <summary>
        /// Execute the workflow instances associated with this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <returns>true if processing happened (and the operation doesn't need to be processed again), 
        /// false if one the workflow instances was locked (causes the message to be reprocessed)</returns>
        public static bool ExecuteWorkflows(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, ServerEntity entity)
        {
            if (entity == null)
                return true;

            List<WorkflowInstance> wis = null;

            try
            {
                // get all the workflow instances for this Item
                wis = suggestionsContext.WorkflowInstances.Where(w => w.EntityID == entity.ID).ToList();
                if (wis.Count > 0)
                {
                    // if the instance is locked by someone else, stop processing
                    // otherwise lock each of the workflow instances
                    foreach (var instance in wis)
                    {
                        if (instance.LockedBy != null && instance.LockedBy != Me)
                            return false;
                        instance.LockedBy = Me;
                    }
                    suggestionsContext.SaveChanges();

                    // reacquire the lock list and verify they were all locked by Me (if not, stop processing)
                    // projecting locks and not workflow instances to ensure that the database's lock values are returned (not from EF's cache)
                    var locks = suggestionsContext.WorkflowInstances.Where(w => w.EntityID == entity.ID).Select(w => w.LockedBy).ToList();
                    foreach (var lockedby in locks)
                        if (lockedby != Me)
                            return false;

                    // loop over the workflow instances and dispatch the new message
                    foreach (var instance in wis)
                    {
                        Workflow workflow = null;
                        try
                        {
                            var wt = suggestionsContext.WorkflowTypes.Single(t => t.Type == instance.WorkflowType);
                            workflow = JsonSerializer.Deserialize<Workflow>(wt.Definition);
                        }
                        catch (Exception ex)
                        {
                            TraceLog.TraceException("Could not find or deserialize workflow definition", ex);
                            continue;
                        }

                        // set the database contexts
                        workflow.UserContext = userContext;
                        workflow.SuggestionsContext = suggestionsContext;

                        // invoke the workflow and process steps until workflow is blocked for user input or is done
                        workflow.Run(instance, entity);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ExecuteWorkflows failed", ex);
                return true;
            }
            finally
            {
                // find and unlock all remaining workflow instances that relate to this entity
                // note that a new context is used for this - to avoid caching problems where the current thread 
                // believes it is the owner but the database says otherwise.
                var context = Storage.NewSuggestionsContext;
                wis = context.WorkflowInstances.Where(w => w.EntityID == entity.ID).ToList();
                if (wis.Count > 0)
                {
                    // unlock each of the workflow instances
                    foreach (var instance in wis)
                        if (instance.LockedBy == Me)
                            instance.LockedBy = null;
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Invoke the workflows for an operation depending on the environment.  
        /// If in Azure, enqueue a message; otherwise, invoke the workflow host directly.
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="operation"></param>
        public static void InvokeWorkflowForOperation(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, Operation operation)
        {
            if (HostEnvironment.IsAzure)
                MessageQueue.EnqueueMessage(operation.ID);
            else
                ProcessOperation(userContext, suggestionsContext, operation);
        }

        /// <summary>
        /// Process the operation based on the underlying entity type and the operation type
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="operation"></param>
        /// <returns>true if processed, false if the instance was locked and the operation needs to be replayed</returns>
        public static bool ProcessOperation(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, Operation operation)
        {
            if (operation == null)
                return true;

            if (userContext == null)
                userContext = Storage.NewUserContext;
            if (suggestionsContext == null)
                suggestionsContext = Storage.NewSuggestionsContext;

            Guid entityID = operation.EntityID;
            string entityType = operation.EntityType.Trim();
            string operationType = operation.OperationType.Trim();

            // try to get a strongly-typed entity (item, folder, user...)
            ServerEntity entity = null, oldEntity = null;
            bool process = true;

            // extract the underlying entity unless this is a delete operation (in which case it's already deleted)
            if (operationType != "DELETE")
            {
                try
                {
                    switch (entityType)
                    {
                        case "Item":
                            Item item = userContext.Items.Include("FieldValues").Single(i => i.ID == entityID);
                            Item oldItem = JsonSerializer.Deserialize<Item>(operation.OldBody);
                            entity = item;
                            oldEntity = oldItem;
                            break;
                        case "Folder":
                            Folder folder = userContext.Folders.Single(i => i.ID == entityID);
                            entity = folder;
                            break;
                        case "User":
                            User user = userContext.Users.Single(i => i.ID == entityID);
                            entity = user;
                            break;
                        case "Suggestion":
                            // if the entity passed in is a suggestion, this is a "meta" request - get the underlying Entity's
                            // ID and type
                            Suggestion suggestion = suggestionsContext.Suggestions.Single(s => s.ID == entityID);
                            entityID = suggestion.EntityID;
                            entityType = suggestion.EntityType;
                            switch (entityType)
                            {
                                case "Item":
                                    entity = userContext.Items.Include("FieldValues").Single(i => i.ID == entityID);
                                    break;
                                case "Folder":
                                    entity = userContext.Folders.Single(i => i.ID == entityID);
                                    break;
                                case "User":
                                    entity = userContext.Users.Single(i => i.ID == entityID);
                                    break;
                            }
                            operationType = "SUGGESTION";
                            break;
                        default:
                            TraceLog.TraceError("Invalid Entity Type " + entityType);
                            process = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException(String.Format("Could not retrieve {0}", entityType), ex);
                    process = false;
                }
            }

            // launch new workflows based on the changes to the item
            if (process)
            {
                switch (operationType)
                {
                    case "DELETE":
                        DeleteWorkflows(userContext, suggestionsContext, entityID);
                        return true;
                    case "POST":
                        StartNewWorkflows(userContext, suggestionsContext, entity);
                        return ExecuteWorkflows(userContext, suggestionsContext, entity);
                    case "PUT":
                        StartTriggerWorkflows(userContext, suggestionsContext, entity, oldEntity);
                        return ExecuteWorkflows(userContext, suggestionsContext, entity);
                    case "SUGGESTION":
                        return ExecuteWorkflows(userContext, suggestionsContext, entity);
                    default:
                        TraceLog.TraceError("Invalid Operation Type " + operationType);
                        return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Restart the workflows associated with an entity
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        /// <param name="workflowType"></param>
        public static void RestartWorkflow(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, ServerEntity entity, string workflowType)
        {
            if (entity == null || workflowType == null)
                return;

            try
            {
                // kill all existing workflows associated with this Item
                // TODO: also need to mark the suggestions associated with this workflow as stale so that they don't
                // show up for the item again.
                var runningWFs = suggestionsContext.WorkflowInstances.Where(wi => wi.EntityID == entity.ID).ToList();
                if (runningWFs.Count > 0)
                {
                    foreach (var wf in runningWFs)
                        suggestionsContext.WorkflowInstances.Remove(wf);
                    suggestionsContext.SaveChanges();
                }
                StartWorkflow(userContext, suggestionsContext, workflowType, entity, null);
            }
            catch (Exception)
            {
                StartWorkflow(userContext, suggestionsContext, workflowType, entity, null);
            }
        }

        /// <summary>
        /// Start NewUser/NewFolder/NewItem workflows based on the entity type
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        public static void StartNewWorkflows(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, ServerEntity entity)
        {
            if (entity == null)
                return;

            // figure out what kind of entity this is
            Item item = entity as Item;
            Folder folder = entity as Folder;
            User user = entity as User;

            // verify there are no workflow instances associated with this item yet
            var wis = suggestionsContext.WorkflowInstances.Where(wi => wi.EntityID == entity.ID).ToList();
            if (wis.Count > 0)
                return;

            if (item != null && item.IsList == false)
            {
                if (item.ItemTypeID == SystemItemTypes.Task)
                    StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewTask, item, null);
                // the Contact and Grocery new item processing happens in ItemProcessor now
                //if (item.ItemTypeID == SystemItemTypes.Contact)
                //    StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewContact, item, null);
                //if (item.ItemTypeID == SystemItemTypes.Grocery)
                //    Workflow.StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewGrocery, item, null);
            }

            if (folder != null)
            {
            }

            if (user != null)
            {
                StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewUser, user, null);
            }
        }

        /// <summary>
        /// Start workflows associated with a change in one or more of the entity's fields
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        /// <param name="oldEntity"></param>
        public static void StartTriggerWorkflows(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, ServerEntity entity, ServerEntity oldEntity)
        {
            if (entity == null || oldEntity == null)
                return;

            // only Item property triggers are supported at this time
            Item item = entity as Item;
            Item oldItem = oldEntity as Item;
            if (item != null)
            {
                // go through field by field, and if a field has changed, trigger the appropriate workflow 
                ItemType itemType = userContext.ItemTypes.Include("Fields").Single(it => it.ID == item.ItemTypeID);

                foreach (var field in itemType.Fields)
                {
                    object newValue = item.GetFieldValue(field);
                    object oldValue = item.GetFieldValue(field);

                    // skip fields that haven't changed
                    if (newValue == null || newValue.Equals(oldValue))
                        continue;

                    // do field-specific processing for select fields
                    switch (field.Name)
                    {
                        case FieldNames.Name:
                            //disable for now
                            //RestartWorkflow(userContext, suggestionsContext, item, WorkflowNames.NewTask);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Start a workflow of a certain type, passing it an entity and some instance data to start
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="type">String representing the workflow type</param>
        /// <param name="entity">Entity to associate with the workflow</param>
        /// <param name="instanceData">Instance data to pass into the workflow</param>
        public static void StartWorkflow(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, string type, ServerEntity entity, string instanceData)
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
                    TraceLog.TraceException("Could not find or deserialize workflow definition", ex);
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
                    LockedBy = WorkflowHost.Me,
                };
                suggestionsContext.WorkflowInstances.Add(instance);
                suggestionsContext.SaveChanges();

                TraceLog.TraceInfo("Starting workflow " + type);

                // invoke the workflow and process steps until workflow is blocked for user input or is done
                workflow.Run(instance, entity);

                // unlock the workflowinstance
                instance.LockedBy = null;
                suggestionsContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("StartWorkflow failed", ex);
                if (instance != null && instance.LockedBy == WorkflowHost.Me)
                {
                    // unlock the workflowinstance
                    instance.LockedBy = null;
                    suggestionsContext.SaveChanges();
                }
            }
        }
    }
}
