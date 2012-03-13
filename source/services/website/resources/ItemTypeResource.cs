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

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Helpers;

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

            // get the itemtype from the message body if one was passed
            ItemType clientItemType;
            if (req.Content.Headers.ContentLength > 0)
            {
                clientItemType = ProcessRequestBody(req, typeof(ItemType)) as ItemType;
                if (clientItemType.ID != id)
                {   // IDs must match
                    TraceLog.TraceError("ItemTypeResource.Delete: Bad Request (ID in URL does not match entity body)");
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                // otherwise get the client itemtype from the database
                try
                {
                    clientItemType = this.StorageContext.ItemTypes.Single<ItemType>(it => it.ID == id);
                }
                catch (Exception)
                {   // itemtype not found - it may have been deleted by someone else.  Return 200 OK.
                    TraceLog.TraceInfo("ItemTypeResource.Delete: entity not found; returned OK anyway");
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.OK);
                }
            }

            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Single<ItemType>(t => t.ID == id);
                if (requestedItemType.UserID != CurrentUser.ID)
                {   // requested itemType does not belong to the authenticated user, return 403 Forbidden
                    TraceLog.TraceError("ItemTypeResource.Delete: Forbidden (entity does not belong to current user)");
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
                }

                this.StorageContext.ItemTypes.Remove(requestedItemType);
                if (this.StorageContext.SaveChanges() < 1)
                {
                    TraceLog.TraceError("ItemTypeResource.Delete: Internal Server Error (database operation did not succeed)");
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
                }
                else
                {
                    TraceLog.TraceInfo("ItemTypeResource.Delete: Accepted");
                    return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {   
                // itemtype not found - it may have been deleted by someone else.  Return 200 OK.
                TraceLog.TraceInfo(String.Format("ItemTypeResource.Delete: exception in database operation: {0}; returned OK anyway", ex.Message));
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.OK);
            }
        }

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ItemType>> GetItemTypes(HttpRequestMessage req)
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
                    Where(lt => lt.UserID == null || lt.UserID == CurrentUser.ID).
                    OrderBy(lt => lt.Name).
                    ToList<ItemType>();
                return new HttpResponseMessageWrapper<List<ItemType>>(req, itemTypes, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {   // itemType not found - return 404 Not Found
                TraceLog.TraceError("ItemTypeResource.GetItemTypes: Not Found; ex: " + ex.Message);
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

                if (requestedItemType.UserID != null && requestedItemType.UserID != CurrentUser.ID)
                {   // requested itemType does not belong to system or authenticated user, return 403 Forbidden
                    TraceLog.TraceError("ItemTypeResource.GetItemType: Forbidden (entity does not belong to current user)");
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
                }

                return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {   // itemType not found - return 404 Not Found
                TraceLog.TraceError("ItemTypeResource.GetItemType: Not Found; ex: " + ex.Message);
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
                clientItemType.UserID = CurrentUser.ID;
            }
            if (clientItemType.UserID != CurrentUser.ID)
            {   // requested itemType does not belong to authenticated user, return 403 Forbidden
                TraceLog.TraceError("ItemTypeResource.Insert: Forbidden (entity does not belong to current user)");
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Forbidden);
            }

            try
            {
                var itemType = this.StorageContext.ItemTypes.Add(clientItemType);
                if (itemType == null || this.StorageContext.SaveChanges() < 1)
                {
                    TraceLog.TraceError("ItemTypeResource.Insert: Internal Server Error (database operation did not succeed)");
                    return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
                }
                else
                {
                    TraceLog.TraceInfo("ItemTypeResource.Insert: Created");
                    return new HttpResponseMessageWrapper<ItemType>(req, itemType, HttpStatusCode.Created);
                }
            }
            catch (Exception ex)
            {   // check for condition where the itemtype is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbItemType = this.StorageContext.ItemTypes.Single(t => t.ID == clientItemType.ID);
                    if (dbItemType.Name == clientItemType.Name)
                    {
                        TraceLog.TraceInfo("ItemTypeResource.Insert: Accepted (entity already in database); ex: " + ex.Message);
                        return new HttpResponseMessageWrapper<ItemType>(req, dbItemType, HttpStatusCode.Accepted);
                    }
                    else
                    {
                        TraceLog.TraceError("ItemTypeResource.Insert: Conflict (entity in database did not match); ex: " + ex.Message);
                        return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.Conflict);
                    }
                }
                catch (Exception e)
                {   // itemtype not inserted - return 409 Conflict
                    TraceLog.TraceError(String.Format("ItemTypeResource.Insert: Conflict (entity was not in database); ex: {0}, ex {1}", ex.Message, e.Message));
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
                TraceLog.TraceError("ItemTypeResource.Update: Bad Request (malformed body)");
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            }

            ItemType originalItemType = itemTypes[0];
            ItemType newItemType = itemTypes[1];

            // make sure the itemtype ID's match
            if (originalItemType.ID != newItemType.ID)
            {
                TraceLog.TraceError("ItemTypeResource.Update: Bad Request (original and new entity ID's do not match)");
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            }
            if (originalItemType.ID != id)
            {
                TraceLog.TraceError("ItemTypeResource.Update: Bad Request (ID in URL does not match entity body)");
                return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.BadRequest);
            }

            if (originalItemType.UserID != CurrentUser.ID || newItemType.UserID != CurrentUser.ID)
            {   // itemtype does not belong to the authenticated user, return 403 Forbidden
                TraceLog.TraceError("ItemTypeResource.Update: Forbidden (entity does not belong to current user)");
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
                        TraceLog.TraceError("ItemTypeResource.Update: Internal Server Error (database operation did not succeed)");
                        return new HttpResponseMessageWrapper<ItemType>(req, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        TraceLog.TraceInfo("ItemTypeResource.Update: Accepted");
                        return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
                    }
                }
                else
                {
                    TraceLog.TraceInfo("ItemTypeResource.Update: Accepted (no changes)");
                    return new HttpResponseMessageWrapper<ItemType>(req, requestedItemType, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {   // itemtype not found - return 404 Not Found
                TraceLog.TraceError("ItemTypeResource.Update: Not Found; ex: " + ex.Message);
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