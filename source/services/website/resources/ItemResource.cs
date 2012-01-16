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
    public class ItemResource
    {
        private ZaplifyStore ZaplifyStore
        {
            get
            {
                return new ZaplifyStore();
            }
        }

        /// <summary>
        /// Delete the Item 
        /// </summary>
        /// <param name="id">id for the item to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> DeleteItem(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Item>(req, code);  // user not authenticated

            // get the new item from the message body
            Item clientItem = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(Item)) as Item;

            // make sure the item ID's match
            if (clientItem.ID != id)
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the itemlist of the item to be deleted
            try
            {
                ItemList itemlist = zaplifystore.ItemLists.Single<ItemList>(tl => tl.ID == clientItem.ItemListID);

                // if the requested item does not belong to the authenticated user, return 403 Forbidden
                if (itemlist.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);

                // get the item to be deleted
                try
                {
                    Item requestedItem = zaplifystore.Items.Include("ItemTags").Single<Item>(t => t.ID == id);

                    // delete all the itemtags associated with this item
                    if (requestedItem.ItemTags != null && requestedItem.ItemTags.Count > 0)
                    {
                        foreach (var tt in requestedItem.ItemTags.ToList())
                            zaplifystore.ItemTags.Remove(tt);
                    }

                    zaplifystore.Items.Remove(requestedItem);
                    int rows = zaplifystore.SaveChanges();
                    if (rows < 1)
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);
                    else
                        return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                }
                catch (Exception)
                {
                    // item not found - it may have been deleted by someone else.  Return 200 OK.
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.OK);
                }
            }
            catch (Exception)
            {
                // itemlist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get the Item for a item id
        /// </summary>
        /// <param name="id">id for the item to return</param>
        /// <returns>Item information</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> GetItem(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Item>(req, code);  // user not authenticated

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the requested item
            try
            {
                Item requestedItem = zaplifystore.Items.Include("ItemTags").Include("FieldValues").Single<Item>(t => t.ID == id);

                // get the itemlist of the requested item
                try
                {
                    ItemList itemlist = zaplifystore.ItemLists.Single<ItemList>(tl => tl.ID == requestedItem.ItemListID);

                    // if the requested item does not belong to the authenticated user, return 403 Forbidden, otherwise return the item
                    if (itemlist.UserID != dbUser.ID)
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);
                    else
                        return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.OK);
                }
                catch (Exception)
                {
                    // user not found - return 404 Not Found
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception)
            {
                // item not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Insert a new Item
        /// </summary>
        /// <returns>New Item</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> InsertItem(HttpRequestMessage req)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Item>(req, code);  // user not authenticated

            // get the new item from the message body
            Item clientItem = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(Item)) as Item;

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);


            // get the itemlist into which to insert the new item
            try
            {
                ItemList itemlist = zaplifystore.ItemLists.Single<ItemList>(tl => tl.ID == clientItem.ItemListID);

                // if the requested item does not belong to the authenticated user, return 403 Forbidden
                if (itemlist.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);

                // fill out the ID if it's not set (e.g. from a javascript client)
                if (clientItem.ID == null || clientItem.ID == Guid.Empty)
                    clientItem.ID = Guid.NewGuid();

                // fill out the timestamps if they aren't set (null, or MinValue.Date, allowing for DST and timezone issues)
                DateTime now = DateTime.UtcNow;
                if (clientItem.Created == null || clientItem.Created.Date == DateTime.MinValue.Date)
                    clientItem.Created = now;
                if (clientItem.LastModified == null || clientItem.LastModified.Date == DateTime.MinValue.Date)
                    clientItem.LastModified = now;

                // make sure the LinkedItemList is null if it's empty
                if (clientItem.LinkedItemListID == Guid.Empty)
                    clientItem.LinkedItemListID = null;

                // add the new item to the database
                try
                {
                    var item = zaplifystore.Items.Add(clientItem);
                    int rows = zaplifystore.SaveChanges();
                    if (item == null || rows < 1)
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);  // return 409 Conflict
                    else
                        return new HttpResponseMessageWrapper<Item>(req, item, HttpStatusCode.Created);  // return 201 Created
                }
                catch (Exception)
                {
                    // check for the condition where the itemlist is already in the database
                    // in that case, return 202 Accepted; otherwise, return 409 Conflict
                    try
                    {
                        var dbItem = zaplifystore.Items.Single(t => t.ID == clientItem.ID);
                        if (dbItem.Name == clientItem.Name)
                            return new HttpResponseMessageWrapper<Item>(req, dbItem, HttpStatusCode.Accepted);
                        else
                            return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);
                    }
                    catch (Exception)
                    {
                        // itemlist not inserted - return 409 Conflict
                        return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Conflict);
                    }
                }
            }
            catch (Exception)
            {
                // itemlist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }
    
        /// <summary>
        /// Update a Item
        /// </summary>
        /// <returns>Updated Item<returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Item> UpdateItem(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<Item>(req, code);  // user not authenticated

            // the body will be two Items - the original and the new values.  Verify this
            List<Item> clientItems = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(List<Item>)) as List<Item>;
            if (clientItems.Count != 2)
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);

            // get the original and new items out of the message body
            Item originalItem = clientItems[0];
            Item newItem = clientItems[1];

            // make sure the item ID's match
            if (originalItem.ID != newItem.ID)
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);
            if (originalItem.ID != id)
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // get the itemlist for the item
            try
            {
                ItemList oldItemList = zaplifystore.ItemLists.Single<ItemList>(tl => tl.ID == originalItem.ItemListID);
                ItemList newItemList = zaplifystore.ItemLists.Single<ItemList>(tl => tl.ID == newItem.ItemListID);

                // if the itemlist does not belong to the authenticated user, return 403 Forbidden
                if (oldItemList.UserID != dbUser.ID || newItemList.UserID != dbUser.ID)
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.Forbidden);

                try
                {
                    Item requestedItem = zaplifystore.Items.Include("ItemTags").Single<Item>(t => t.ID == id);

                    bool changed = false;

                    // delete all the itemtags associated with this item
                    if (requestedItem.ItemTags != null && requestedItem.ItemTags.Count > 0)
                    {
                        foreach (var tt in requestedItem.ItemTags.ToList())
                            zaplifystore.ItemTags.Remove(tt);
                        changed = true;
                    }

                    // call update and make sure the changed flag reflects the outcome correctly
                    changed = (Update(requestedItem, originalItem, newItem) == true ? true : changed);
                    if (changed == true)
                    {
                        int rows = zaplifystore.SaveChanges();
                        if (rows < 1)
                            return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.InternalServerError);
                        else
                            return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                    }
                    else
                        return new HttpResponseMessageWrapper<Item>(req, requestedItem, HttpStatusCode.Accepted);
                }
                catch (Exception)
                {
                    // item not found - return 404 Not Found
                    return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
                }
            }
            catch (Exception)
            {
                // itemlist not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Item>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Item requestedItem, Item originalItem, Item newItem)
        {
            bool updated = false;
            // timestamps!!
            Type t = requestedItem.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedItem, null);
                object origValue = pi.GetValue(originalItem, null);
                object newValue = pi.GetValue(newItem, null);

                // if this is the TasgTags field make it simple - if this update is the last one, it wins
                if (pi.Name == "ItemTags")
                {
                    if (newItem.LastModified > requestedItem.LastModified)
                    {
                        pi.SetValue(requestedItem, newValue, null);
                        updated = true;
                    }
                    continue;
                }

                // if the value has changed, process further 
                if (!Object.Equals(origValue, newValue))
                {
                    // if the server has the original value, or the new item has a later timestamp than the server, then make the update
                    if (Object.Equals(serverValue, origValue) || newItem.LastModified > requestedItem.LastModified)
                    {
                        pi.SetValue(requestedItem, newValue, null);
                        updated = true;
                    }
                }
            }

            return updated;
        }
    }
}