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
    public class TaskResource
    {
        private TaskStore TaskStore
        {
            get
            {
                return new TaskStore();
            }
        }

        /// <summary>
        /// Delete the Task 
        /// </summary>
        /// <param name="id">id for the task to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Task> DeleteTask(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Task>(req, code);  // user not authenticated

            // get the new task from the message body
            Task clientTask = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(Task)) as Task;

            // make sure the task ID's match
            if (clientTask.ID != id)
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the tasklist of the task to be deleted
            try
            {
                TaskList tasklist = taskstore.TaskLists.Single<TaskList>(tl => tl.ID == clientTask.TaskListID);

                // if the requested task does not belong to the authenticated user, return 403 Forbidden
                if (tasklist.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Forbidden);

                // get the task to be deleted
                try
                {
                    Task requestedTask = taskstore.Tasks.Include("TaskTags").Single<Task>(t => t.ID == id);

                    // delete all the tasktags associated with this task
                    if (requestedTask.TaskTags != null && requestedTask.TaskTags.Count > 0)
                    {
                        foreach (var tt in requestedTask.TaskTags.ToList())
                            taskstore.TaskTags.Remove(tt);
                    }

                    taskstore.Tasks.Remove(requestedTask);
                    int rows = taskstore.SaveChanges();
                    if (rows < 1)
                        return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<Task>(req, requestedTask, HttpStatusCode.Accepted);
                }
                catch (Exception)
                {
                    // task not found - it may have been deleted by someone else.  Return 200 OK.
                    return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.OK);
                }
            }
            catch (Exception)
            {
                // tasklist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the Task for a task id
        /// </summary>
        /// <param name="id">id for the task to return</param>
        /// <returns>Task information</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Task> GetTask(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Task>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested task
            try
            {
                Task requestedTask = taskstore.Tasks.Include("TaskTags").Single<Task>(t => t.ID == id);

                // get the tasklist of the requested task
                try
                {
                    TaskList tasklist = taskstore.TaskLists.Single<TaskList>(tl => tl.ID == requestedTask.TaskListID);

                    // if the requested task does not belong to the authenticated user, return 403 Forbidden, otherwise return the task
                    if (tasklist.UserID != dbUser.ID)
                        return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Forbidden);
                    else
                        return new HttpResponseMessageWrapper<Task>(req, requestedTask, HttpStatusCode.OK);
                }
                catch (Exception)
                {
                    // user not found - return 404 Not Found
                    return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception)
            {
                // task not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new Task
        /// </summary>
        /// <returns>New Task</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Task> InsertTask(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Task>(req, code);  // user not authenticated

            // get the new task from the message body
            Task clientTask = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(Task)) as Task;

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);


            // get the tasklist into which to insert the new task
            try
            {
                TaskList tasklist = taskstore.TaskLists.Single<TaskList>(tl => tl.ID == clientTask.TaskListID);

                // if the requested task does not belong to the authenticated user, return 403 Forbidden
                if (tasklist.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Forbidden);

                // fill out the ID if it's not set (e.g. from a javascript client)
                if (clientTask.ID == null || clientTask.ID == Guid.Empty)
                    clientTask.ID = Guid.NewGuid();

                // fill out the timestamps if they aren't set (null, or MinValue.Date, allowing for DST and timezone issues)
                DateTime now = DateTime.UtcNow;
                if (clientTask.Created == null || clientTask.Created.Date == DateTime.MinValue.Date)
                    clientTask.Created = now;
                if (clientTask.LastModified == null || clientTask.LastModified.Date == DateTime.MinValue.Date)
                    clientTask.LastModified = now;

                // make sure the LinkedTaskList is null if it's empty
                if (clientTask.LinkedTaskListID == Guid.Empty)
                    clientTask.LinkedTaskListID = null;

                // add the new task to the database
                try
                {
                    var task = taskstore.Tasks.Add(clientTask);
                    int rows = taskstore.SaveChanges();
                    if (task == null || rows < 1)
                        return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Conflict);  // return 409 Conflict
                    else
                        return new HttpResponseMessageWrapper<Task>(req, task, HttpStatusCode.Created);  // return 201 Created
                }
                catch (Exception)
                {
                    // check for the condition where the tasklist is already in the database
                    // in that case, return 202 Accepted; otherwise, return 409 Conflict
                    try
                    {
                        var dbTask = taskstore.Tasks.Single(t => t.ID == clientTask.ID);
                        if (dbTask.Name == clientTask.Name)
                            return new HttpResponseMessageWrapper<Task>(req, dbTask, HttpStatusCode.Accepted);
                        else
                            return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Conflict);
                    }
                    catch (Exception)
                    {
                        // tasklist not inserted - return 409 Conflict
                        return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Conflict);
                    }
                }
            }
            catch (Exception)
            {
                // tasklist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.NotFound);
            }
        }
    
        /// <summary>
        /// Update a Task
        /// </summary>
        /// <returns>Updated Task<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Task> UpdateTask(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Task>(req, code);  // user not authenticated

            // the body will be two Tasks - the original and the new values.  Verify this
            List<Task> clientTasks = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(List<Task>)) as List<Task>;
            if (clientTasks.Count != 2)
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.BadRequest);

            // get the original and new tasks out of the message body
            Task originalTask = clientTasks[0];
            Task newTask = clientTasks[1];

            // make sure the task ID's match
            if (originalTask.ID != newTask.ID)
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.BadRequest);
            if (originalTask.ID != id)
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the tasklist for the task
            try
            {
                TaskList oldTaskList = taskstore.TaskLists.Single<TaskList>(tl => tl.ID == originalTask.TaskListID);
                TaskList newTaskList = taskstore.TaskLists.Single<TaskList>(tl => tl.ID == newTask.TaskListID);

                // if the tasklist does not belong to the authenticated user, return 403 Forbidden
                if (oldTaskList.UserID != dbUser.ID || newTaskList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.Forbidden);

                try
                {
                    Task requestedTask = taskstore.Tasks.Include("TaskTags").Single<Task>(t => t.ID == id);

                    bool changed = false;

                    // delete all the tasktags associated with this task
                    if (requestedTask.TaskTags != null && requestedTask.TaskTags.Count > 0)
                    {
                        foreach (var tt in requestedTask.TaskTags.ToList())
                            taskstore.TaskTags.Remove(tt);
                        changed = true;
                    }

                    // call update and make sure the changed flag reflects the outcome correctly
                    changed = (Update(requestedTask, originalTask, newTask) == true ? true : changed);
                    if (changed == true)
                    {
                        int rows = taskstore.SaveChanges();
                        if (rows < 1)
                            return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.InternalServerError);
                        else
                            return new HttpResponseMessageWrapper<Task>(req, requestedTask, HttpStatusCode.Accepted);
                    }
                    else
                        return new HttpResponseMessageWrapper<Task>(req, requestedTask, HttpStatusCode.Accepted);
                }
                catch (Exception)
                {
                    // task not found - return 404 Not Found
                    return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception)
            {
                // tasklist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Task>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Task requestedTask, Task originalTask, Task newTask)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedTask.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedTask, null);
                object origValue = pi.GetValue(originalTask, null);
                object newValue = pi.GetValue(newTask, null);

                // if this is the TasgTags field make it simple - if this update is the last one, it wins
                if (pi.Name == "TaskTags")
                {
                    if (newTask.LastModified > requestedTask.LastModified)
                    {
                        pi.SetValue(requestedTask, newValue, null);
                        updated = true;
                    }
                    continue;
                }

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, or the new task has a later timestamp than the server, then make the update
                    if (Object.Equals(serverValue, origValue) || newTask.LastModified > requestedTask.LastModified)
                    {
                        pi.SetValue(requestedTask, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}