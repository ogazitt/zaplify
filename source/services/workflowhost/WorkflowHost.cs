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
        public WorkflowHost()
        {
        }

        public WorkflowHost(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext)
        {
            UserContext = userContext;
            SuggestionsContext = suggestionsContext;
        }

        public UserStorageContext UserContext { get; set; }
        public SuggestionsStorageContext SuggestionsContext { get; set; }

        public static string Me
        {
            get { return String.Concat(Environment.MachineName.ToLower(), "-", Thread.CurrentThread.ManagedThreadId.ToString()); }
        }

        /// <summary>
        /// Delete all workflow instances associated with this entity
        /// </summary>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        public void DeleteWorkflows(ServerEntity entity)
        {
            DeleteWorkflows(UserContext, SuggestionsContext, entity);
        }

        /// <summary>
        /// Delete all workflow instances associated with this entity
        /// </summary>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        public static void DeleteWorkflows(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, ServerEntity entity)
        {
            if (entity == null)
                return;
            Guid entityID = entity.ID;

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
        /// <returns>true if one the workflow instances was locked (causes the message to be reenqueued); false if the message was processed</returns>
        public bool ExecuteWorkflows(ServerEntity entity)
        {
            return ExecuteWorkflows(UserContext, SuggestionsContext, entity);
        }

        /// <summary>
        /// Execute the workflow instances associated with this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true if one the workflow instances was locked (causes the message to be reenqueued); false if the message was processed</returns>
        public static bool ExecuteWorkflows(UserStorageContext userContext, SuggestionsStorageContext suggestionsContext, ServerEntity entity)
        {
            if (entity == null)
                return false;

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
                            return true;
                        instance.LockedBy = Me;
                    }
                    suggestionsContext.SaveChanges();

                    // reacquire the lock list and verify they were all locked by Me (if not, stop processing)
                    // projecting locks and not workflow instances to ensure that the database's lock values are returned (not from EF's cache)
                    var locks = suggestionsContext.WorkflowInstances.Where(w => w.EntityID == entity.ID).Select(w => w.LockedBy).ToList();
                    foreach (var lockedby in locks)
                        if (lockedby != Me)
                            return true;

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
                            TraceLog.TraceException("ExecuteWorkflows: could not find or deserialize workflow definition", ex);
                            continue;
                        }

                        // set the database contexts
                        workflow.UserContext = userContext;
                        workflow.SuggestionsContext = suggestionsContext;

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

        /// <summary>
        /// Restart the workflows associated with an entity
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="entity"></param>
        /// <param name="workflowType"></param>
        public void RestartWorkflow(ServerEntity entity, string workflowType)
        {
            RestartWorkflow(UserContext, SuggestionsContext, entity, workflowType);
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
        public void StartNewWorkflows(ServerEntity entity)
        {
            StartNewWorkflows(UserContext, SuggestionsContext, entity);
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
                if (item.ItemTypeID == SystemItemTypes.Contact)
                    StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewContact, item, null);
                // the ShoppingItem category assignment now happens "synchronously" in the Web API code
                //if (item.ItemTypeID == SystemItemTypes.ShoppingItem)
                //    Workflow.StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewShoppingItem, item, null);
            }

            if (folder != null)
            {
                // the "New User" workflow gets triggered on the creation of a new User, but the Entity that gets 
                // sent is the People folder, because the UI wants to anchor the suggestions off of the People folder
                //if (folder.ItemTypeID == SystemItemTypes.Contact)
                //    Workflow.StartWorkflow(userContext, suggestionsContext, WorkflowNames.NewUser, folder, null);
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
        public void StartTriggerWorkflows(ServerEntity entity, ServerEntity oldEntity)
        {
            StartTriggerWorkflows(UserContext, SuggestionsContext, entity, oldEntity);
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
        public void StartWorkflow(string type, ServerEntity entity, string instanceData)
        {
            StartWorkflow(UserContext, SuggestionsContext, type, entity, instanceData);
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
                    LockedBy = WorkflowHost.Me,
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
