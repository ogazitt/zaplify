using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ApplicationServer.Http;
using System.Net.Http;
using System.Net;
using BuiltSteady.Zaplify.Website.Helpers;
using BuiltSteady.Zaplify.Website.Models;
using System.Reflection;
using System.Web.Configuration;
using System.Data.Entity;
using System.Net.Http.Headers;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.Website.Resources
{
    [ServiceContract]
    [LogMessages]
    public class ItemListResource
    {
        private ZaplifyStore ZaplifyStore 
        {
            get
            {
                return new ZaplifyStore();
            }
        }

        /// <summary>
        /// Delete the ItemList 
        /// </summary>
        /// <param name="id">id for the ItemList to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemList> DeleteItemList(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ItemList>(req, code);  // user not authenticated

            // get the ItemList from the message body
            ItemList clientItemList = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(ItemList)) as ItemList;
 
            // make sure the ItemList ID's match
            if (clientItemList.ID != id)
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the ItemList to be deleted
            try
            {
                ItemList requestedItemList = zaplifystore.ItemLists.Include("Items.ItemTags").Single<ItemList>(tl => tl.ID == id);

                // if the requested ItemList does not belong to the authenticated user, return 403 Forbidden
                if (requestedItemList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Forbidden);

                // remove the itemtags associated with each of the items in this itemlist
                if (requestedItemList.Items != null && requestedItemList.Items.Count > 0)
                {
                    foreach (Item t in requestedItemList.Items)
                    {
                        // delete all the itemtags associated with this item
                        if (t.ItemTags != null && t.ItemTags.Count > 0)
                        {
                            foreach (var tt in t.ItemTags.ToList())
                                zaplifystore.ItemTags.Remove(tt);
                        }
                    }
                }

                // remove the current itemlist 
                zaplifystore.ItemLists.Remove(requestedItemList);
                int rows = zaplifystore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<ItemList>(req, requestedItemList, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // ItemList not found - return 404 Not Found
                //return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.NotFound);
                // ItemList not found - it may have been deleted by someone else.  Return 200 OK.
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Get all itemlists for the current user
        /// </summary>
        /// <returns>List of itemlists for the current user</returns>
        [WebGet(UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ItemList>> GetItemLists(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<List<ItemList>>(req, code);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the itemlists for this user
            try
            {
                Guid id = dbUser.ID;
                var itemlists = zaplifystore.ItemLists.Where(tl => tl.UserID == id).Include(tl => tl.Items).ToList();
                var response = new HttpResponseMessageWrapper<List<ItemList>>(req, itemlists, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception)
            {
                // itemlists not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<ItemList>>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the ItemList for a itemlist id
        /// </summary>
        /// <param name="id">ID for the itemlist</param>
        /// <returns></returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemList> GetItemList(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<ItemList>(req, code);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested itemlist
            try
            {
                ItemList requestedItemList = zaplifystore.ItemLists.Include("Items.ItemTags").Single<ItemList>(tl => tl.ID == id);

                // if the requested user is not the same as the authenticated user, return 403 Forbidden
                if (requestedItemList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Forbidden);
                else
                    return new HttpResponseMessageWrapper<ItemList>(req, requestedItemList, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // itemlist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new ItemList
        /// </summary>
        /// <returns>New ItemList</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemList> InsertItemList(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)  // user not authenticated
                return new HttpResponseMessageWrapper<ItemList>(req, code);

            ItemList clientItemList = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(ItemList)) as ItemList;

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // check to make sure the userid in the new itemlist is the same userid for the current user
            if (clientItemList.UserID == null || clientItemList.UserID == Guid.Empty)
                clientItemList.UserID = dbUser.ID;
            if (clientItemList.UserID != dbUser.ID)
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Forbidden);

            // fill out the ID if it's not set (e.g. from a javascript client)
            if (clientItemList.ID == null || clientItemList.ID == Guid.Empty)
                clientItemList.ID = Guid.NewGuid();

            // this operation isn't meant to do more than just insert the new itemlist
            // therefore make sure items collection is empty
            if (clientItemList.Items != null)
                clientItemList.Items.Clear();

            // add the new itemlist
            try
            {
                var itemlist = zaplifystore.ItemLists.Add(clientItemList);
                int rows = zaplifystore.SaveChanges();
                if (itemlist == null || rows != 1)
                    return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Conflict);
                else
                    return new HttpResponseMessageWrapper<ItemList>(req, itemlist, HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // check for the condition where the itemlist is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbItemList = zaplifystore.ItemLists.Single(tl => tl.ID == clientItemList.ID);
                    if (dbItemList.DefaultItemTypeID == clientItemList.DefaultItemTypeID &&
                        dbItemList.Name == clientItemList.Name &&
                        dbItemList.UserID == clientItemList.UserID)
                        return new HttpResponseMessageWrapper<ItemList>(req, dbItemList, HttpStatusCode.Accepted);
                    else
                        return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {
                    // itemlist not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Conflict);
                }
            }
        }

        /// <summary>
        /// Update a ItemList
        /// </summary>
        /// <returns>Updated ItemList<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemList> UpdateItemList(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<ItemList>(req, code);  // user not authenticated

            // the body will be two ItemLists - the original and the new values.  Verify this
            List<ItemList> clientItemLists = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(List<ItemList>)) as List<ItemList>;
            if (clientItemLists.Count != 2)
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.BadRequest);

            // get the original and new ItemLists out of the message body
            ItemList originalItemList = clientItemLists[0];
            ItemList newItemList = clientItemLists[1];

            // make sure the ItemList ID's match
            if (originalItemList.ID != newItemList.ID)
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.BadRequest);
            if (originalItemList.ID != id)
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // update the ItemList
            try
            {
                ItemList requestedItemList = zaplifystore.ItemLists.Single<ItemList>(t => t.ID == id);

                // if the ItemList does not belong to the authenticated user, return 403 Forbidden
                if (requestedItemList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.Forbidden);
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalItemList.UserID = requestedItemList.UserID;
                newItemList.UserID = requestedItemList.UserID;

                bool changed = Update(requestedItemList, originalItemList, newItemList);
                if (changed == true)
                {
                    int rows = zaplifystore.SaveChanges();
                    if (rows != 1)
                        return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<ItemList>(req, requestedItemList, HttpStatusCode.Accepted);
                }
                else
                    return new HttpResponseMessageWrapper<ItemList>(req, requestedItemList, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                // ItemList not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemList>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Update the requested itemlist with values from the new itemlist
        /// Currently, the algorithm updates only if the server's current value is equal 
        /// to the original value passed in.
        /// NOTE: the server value for itemlists currently does not include the Item collection
        /// because we did not .Include() it in the EF query.  This works well so that the update
        /// loop bypasses the Items collection - we are only updating scalar values.
        /// </summary>
        /// <param name="requestedItemList"></param>
        /// <param name="originalItemList"></param>
        /// <param name="newItemList"></param>
        /// <returns></returns>
        private bool Update(ItemList requestedItemList, ItemList originalItemList, ItemList newItemList)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedItemList.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedItemList, null);
                object origValue = pi.GetValue(originalItemList, null);
                object newValue = pi.GetValue(newItemList, null);

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (Object.Equals(serverValue, origValue))
                    {
                        pi.SetValue(requestedItemList, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}