using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ApplicationServer.Http;
using System.Net.Http;
using System.Net;
using TaskStoreWeb.Helpers;
using TaskStoreWeb.Models;
using System.Reflection;
using System.Web.Configuration;
using System.Data.Entity;
using System.Net.Http.Headers;
using TaskStoreServerEntities;

namespace TaskStoreWeb.Resources
{
    [ServiceContract]
    [LogMessages]
    public class TaskListResource
    {
        private TaskStore TaskStore 
        {
            get
            {
                return new TaskStore();
            }
        }

        /// <summary>
        /// Delete the TaskList 
        /// </summary>
        /// <param name="id">id for the TaskList to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<TaskList> DeleteTaskList(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<TaskList>(req, code);  // user not authenticated

            // get the TaskList from the message body
            TaskList clientTaskList = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(TaskList)) as TaskList;
 
            // make sure the TaskList ID's match
            if (clientTaskList.ID != id)
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the TaskList to be deleted
            try
            {
                TaskList requestedTaskList = taskstore.TaskLists.Include("Tasks.TaskTags").Single<TaskList>(tl => tl.ID == id);

                // if the requested TaskList does not belong to the authenticated user, return 403 Forbidden
                if (requestedTaskList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Forbidden);

                // remove the tasktags associated with each of the tasks in this tasklist
                if (requestedTaskList.Tasks != null && requestedTaskList.Tasks.Count > 0)
                {
                    foreach (Task t in requestedTaskList.Tasks)
                    {
                        // delete all the tasktags associated with this task
                        if (t.TaskTags != null && t.TaskTags.Count > 0)
                        {
                            foreach (var tt in t.TaskTags.ToList())
                                taskstore.TaskTags.Remove(tt);
                        }
                    }
                }

                // remove the current tasklist 
                taskstore.TaskLists.Remove(requestedTaskList);
                int rows = taskstore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<TaskList>(req, requestedTaskList, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // TaskList not found - return 404 Not Found
                //return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.NotFound);
                // TaskList not found - it may have been deleted by someone else.  Return 200 OK.
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Get all tasklists for the current user
        /// </summary>
        /// <returns>List of tasklists for the current user</returns>
        [WebGet(UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<TaskList>> GetTaskLists(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<List<TaskList>>(req, code);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the tasklists for this user
            try
            {
                Guid id = dbUser.ID;
                var tasklists = taskstore.TaskLists.Where(tl => tl.UserID == id).Include(tl => tl.Tasks).ToList();
                var response = new HttpResponseMessageWrapper<List<TaskList>>(req, tasklists, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception)
            {
                // tasklists not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<TaskList>>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the TaskList for a tasklist id
        /// </summary>
        /// <param name="id">ID for the tasklist</param>
        /// <returns></returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<TaskList> GetTaskList(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<TaskList>(req, code);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested tasklist
            try
            {
                TaskList requestedTaskList = taskstore.TaskLists.Include("Tasks.TaskTags").Single<TaskList>(tl => tl.ID == id);

                // if the requested user is not the same as the authenticated user, return 403 Forbidden
                if (requestedTaskList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<TaskList>(req, requestedTaskList, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // tasklist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new TaskList
        /// </summary>
        /// <returns>New TaskList</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<TaskList> InsertTaskList(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<TaskList>(req, code);

            TaskList clientTaskList = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(TaskList)) as TaskList;

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // check to make sure the userid in the new tasklist is the same userid for the current user
            if (clientTaskList.UserID == null || clientTaskList.UserID == Guid.Empty)
                clientTaskList.UserID = dbUser.ID;
            if (clientTaskList.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Forbidden);

            // fill out the ID if it's not set (e.g. from a javascript client)
            if (clientTaskList.ID == null || clientTaskList.ID == Guid.Empty)
                clientTaskList.ID = Guid.NewGuid();

            // this operation isn't meant to do more than just insert the new tasklist
            // therefore make sure tasks collection is empty
            if (clientTaskList.Tasks != null)
                clientTaskList.Tasks.Clear();

            // add the new tasklist
            try
            {
                var tasklist = taskstore.TaskLists.Add(clientTaskList);
                int rows = taskstore.SaveChanges();
                if (tasklist == null || rows != 1)
                    return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Conflict);
                else
                    return new HttpResponseMessageWrapper<TaskList>(req, tasklist, HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // check for the condition where the tasklist is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbTaskList = taskstore.TaskLists.Single(tl => tl.ID == clientTaskList.ID);
                    if (dbTaskList.ListTypeID == clientTaskList.ListTypeID &&
                        dbTaskList.Name == clientTaskList.Name &&
                        dbTaskList.UserID == clientTaskList.UserID)
                        return new HttpResponseMessageWrapper<TaskList>(req, dbTaskList, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // tasklist not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Conflict);
                }
            }
        }

        /// <summary>
        /// Update a TaskList
        /// </summary>
        /// <returns>Updated TaskList<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<TaskList> UpdateTaskList(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<TaskList>(req, code);  // user not authenticated

            // the body will be two TaskLists - the original and the new values.  Verify this
            List<TaskList> clientTaskLists = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(List<TaskList>)) as List<TaskList>;
            if (clientTaskLists.Count != 2)
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.BadRequest);

            // get the original and new TaskLists out of the message body
            TaskList originalTaskList = clientTaskLists[0];
            TaskList newTaskList = clientTaskLists[1];

            // make sure the TaskList ID's match
            if (originalTaskList.ID != newTaskList.ID)
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.BadRequest);
            if (originalTaskList.ID != id)
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // update the TaskList
            try
            {
                TaskList requestedTaskList = taskstore.TaskLists.Single<TaskList>(t => t.ID == id);

                // if the TaskList does not belong to the authenticated user, return 403 Forbidden
                if (requestedTaskList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.Forbidden);
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalTaskList.UserID = requestedTaskList.UserID;
                newTaskList.UserID = requestedTaskList.UserID;

                bool changed = Update(requestedTaskList, originalTaskList, newTaskList);
                if (changed == true)
                {
                    int rows = taskstore.SaveChanges();
                    if (rows != 1)
                        return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<TaskList>(req, requestedTaskList, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<TaskList>(req, requestedTaskList, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // TaskList not found - return 404 Not Found
                return new HttpResponseMessageWrapper<TaskList>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Update the requested tasklist with values from the new tasklist
        /// Currently, the algorithm updates only if the server's current value is equal 
        /// to the original value passed in.
        /// NOTE: the server value for tasklists currently does not include the Task collection
        /// because we did not .Include() it in the EF query.  This works well so that the update
        /// loop bypasses the Tasks collection - we are only updating scalar values.
        /// </summary>
        /// <param name="requestedTaskList"></param>
        /// <param name="originalTaskList"></param>
        /// <param name="newTaskList"></param>
        /// <returns></returns>
        private bool Update(TaskList requestedTaskList, TaskList originalTaskList, TaskList newTaskList)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedTaskList.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedTaskList, null);
                object origValue = pi.GetValue(originalTaskList, null);
                object newValue = pi.GetValue(newTaskList, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedTaskList, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}