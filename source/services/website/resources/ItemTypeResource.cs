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
using BuiltSteady.Zaplify.Website.Helpers;
using BuiltSteady.Zaplify.Website.Models;
using System.Web.Configuration;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.Website.Resources
{
    [ServiceContract]
    [LogMessages]
    public class ItemTypeResource
    {
        private ZaplifyStore ZaplifyStore
        {
            get
            {
                return new ZaplifyStore();
            }
        }

        /// <summary>
        /// Delete the listType 
        /// </summary>
        /// <param name="id">id for the listType to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> DeleteItemType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ItemType>(req, code);  // user not authenticated

            // get the new listType from the message body
            ItemType clientItemType = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(ItemType)) as ItemType;

            // make sure the listType ID's match
            if (clientItemType.ID != id)
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the listType to be deleted
            try
            {
                ItemType requestedItemType = zaplifystore.ItemTypes.Single<ItemType>(t => t.ID == id);

                // if the requested listType does not belong to the authenticated user, return 403 Forbidden
                if (requestedItemType.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);

                zaplifystore.ItemTypes.Remove(requestedItemType);
                int rows = zaplifystore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get all ItemTypes
        /// </summary>
        /// <returns>All ItemType information</returns>
        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ItemType>> Get(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<List<ItemType>>(req, code);  // user not authenticated

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get all ItemTypes
            try
            {
                var listTypes = zaplifystore.ItemTypes.
                    Include("Fields").
                    Where(lt => lt.UserID == null || lt.UserID == dbUser.ID).
                    OrderBy(lt => lt.Name).
                    ToList<ItemType>();
                return new HttpResponseMessageWrapper<List<ItemType>>(req, listTypes, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<ItemType>>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the listType for a listType id
        /// </summary>
        /// <param name="id">id for the listType to return</param>
        /// <returns>listType information</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> GetItemType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ItemType>(req, code);  // user not authenticated

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested listType
            try
            {
                ItemType requestedItemType = zaplifystore.ItemTypes.Include("Fields").Single<ItemType>(t => t.ID == id);

                // if the requested listType is not generic (i.e. UserID == 0), 
                // and does not belong to the authenticated user, return 403 Forbidden, otherwise return the listType
                if (requestedItemType.UserID != null && requestedItemType.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new listType
        /// </summary>
        /// <returns>New listType</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> InsertItemType(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ItemType>(req, code);  // user not authenticated

            // get the new listType from the message body
            ItemType clientItemType = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(ItemType)) as ItemType;

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the requested listType does not belong to the authenticated user, return 403 Forbidden, otherwise return the listType
            if (clientItemType.UserID == null || clientItemType.UserID == Guid.Empty)
                clientItemType.UserID = dbUser.ID;
            if (clientItemType.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);

            // add the new listType to the database
            try
            {
                var listType = zaplifystore.ItemTypes.Add(clientItemType);
                int rows = zaplifystore.SaveChanges();
                if (listType == null || rows != 1)
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
                else
                    return new HttpResponseMessageWrapper<ItemType>(req, listType, HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // check for the condition where the listtype is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbItemType = zaplifystore.ItemTypes.Single(t => t.ID == clientItemType.ID);
                    if (dbItemType.Name == clientItemType.Name)
                        return new HttpResponseMessageWrapper<ItemType>(req, dbItemType, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // listtype not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
                }
            }
        }
    
        /// <summary>
        /// Update a listType
        /// </summary>
        /// <returns>Updated listType<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> UpdateItemType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ItemType>(req, code);  // user not authenticated

            // the body will be two ItemTypes - the original and the new values.  Verify this
            List<ItemType> clientItemTypes = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(List<ItemType>)) as List<ItemType>;
            if (clientItemTypes.Count != 2)
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);

            // get the original and new ItemTypes out of the message body
            ItemType originalItemType = clientItemTypes[0];
            ItemType newItemType = clientItemTypes[1];

            // make sure the listType ID's match
            if (originalItemType.ID != newItemType.ID)
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            if (originalItemType.ID != id)
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // if the listType does not belong to the authenticated user, return 403 Forbidden
            if (originalItemType.UserID != dbUser.ID || newItemType.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);

            // update the listType
            try
            {
                ItemType requestedItemType = zaplifystore.ItemTypes.Single<ItemType>(t => t.ID == id);
                bool changed = Update(requestedItemType, originalItemType, newItemType);
                if (changed == true)
                {
                    int rows = zaplifystore.SaveChanges();
                    if (rows != 1)
                        return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // listType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(ItemType requestedItemType, ItemType originalItemType, ItemType newItemType)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedItemType.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedItemType, null);
                object origValue = pi.GetValue(originalItemType, null);
                object newValue = pi.GetValue(newItemType, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedItemType, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}