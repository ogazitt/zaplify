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
    public class TagResource
    {
        private TaskStore TaskStore
        {
            get
            {
                return new TaskStore();
            }
        }

        /// <summary>
        /// Delete the tag 
        /// </summary>
        /// <param name="id">id for the tag to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> DeleteTag(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Tag>(req, code);  // user not authenticated

            // get the new tag from the message body
            Tag clientTag = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(Tag)) as Tag;

            // make sure the Tag ID's match
            if (clientTag.ID != id)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the tag to be deleted
            try
            {
                Tag requestedTag = taskstore.Tags.Include("TaskTags").Single<Tag>(t => t.ID == id);

                // if the requested tag does not belong to the authenticated user, return 403 Forbidden
                if (requestedTag.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);

                // delete all the tasktags associated with this task
                if (requestedTag.TaskTags != null && requestedTag.TaskTags.Count > 0)
                {
                    foreach (var tt in requestedTag.TaskTags.ToList())
                        taskstore.TaskTags.Remove(tt);
                }

                taskstore.Tags.Remove(requestedTag);
                int rows = taskstore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // tag not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get all Tags
        /// </summary>
        /// <returns>All Tag information</returns>
        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Tag>> Get(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<List<Tag>>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get all Tags
            try
            {
                var tags = taskstore.Tags.
                    Include("Fields").
                    Where(lt => lt.UserID == null || lt.UserID == dbUser.ID).
                    OrderBy(lt => lt.Name).
                    ToList<Tag>();
                return new HttpResponseMessageWrapper<List<Tag>>(req, tags, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // tag not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<Tag>>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the tag for a tag id
        /// </summary>
        /// <param name="id">id for the tag to return</param>
        /// <returns>tag information</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> GetTag(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Tag>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested tag
            try
            {
                Tag requestedTag = taskstore.Tags.Include("Fields").Single<Tag>(t => t.ID == id);

                // if the requested tag is not generic (i.e. UserID == 0), 
                // and does not belong to the authenticated user, return 403 Forbidden, otherwise return the tag
                if (requestedTag.UserID != null && requestedTag.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // tag not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new tag
        /// </summary>
        /// <returns>New tag</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> InsertTag(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Tag>(req, code);  // user not authenticated

            // get the new tag from the message body
            Tag clientTag = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(Tag)) as Tag;

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the requested tag does not belong to the authenticated user, return 403 Forbidden, otherwise return the tag
            if (clientTag.UserID == null || clientTag.UserID == Guid.Empty)
                clientTag.UserID = dbUser.ID;
            if (clientTag.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);

            // add the new tag to the database
            try
            {
                var tag = taskstore.Tags.Add(clientTag);
                int rows = taskstore.SaveChanges();
                if (tag == null || rows != 1)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Conflict);
                else
                    return new HttpResponseMessageWrapper<Tag>(req, tag, HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // check for the condition where the tag is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbTag = taskstore.Tags.Single(t => t.ID == clientTag.ID);
                    if (dbTag.Name == clientTag.Name)
                        return new HttpResponseMessageWrapper<Tag>(req, dbTag, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // tag not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Conflict);
                }
            }
        }
    
        /// <summary>
        /// Update a tag
        /// </summary>
        /// <returns>Updated tag<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> UpdateTag(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Tag>(req, code);  // user not authenticated

            // the body will be two Tags - the original and the new values.  Verify this
            List<Tag> clientTags = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(List<Tag>)) as List<Tag>;
            if (clientTags.Count != 2)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);

            // get the original and new Tags out of the message body
            Tag originalTag = clientTags[0];
            Tag newTag = clientTags[1];

            // make sure the tag ID's match
            if (originalTag.ID != newTag.ID)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);
            if (originalTag.ID != id)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // update the tag
            try
            {
                Tag requestedTag = taskstore.Tags.Single<Tag>(t => t.ID == id);

                // if the TaskList does not belong to the authenticated user, return 403 Forbidden
                if (requestedTag.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalTag.UserID = requestedTag.UserID;
                newTag.UserID = requestedTag.UserID;
                
                bool changed = Update(requestedTag, originalTag, newTag);
                if (changed == true)
                {
                    int rows = taskstore.SaveChanges();
                    if (rows != 1)
                        return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // tag not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Tag requestedTag, Tag originalTag, Tag newTag)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedTag.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedTag, null);
                object origValue = pi.GetValue(originalTag, null);
                object newValue = pi.GetValue(newTag, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedTag, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}