namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.ServerEntities;

    [ServiceContract]
    [LogMessages]
    public class ItemTypeResource : BaseResource
    {

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> DeleteItemType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<ItemType>(req, code);  
            }

            // get the new itemType from the message body
            ItemType clientItemType = ProcessRequestBody(req, typeof(ItemType)) as ItemType;
            if (clientItemType.ID != id)
            {   // verify ID's match
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            }


            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Single<ItemType>(t => t.ID == id);
                if (requestedItemType.UserID != CurrentUserID)
                {   // requested itemType does not belong to the authenticated user, return 403 Forbidden
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
                }

                this.StorageContext.ItemTypes.Remove(requestedItemType);
                if (this.StorageContext.SaveChanges() > 0)
                {
                    return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
                }

                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
            }
            catch (Exception)
            {   // itemType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ItemType>> Get(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<List<ItemType>>(req, code);
            } 

            try
            {
                var itemTypes = this.StorageContext.ItemTypes.
                    Include("Fields").
                    Where(lt => lt.UserID == null || lt.UserID == CurrentUserID).
                    OrderBy(lt => lt.Name).
                    ToList<ItemType>();
                return new HttpResponseMessageWrapper<List<ItemType>>(req, itemTypes, HttpStatusCode.OK);
            }
            catch (Exception)
            {   // itemType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<ItemType>>(req, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> GetItemType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<ItemType>(req, code);
            } 

            // get the requested itemType
            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Include("Fields").Single<ItemType>(t => t.ID == id);

                if (requestedItemType.UserID != null && requestedItemType.UserID != CurrentUserID)
                {   // requested itemType does not belong to system or authenticated user, return 403 Forbidden
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
                }

                return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.OK);
            }
            catch (Exception)
            {   // itemType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.NotFound);
            }
        }


        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> InsertItemType(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<ItemType>(req, code);
            } 

            // get the new itemType from the message body
            ItemType clientItemType = ProcessRequestBody(req, typeof(ItemType)) as ItemType;

            if (clientItemType.UserID == null || clientItemType.UserID == Guid.Empty)
            {   // changing a system itemType to a user itemType
                clientItemType.UserID = CurrentUserID;
            }
            if (clientItemType.UserID != CurrentUserID)
            {   // requested itemType does not belong to authenticated user, return 403 Forbidden
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
            }

            try
            {
                var itemType = this.StorageContext.ItemTypes.Add(clientItemType);
                if (this.StorageContext.SaveChanges() > 0 && itemType != null)
                {
                    return new HttpResponseMessageWrapper<ItemType>(req, itemType, HttpStatusCode.Created);
                }
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
            }
            catch (Exception)
            {   // check for condition where the listtype is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbItemType = this.StorageContext.ItemTypes.Single(t => t.ID == clientItemType.ID);
                    if (dbItemType.Name == clientItemType.Name)
                    {
                        return new HttpResponseMessageWrapper<ItemType>(req, dbItemType, HttpStatusCode.Accepted);
                    }
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
                }
                catch (Exception)
                {   // listtype not inserted - return 409 Conflict
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
                }
            }
        }
    
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> UpdateItemType(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<ItemType>(req, code);
            } 

            // the body will contain two ItemTypes - the original and the new values
            List<ItemType> itemTypes = ProcessRequestBody(req, typeof(List<ItemType>)) as List<ItemType>;
            if (itemTypes.Count != 2)
            {   // body should contain two ItemTypes, the original and new values
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            }

            ItemType originalItemType = itemTypes[0];
            ItemType newItemType = itemTypes[1];

            if (originalItemType.ID != newItemType.ID ||originalItemType.ID != id)
            {   // IDs must match
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            }

            if (originalItemType.UserID != CurrentUserID || newItemType.UserID != CurrentUserID)
            {   // itemType does not belong to the authenticated user, return 403 Forbidden
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
            }

            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Single<ItemType>(t => t.ID == id);
                bool changed = Update(requestedItemType, originalItemType, newItemType);
                if (changed == true)
                {
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
                    }
                }
                return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {   // itemType not found - return 404 Not Found
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.NotFound);
            }
        }

        private bool Update(ItemType requestedItemType, ItemType originalItemType, ItemType newItemType)
        {
            bool updated = false;
            // TODO: timestamps!
            Type t = requestedItemType.GetType();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                object serverValue = pi.GetValue(requestedItemType, null);
                object origValue = pi.GetValue(originalItemType, null);
                object newValue = pi.GetValue(newItemType, null);

                // if the value has changed, process further 
                if (!object.Equals(origValue, newValue))
                {
                    // if the server has the original value, make the update
                    if (object.Equals(serverValue, origValue))
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