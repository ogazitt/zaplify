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
                        Guid itemID = msg.Content;
                        Item item = null;

                        // try to get the item
                        try
                        {
                            item = StorageContext.Items.Single(i => i.ID == itemID);
                        }
                        catch (Exception ex)
                        {
                            LoggingHelper.TraceError("WorkflowWorker: could not retrieve item; ex: " + ex.Message);
                        }

                        // get all the workflow instances for this Item
                        var wis = StorageContext.WorkflowInstances.Where(w => w.ItemID == itemID).ToList();
                        if (wis.Count > 0)
                        {
                            // loop over the workflow instances and dispatch the new message
                            foreach (var instance in wis)
                            {
                                string workflowType = instance.WorkflowType;
                                Workflow workflow = WorkflowList.Workflows[workflowType];

                                // invoke the workflow and process steps until workflow is blocked for user input
                                bool completed = true;
                                while (completed)
                                {
                                    completed = workflow.Process(instance, item);
                                }
                            }
                        }
                        else
                        {
                            // no workflows present - start the New Item workflow
                            DateTime now = DateTime.Now;

                            // create the new workflow instance and store in the workflow DB
                            var instance = new WorkflowInstance() 
                            { 
                                ID = Guid.NewGuid(),
                                ItemID = itemID,
                                WorkflowType = WorkflowNames.NewItem,
                                State = null,
                                Name = item.Name,
                                Body = SerializationHelper.JsonSerialize(item),
                                Created = now,
                                LastModified = now,
                            };
                            StorageContext.WorkflowInstances.Add(instance);
                            StorageContext.SaveChanges();

                            // invoke the workflow and process steps until workflow is blocked for user input
                            Workflow workflow = WorkflowList.Workflows[WorkflowNames.NewItem];
                            bool completed = true;
                            while (completed)
                            {
                                completed = workflow.Process(instance, item);
                            }
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
    }
}
