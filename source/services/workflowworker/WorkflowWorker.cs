using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowHost;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class WorkflowWorker : IWorker
    {
        private int? timeout;
        public int Timeout 
        { 
            get 
            {
                if (!timeout.HasValue)
                {
                    timeout = ConfigurationSettings.GetAsNullableInt(HostEnvironment.WorkflowWorkerTimeoutConfigKey);
                    if (timeout == null)
                        timeout = 5000;  // default to 5 seconds
                    else
                        timeout *= 1000;  // convert to ms
                }
                return timeout.Value;
            }
        }

        public void Start()
        {
            MessageQueue.Initialize();

            // run an infinite loop doing the following:
            //   read a message off the queue
            //   dispatch the message appropriately
            //   remove the message but reenqueue it if processing failed
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
                        // make sure we get fresh database contexts to avoid EF caching stale data
                        var UserContext = Storage.NewUserContext;
                        var SuggestionsContext = Storage.NewSuggestionsContext;

                        // get the operation ID passed in as the message content
                        Guid operationID = msg.Content;
                        Operation operation = null;
                        try
                        {
                            operation = UserContext.Operations.Single(o => o.ID == operationID);
                        }
                        catch (Exception ex)
                        {
                            TraceLog.TraceException("Could not retrieve operation", ex);

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

                        // process the operation (invoking workflows as necessary)
                        bool processed = WorkflowHost.WorkflowHost.ProcessOperation(UserContext, SuggestionsContext, operation);

                        // remove the message from the queue
                        bool deleted = MessageQueue.DeleteMessage(msg.MessageRef);

                        // reenqueue and sleep if the processing failed
                        if (deleted && !processed)
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
                    TraceLog.TraceException("Message processing failed", ex);
                }

                // sleep for the timeout period
                Thread.Sleep(Timeout);
            }
        }
    }
}
