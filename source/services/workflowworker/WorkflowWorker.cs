using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using WFH = BuiltSteady.Zaplify.WorkflowHost;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class WorkflowWorker : IWorker
    {
        public int Timeout { get { return 5000; } } // 5 seconds

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
                        var UserContext = Storage.NewUserContext;
                        var SuggestionsContext = Storage.NewSuggestionsContext;

                        // create a new workflow host
                        var workflowHost = new WFH.WorkflowHost(UserContext, SuggestionsContext);

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
                                    switch (entityType)
                                    {
                                        case "Item":
                                            entity = UserContext.Items.Include("FieldValues").Single(i => i.ID == entityID);
                                            break;
                                        case "Folder":
                                            entity = UserContext.Folders.Single(i => i.ID == entityID);
                                            break;
                                        case "User":
                                            entity = UserContext.Users.Single(i => i.ID == entityID);
                                            break;
                                    }
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

                        // launch new workflows based on the changes to the item
                        if (process)
                        {
                            switch (operationType)
                            {
                                case "DELETE":
                                    workflowHost.DeleteWorkflows(entity);
                                    break;
                                case "POST":
                                    workflowHost.StartNewWorkflows(entity);
                                    reenqueue = workflowHost.ExecuteWorkflows(entity);
                                    break;
                                case "PUT":
                                    workflowHost.StartTriggerWorkflows(entity, oldEntity);
                                    reenqueue = workflowHost.ExecuteWorkflows(entity);
                                    break;
                                case "SUGGESTION":
                                    reenqueue = workflowHost.ExecuteWorkflows(entity);
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
    }
}
