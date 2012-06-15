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
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<ItemType>(req, operation, code);  
            }

            // get the itemtype from the message body if one was passed
            ItemType clientItemType;
            if (req.Content.Headers.ContentLength > 0)
            {
                clientItemType = null;
                code = ProcessRequestBody<ItemType>(req, out clientItemType, out operation);
                if (code != HttpStatusCode.OK)  // error encountered processing body
                    return ReturnResult<ItemType>(req, operation, code);

                if (clientItemType.ID != id)
                {   // IDs must match
                    TraceLog.TraceError("ID in URL does not match entity body");
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                // otherwise get the client itemtype from the database
                try
                {
                    clientItemType = this.StorageContext.ItemTypes.Single<ItemType>(it => it.ID == id);
                    operation = this.StorageContext.CreateOperation(CurrentUser, req.Method.Method, null, clientItemType, null);
                }
                catch (Exception)
                {   // itemtype not found - it may have been deleted by someone else.  Return 200 OK.
                    TraceLog.TraceInfo("Entity not found, return OK");
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.OK);
                }
            }

            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Single<ItemType>(t => t.ID == id);
                if (requestedItemType.UserID != CurrentUser.ID)
                {   // requested itemType does not belong to the authenticated user, return 403 Forbidden
                    TraceLog.TraceError("Entity does not belong to current user)");
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.Forbidden);
                }

                this.StorageContext.ItemTypes.Remove(requestedItemType);
                if (this.StorageContext.SaveChanges() < 1)
                {
                    TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.InternalServerError);
                }
                else
                {
                    TraceLog.TraceInfo("Accepted");
                    return ReturnResult<ItemType>(req, operation, requestedItemType, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {   
                // itemtype not found - it may have been deleted by someone else.  Return 200 OK.
                TraceLog.TraceInfo(String.Format("Exception in database operation, return OK : Exception[{0}]", ex.Message));
                return ReturnResult<ItemType>(req, operation, HttpStatusCode.OK);
            }
        }

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ItemType>> GetItemTypes(HttpRequestMessage req)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<List<ItemType>>(req, operation, code);
            } 

            try
            {
                var itemTypes = this.StorageContext.ItemTypes.
                    Include("Fields").
                    Where(lt => lt.UserID == null || lt.UserID == CurrentUser.ID).
                    OrderBy(lt => lt.Name).
                    ToList<ItemType>();
                return ReturnResult<List<ItemType>>(req, operation, itemTypes, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {   // itemType not found - return 404 Not Found
                TraceLog.TraceException("Resource not found", ex);
                return ReturnResult<List<ItemType>>(req, operation, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> GetItemType(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<ItemType>(req, operation, code);
            } 

            // get the requested itemType
            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Include("Fields").Single<ItemType>(t => t.ID == id);

                if (requestedItemType.UserID != null && requestedItemType.UserID != CurrentUser.ID)
                {   // requested itemType does not belong to system or authenticated user, return 403 Forbidden
                    TraceLog.TraceError("Entity does not belong to current user)");
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.Forbidden);
                }

                return ReturnResult<ItemType>(req, operation, requestedItemType, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {   // itemType not found - return 404 Not Found
                TraceLog.TraceException("Resource not found", ex);
                return ReturnResult<ItemType>(req, operation, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> InsertItemType(HttpRequestMessage req)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<ItemType>(req, operation, code);
            } 

            // get the new itemType from the message body
            ItemType clientItemType = null;
            code = ProcessRequestBody(req, out clientItemType, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<ItemType>(req, operation, code);

            try
            {
                var itemType = this.StorageContext.ItemTypes.Add(clientItemType);
                if (itemType == null || this.StorageContext.SaveChanges() < 1)
                {
                    TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.InternalServerError);
                }
                else
                {
                    TraceLog.TraceInfo("Created");
                    return ReturnResult<ItemType>(req, operation, itemType, HttpStatusCode.Created);
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
                        TraceLog.TraceInfo("Accepted (entity already in database) : Exception[" + ex.Message + "]");
                        return ReturnResult<ItemType>(req, operation, dbItemType, HttpStatusCode.Accepted);
                    }
                    else
                    {
                        TraceLog.TraceException("Entity in database did not match)", ex);
                        return ReturnResult<ItemType>(req, operation, HttpStatusCode.Conflict);
                    }
                }
                catch (Exception e)
                {   // itemtype not inserted - return 409 Conflict
                    TraceLog.TraceException(String.Format("Entity was not in database) : Exception[{0}]", ex.Message), e);
                    return ReturnResult<ItemType>(req, operation, HttpStatusCode.Conflict);
                }
            }
        }
    
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<ItemType> UpdateItemType(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<ItemType>(req, operation, code);
            } 

            // the body will contain two ItemTypes - the original and the new values
            List<ItemType> itemTypes = null;
            code = ProcessRequestBody<List<ItemType>>(req, out itemTypes, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<ItemType>(req, operation, code);

            ItemType originalItemType = itemTypes[0];
            ItemType newItemType = itemTypes[1];

            // make sure the itemtype ID's match
            if (originalItemType.ID != id || newItemType.ID != id)
            {
                TraceLog.TraceError("ID in URL does not match entity body)");
                return ReturnResult<ItemType>(req, operation, HttpStatusCode.BadRequest);
            }

            try
            {
                ItemType requestedItemType = this.StorageContext.ItemTypes.Single<ItemType>(t => t.ID == id);
                bool changed = Update(requestedItemType, originalItemType, newItemType);
                if (changed == true)
                {
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        TraceLog.TraceError("Internal Server Error (database operation did not succeed)");
                        return ReturnResult<ItemType>(req, operation, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        TraceLog.TraceInfo("Accepted");
                        return ReturnResult<ItemType>(req, operation, requestedItemType, HttpStatusCode.Accepted);
                    }
                }
                else
                {
                    TraceLog.TraceInfo("Accepted (no changes)");
                    return ReturnResult<ItemType>(req, operation, requestedItemType, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {   // itemtype not found - return 404 Not Found
                TraceLog.TraceException("Resource not found", ex);
                return ReturnResult<ItemType>(req, operation, HttpStatusCode.NotFound);
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