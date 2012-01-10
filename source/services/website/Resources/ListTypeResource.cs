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
    public class ListTypeResource
    {
        private TaskStore TaskStore
        {
            get
            {
                return new TaskStore();
            }
        }

        /// <summary>
        /// Delete the listType 
        /// </summary>
        /// <param name="id">id for the listType to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<ListType> DeleteListType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ListType>(req, code);  // user not authenticated

            // get the new listType from the message body
            ListType clientListType = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(ListType)) as ListType;

            // make sure the listType ID's match
            if (clientListType.ID != id)
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the listType to be deleted
            try
            {
                ListType requestedListType = taskstore.ListTypes.Single<ListType>(t => t.ID == id);

                // if the requested listType does not belong to the authenticated user, return 403 Forbidden
                if (requestedListType.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Forbidden);

                taskstore.ListTypes.Remove(requestedListType);
                int rows = taskstore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<ListType>(req, requestedListType, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get all ListTypes
        /// </summary>
        /// <returns>All ListType information</returns>
        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ListType>> Get(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<List<ListType>>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get all ListTypes
            try
            {
                var listTypes = taskstore.ListTypes.
                    Include("Fields").
                    Where(lt => lt.UserID == null || lt.UserID == dbUser.ID).
                    OrderBy(lt => lt.Name).
                    ToList<ListType>();
                return new HttpResponseMessageWrapper<List<ListType>>(req, listTypes, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<ListType>>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the listType for a listType id
        /// </summary>
        /// <param name="id">id for the listType to return</param>
        /// <returns>listType information</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<ListType> GetListType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ListType>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested listType
            try
            {
                ListType requestedListType = taskstore.ListTypes.Include("Fields").Single<ListType>(t => t.ID == id);

                // if the requested listType is not generic (i.e. UserID == 0), 
                // and does not belong to the authenticated user, return 403 Forbidden, otherwise return the listType
                if (requestedListType.UserID != null && requestedListType.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<ListType>(req, requestedListType, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new listType
        /// </summary>
        /// <returns>New listType</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<ListType> InsertListType(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ListType>(req, code);  // user not authenticated

            // get the new listType from the message body
            ListType clientListType = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(ListType)) as ListType;

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the requested listType does not belong to the authenticated user, return 403 Forbidden, otherwise return the listType
            if (clientListType.UserID == null || clientListType.UserID == Guid.Empty)
                clientListType.UserID = dbUser.ID;
            if (clientListType.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Forbidden);

            // add the new listType to the database
            try
            {
                var listType = taskstore.ListTypes.Add(clientListType);
                int rows = taskstore.SaveChanges();
                if (listType == null || rows != 1)
                    return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Conflict);
                else
                    return new HttpResponseMessageWrapper<ListType>(req, listType, HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // check for the condition where the listtype is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbListType = taskstore.ListTypes.Single(t => t.ID == clientListType.ID);
                    if (dbListType.Name == clientListType.Name)
                        return new HttpResponseMessageWrapper<ListType>(req, dbListType, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // listtype not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Conflict);
                }
            }
        }
    
        /// <summary>
        /// Update a listType
        /// </summary>
        /// <returns>Updated listType<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<ListType> UpdateListType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ListType>(req, code);  // user not authenticated

            // the body will be two ListTypes - the original and the new values.  Verify this
            List<ListType> clientListTypes = ResourceHelper.ProcessRequestBody(req, TaskStore, typeof(List<ListType>)) as List<ListType>;
            if (clientListTypes.Count != 2)
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.BadRequest);

            // get the original and new ListTypes out of the message body
            ListType originalListType = clientListTypes[0];
            ListType newListType = clientListTypes[1];

            // make sure the listType ID's match
            if (originalListType.ID != newListType.ID)
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.BadRequest);
            if (originalListType.ID != id)
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.BadRequest);

            TaskStore taskstore = TaskStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = taskstore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the listType does not belong to the authenticated user, return 403 Forbidden
            if (originalListType.UserID != dbUser.ID || newListType.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.Forbidden);

            // update the listType
            try
            {
                ListType requestedListType = taskstore.ListTypes.Single<ListType>(t => t.ID == id);
                bool changed = Update(requestedListType, originalListType, newListType);
                if (changed == true)
                {
                    int rows = taskstore.SaveChanges();
                    if (rows != 1)
                        return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<ListType>(req, requestedListType, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<ListType>(req, requestedListType, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ListType>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(ListType requestedListType, ListType originalListType, ListType newListType)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedListType.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedListType, null);
                object origValue = pi.GetValue(originalListType, null);
                object newValue = pi.GetValue(newListType, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedListType, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}