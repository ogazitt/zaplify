using System;
using System.Linq;
using System.Threading;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class WorkflowWorker
    {
        const int timeout = 30000;  // 30 seconds

        private static StorageContext storageContext;
        public static StorageContext StorageContext
        {
            get
            {
                if (storageContext == null)
                {
                    storageContext = Storage.NewContext;
                }
                return storageContext;
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
                    if (msg != null)
                    {
                        Guid operationID = msg.Content;
                        Operation operation = null;
                        try
                        {
                            operation = StorageContext.Operations.Single(o => o.ID == operationID);
                        }
                        catch (Exception ex)
                        {
                            LoggingHelper.TraceError("WorkflowWorker: could not retrieve operation; ex: " + ex.Message);
                            throw;  // caught by the outer try block, so as to execute the sleep call
                        }

                        Guid entityID = operation.EntityID;
                        Item item = null;

                        // try to get the item
                        try
                        {
                            item = StorageContext.Items.Single(i => i.ID == entityID);
                        }
                        catch (Exception ex)
                        {
                            LoggingHelper.TraceError("WorkflowWorker: could not retrieve item; ex: " + ex.Message);
                        }

                        // launch new workflows based on the changes to the item
                        switch (operation.OperationType)
                        {
                            case "DELETE":
                                DeleteWorkflows(entityID);
                                break;
                            case "POST":
                                StartNewWorkflows(entityID, item);
                                ExecuteWorkflows(entityID, item);
                                break;
                            case "PUT":
                                StartTriggerWorkflows(entityID, item);
                                ExecuteWorkflows(entityID, item);
                                break;
                        }

                        // remove the message from the queue
                        MessageQueue.DeleteMessage(msg.MessageRef);
                    }
                }
                catch (Exception ex)
                {
                    LoggingHelper.TraceError("WorkflowWorker: message processing failed; ex: " + ex.Message);
                }

                // sleep for the timeout period
                Thread.Sleep(timeout);
            }
        }

        void DeleteWorkflows(Guid entityID)
        {
            // get all the workflow instances for this Item
            var wis = StorageContext.WorkflowInstances.Where(w => w.ItemID == entityID).ToList();
            if (wis.Count > 0)
            {
                // loop over the workflow instances and dispatch the new message
                foreach (var instance in wis)
                {
                    StorageContext.WorkflowInstances.Remove(instance);
                }
                StorageContext.SaveChanges();
            }
        }

        void ExecuteWorkflows(Guid entityID, Item item)
        {
            // get all the workflow instances for this Item
            var wis = StorageContext.WorkflowInstances.Where(w => w.ItemID == entityID).ToList();
            if (wis.Count > 0)
            {
                // loop over the workflow instances and dispatch the new message
                foreach (var instance in wis)
                {
                    string workflowType = instance.WorkflowType;
                    Workflow workflow = WorkflowList.Workflows[workflowType];

                    // get the data from the just-completed state (if any is available)
                    object data = null;
                    try
                    {
                        data = StorageContext.
                            Suggestions.
                            Single(sugg => sugg.WorkflowInstanceID == instance.ID && sugg.State == instance.State && sugg.TimeChosen != null).
                            Value;
                    }
                    catch
                    {
                    }

                    // invoke the workflow and process steps until workflow is blocked for user input
                    bool completed = true;
                    while (completed)
                    {
                        completed = workflow.Process(instance, item, data);
                    }
                }
            }
        }

        void StartNewWorkflows(Guid entityID, Item item)
        {
            // go through field by field, and if a field is empty, trigger the appropraite workflow
            StartWorkflow(WorkflowNames.NewItem, entityID, item.Name, item);
        }

        void StartTriggerWorkflows(Guid entityID, Item item)
        {
            // go through field by field, and if a field has changed, trigger the appropriate workflow 
        }

        void StartWorkflow(string type, Guid entityID, string name, object entity)
        {
            DateTime now = DateTime.Now;

            // create the new workflow instance and store in the workflow DB
            var instance = new WorkflowInstance()
            {
                ID = Guid.NewGuid(),
                ItemID = entityID,
                WorkflowType = type,
                State = null,
                Name = name,
                Body = SerializationHelper.JsonSerialize(entity),
                Created = now,
                LastModified = now,
            };
            StorageContext.WorkflowInstances.Add(instance);
            StorageContext.SaveChanges();
        }
    }
}
