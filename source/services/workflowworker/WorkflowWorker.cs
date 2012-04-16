using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class WorkflowWorker : IWorker
    {
        public int Timeout { get { return 5000; } } // 5 seconds

        private UserStorageContext userContext;
        public UserStorageContext UserContext
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

        private SuggestionsStorageContext suggestionsContext;
        public SuggestionsStorageContext SuggestionsContext
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

        public static string Me
        {
            get { return String.Concat(Environment.MachineName.ToLower(), "-", Thread.CurrentThread.ManagedThreadId.ToString()); }
        }

        public void Start()
        {
            MessageQueue.Initialize();

            // run an infinite loop doing the following:
            //   read a message off the queue
            //   dispatch the message appropriately
            //   sleep for the timeout period
            Guid lastOperationID = Guid.Empty;
            while (true)
            {
                try
                {
                    // get a message from the queue.  note that the Dequeue call doesn't block
                    // on the availability of a message
                    MQMessage<Guid> msg = MessageQueue.DequeueMessage<Guid>();
                    while (msg != null)
                    {
                        bool reenqueue = false;

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
                            TraceLog.TraceException("WorkflowWorker: could not retrieve operation", ex);

                            // there are two possibilities - one is a transient issue with the database connection (e.g. DB wasn't initialized, or a weird race condition between 
                            // the DB value not getting stored before the workflow message gets dequeued).  in this case we let message expire and get dequeued again in the future.
                            // the other case is a poison message (one that points to an operation that doesn't exist in the database).  
                            // so if we've seen this message before, we want to delete it.
                            if (lastOperationID == operationID)
                                MessageQueue.DeleteMessage(msg.MessageRef);
                            else
                                lastOperationID = operationID;

                            throw;  // caught by the outer try block so as to hit the sleep call
                        }

                        Guid entityID = operation.EntityID;
                        string entityType = operation.EntityType.Trim();
                        string operationType = operation.OperationType.Trim();

                        // try to get a strongly-typed entity (item, folder, user...)
                        ServerEntity entity = null, oldEntity = null;
                        bool process = true;
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
                                        process = false;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                TraceLog.TraceException(String.Format("WorkflowWorker: could not retrieve {0}", entityType), ex);
                                process = false;
                            }
                        }

                        // launch new workflows based on the changes to the item

                        if (process)
                        {
                            switch (operationType)
                            {
                                case "DELETE":
                                    DeleteWorkflows(entityID);
                                    break;
                                case "POST":
                                    StartNewWorkflows(entity);
                                    reenqueue = ExecuteWorkflows(entity);
                                    break;
                                case "PUT":
                                    StartTriggerWorkflows(entity, oldEntity);
                                    reenqueue = ExecuteWorkflows(entity);
                                    break;
                                case "SUGGESTION":
                                    reenqueue = ExecuteWorkflows(entity);
                                    break;
                                default:
                                    TraceLog.TraceError("WorkflowWorker: invalid Operation Type " + operationType);
                                    break;
                            }
                        }

                        // remove the message from the queue
                        bool deleted = MessageQueue.DeleteMessage(msg.MessageRef);

                        // reenqueue and sleep if the processing failed
                        if (deleted && reenqueue)
                        {
                            Thread.Sleep(Timeout);
                            MessageQueue.EnqueueMessage(operationID);
                        }

                        // dequeue the next message
                        msg = MessageQueue.DequeueMessage<Guid>();
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("WorkflowWorker: message processing failed", ex);
                }

                // sleep for the timeout period
                Thread.Sleep(Timeout);
            }
        }

        #region Helpers

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
                TraceLog.TraceException("DeleteWorkflows failed", ex);
            }
        }

        /// <summary>
        /// Execute the workflow instances associated with this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true if one the workflow instances was locked (causes the message to be reenqueued); false if the message was processed</returns>
        bool ExecuteWorkflows(ServerEntity entity)
        {
            if (entity == null)
                return false;

            List<WorkflowInstance> wis = null;

            try
            {
                // get all the workflow instances for this Item
                wis = SuggestionsContext.WorkflowInstances.Where(w => w.EntityID == entity.ID).ToList();
                if (wis.Count > 0)
                {
                    // if the instance is locked by someone else, stop processing
                    // otherwise lock each of the workflow instances
                    foreach (var instance in wis)
                    {
                        if (instance.LockedBy != null && instance.LockedBy != Me)
                            return true;
                        instance.LockedBy = Me;
                    }
                    SuggestionsContext.SaveChanges();

                    // reacquire the lock list and verify they were all locked by Me (if not, stop processing)
                    // projecting locks and not workflow instances to ensure that the database's lock values are returned (not from EF's cache)
                    var locks = SuggestionsContext.WorkflowInstances.Where(w => w.EntityID == entity.ID).Select(w => w.LockedBy).ToList();
                    foreach (var lockedby in locks)
                        if (lockedby != Me)
                            return true;

                    // loop over the workflow instances and dispatch the new message
                    foreach (var instance in wis)
                    {
                        Workflow workflow = null;
                        if (WorkflowList.Workflows.TryGetValue(instance.WorkflowType, out workflow) == false)
                        {
                            try
                            {
                                var wt = SuggestionsContext.WorkflowTypes.Single(t => t.Type == instance.WorkflowType);
                                workflow = JsonSerializer.Deserialize<Workflow>(wt.Definition);
                            }
                            catch (Exception ex)
                            {
                                TraceLog.TraceException("ExecuteWorkflows: could not find or deserialize workflow definition", ex);
                                continue;
                            }
                        }

                        // set the database contexts
                        workflow.UserContext = UserContext;
                        workflow.SuggestionsContext = SuggestionsContext;

                        // invoke the workflow and process steps until workflow is blocked for user input or is done
                        workflow.Run(instance, entity);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ExecuteWorkflows: failed", ex);
                return false;
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
                Workflow.StartWorkflow(workflowType, entity, null, UserContext, SuggestionsContext);
            }
            catch (Exception)
            {
                Workflow.StartWorkflow(workflowType, entity, null, UserContext, SuggestionsContext);
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

            // verify there are no workflow instances associated with this item yet
            var wis = SuggestionsContext.WorkflowInstances.Where(wi => wi.EntityID == entity.ID).ToList();
            if (wis.Count > 0)
                return;

            if (item != null)
            {
                if (item.ItemTypeID == SystemItemTypes.Task)
                    Workflow.StartWorkflow(WorkflowNames.NewTask, item, null, UserContext, SuggestionsContext);
            }

            if (folder != null)
            {
            }

            if (user != null)
            {
                Workflow.StartWorkflow(WorkflowNames.NewUser, user, null, UserContext, SuggestionsContext);
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

        #endregion Helpers
    }
}
