using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ApplicationServer.Http;
using System.Net.Http;
using System.Net;
using System.Reflection;
using TaskStoreWeb.Helpers;
using TaskStoreWeb.Models;
using System.Web.Configuration;
using TaskStoreServerEntities;

namespace TaskStoreWeb.Resources
{
    [ServiceContract]
    [LogMessages]
    public class OperationResource
    {
        private TaskStore TaskStore
        {
            get
            {
                return new TaskStore();
            }
        }

        /// <summary>
        /// Delete the Operation 
        /// </summary>
        /// <param name="id">id for the operation to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Operation> DeleteOperation(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Operation>(req, code);  // user not authenticated

            // get the operation from the message body
            Operation clientOperation = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(Operation)) as Operation;

            // make sure the ID's match
            if (clientOperation.ID != id)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);


            // if the requested operation does not belong to the authenticated user, return 403 Forbidden
            if (clientOperation.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Forbidden);

            // get the operation to be deleted
            try
            {
                Operation requestedOperation = taskstore.Operations.Single<Operation>(t => t.ID == id);
                taskstore.Operations.Remove(requestedOperation);
                int rows = taskstore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<Operation>(req, requestedOperation, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // operation not found - it may have been deleted by someone else.  Return 200 OK.
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Get the Operation for a operation id
        /// </summary>
        /// <param name="id">id for the operation to return</param>
        /// <returns>Operation information</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Operation> GetOperation(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Operation>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested operation
            try
            {
                Operation requestedOperation = taskstore.Operations.Single<Operation>(t => t.ID == id);

                // if the requested operation does not belong to the authenticated user, return 403 Forbidden, otherwise return the operation
                if (requestedOperation.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<Operation>(req, requestedOperation, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // operation not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new Operation
        /// </summary>
        /// <returns>New Operation</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Operation> InsertOperation(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Operation>(req, code);  // user not authenticated

            // get the new operation from the message body
            Operation clientOperation = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(Operation)) as Operation;

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the requested operation does not belong to the authenticated user, return 403 Forbidden
            if (clientOperation.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Forbidden);

            // fill out the ID if it's not set (e.g. from a javascript client)
            if (clientOperation.ID == null || clientOperation.ID == Guid.Empty)
                clientOperation.ID = Guid.NewGuid();

            // fill out the timestamps if they aren't set (null, or MinValue.Date, allowing for DST and timezone issues)
            DateTime now = DateTime.UtcNow;
            if (clientOperation.Timestamp == null || clientOperation.Timestamp.Date == DateTime.MinValue.Date)
                clientOperation.Timestamp = now;

            // add the new operation to the database
            try
            {
                var operation = taskstore.Operations.Add(clientOperation);
                int rows = taskstore.SaveChanges();
                if (operation == null || rows < 1)
                    return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Conflict);  // return 409 Conflict
                else
                    return new HttpResponseMessageWrapper<Operation>(req, operation, HttpStatusCode.Created);  // return 201 Created
            }
            catch (Exception)
            {
                // check for the condition where the operation is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbOperation = taskstore.Operations.Single(t => t.ID == clientOperation.ID);
                    if (dbOperation.EntityName == clientOperation.EntityName)
                        return new HttpResponseMessageWrapper<Operation>(req, dbOperation, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // operation not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Conflict);
                }
            }
        }
    
        /// <summary>
        /// Update an Operation
        /// </summary>
        /// <returns>Updated Operation<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Operation> UpdateOperation(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Operation>(req, code);  // user not authenticated

            // the body will be two Operations - the original and the new values.  Verify this
            List<Operation> clientOperations = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(List<Operation>)) as List<Operation>;
            if (clientOperations.Count != 2)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.BadRequest);

            // get the original and new operations out of the message body
            Operation originalOperation = clientOperations[0];
            Operation newOperation = clientOperations[1];

            // make sure the operation ID's match
            if (originalOperation.ID != newOperation.ID)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.BadRequest);
            if (originalOperation.ID != id)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the operation does not belong to the authenticated user, return 403 Forbidden
            if (originalOperation.UserID != dbUser.ID || newOperation.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.Forbidden);

            try
            {
                Operation requestedOperation = taskstore.Operations.Single<Operation>(t => t.ID == id);

                bool changed = false;

                // call update and make sure the changed flag reflects the outcome correctly
                changed = (Update(requestedOperation, originalOperation, newOperation) == true ? true : changed);
                if (changed == true)
                {
                    int rows = taskstore.SaveChanges();
                    if (rows < 1)
                        return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<Operation>(req, requestedOperation, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<Operation>(req, requestedOperation, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // operation not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Operation>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Operation requestedOperation, Operation originalOperation, Operation newOperation)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedOperation.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedOperation, null);
                object origValue = pi.GetValue(originalOperation, null);
                object newValue = pi.GetValue(newOperation, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedOperation, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}