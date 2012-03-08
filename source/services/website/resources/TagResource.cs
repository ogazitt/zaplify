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
    public class TagResource : BaseResource
    {

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> DeleteTag(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Tag>(req, code);  
            }

            // get the tag from the message body if one was passed
            Tag clientTag;
            if (req.Content.Headers.ContentLength > 0)
            {
                clientTag = clientTag = ProcessRequestBody(req, typeof(Tag)) as Tag;
                if (clientTag.ID != id)
                {   // IDs must match
                    LoggingHelper.TraceError("TagResource.Delete: Bad Request (ID in URL does not match entity body)");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                // otherwise get the client tag from the database
                try
                {
                    clientTag = this.StorageContext.Tags.Single<Tag>(t => t.ID == id);
                }
                catch (Exception)
                {   // tag not found - it may have been deleted by someone else.  Return 200 OK.
                    LoggingHelper.TraceInfo("TagResource.Delete: entity not found; returned OK anyway");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.OK);
                }
            }

            // get the tag to be deleted
            try
            {
                Tag requestedTag = this.StorageContext.Tags.Include("ItemTags").Single<Tag>(t => t.ID == id);
                if (requestedTag.UserID != CurrentUser.ID)
                {   // requested tag does not belong to the authenticated user, return 403 Forbidden
                    LoggingHelper.TraceError("TagResource.Delete: Forbidden (entity does not belong to current user)");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
                }

                // delete all the itemtags associated with this item
                if (requestedTag.ItemTags != null && requestedTag.ItemTags.Count > 0)
                {
                    foreach (var tt in requestedTag.ItemTags.ToList())
                        this.StorageContext.ItemTags.Remove(tt);
                }

                this.StorageContext.Tags.Remove(requestedTag);
                if (this.StorageContext.SaveChanges() < 1)
                {
                    LoggingHelper.TraceError("TagResource.Delete: Internal Server Error (database operation did not succeed)");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.InternalServerError);
                }
                else
                {
                    LoggingHelper.TraceInfo("TagResource.Delete: Accepted");
                    return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {
                // tag not found - it may have been deleted by someone else.  Return 200 OK.
                LoggingHelper.TraceInfo(String.Format("TagResource.Delete: exception in database operation: {0}; returned OK anyway", ex.Message));
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.OK);
            }
        }

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Tag>> GetTags(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<List<Tag>>(req, code);
            }

            // get all Tags
            try
            {
                var tags = this.StorageContext.Tags.
                    Include("Fields").
                    Where(lt => lt.UserID == null || lt.UserID == CurrentUser.ID).
                    OrderBy(lt => lt.Name).
                    ToList<Tag>();
                return new HttpResponseMessageWrapper<List<Tag>>(req, tags, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                // tag not found - return 404 Not Found
                LoggingHelper.TraceError("TagResource.GetTags: Not Found; ex: " + ex.Message);
                return new HttpResponseMessageWrapper<List<Tag>>(req, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> GetTag(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Tag>(req, code);
            }

            // get the requested tag
            try
            {
                Tag requestedTag = this.StorageContext.Tags.Include("Fields").Single<Tag>(t => t.ID == id);

                // if the requested tag is not generic (i.e. UserID == 0), 
                // and does not belong to the authenticated user, return 403 Forbidden, otherwise return the tag
                if (requestedTag.UserID != null && requestedTag.UserID != CurrentUser.ID)
                {
                    LoggingHelper.TraceError("TagResource.GetItemType: Forbidden (entity does not belong to current user)");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
                }

                return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                // tag not found - return 404 Not Found
                LoggingHelper.TraceError("TagResource.GetItemType: Not Found; ex: " + ex.Message);
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> InsertTag(HttpRequestMessage req)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Tag>(req, code);
            }

            // get the new tag from the message body
            Tag clientTag = ProcessRequestBody(req, typeof(Tag)) as Tag;

            // if the requested tag does not belong to the authenticated user, return 403 Forbidden, otherwise return the tag
            if (clientTag.UserID == null || clientTag.UserID == Guid.Empty)
                clientTag.UserID = CurrentUser.ID;
            if (clientTag.UserID != CurrentUser.ID)
            {
                LoggingHelper.TraceError("TagResource.Insert: Forbidden (entity does not belong to current user)");
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
            }

            // add the new tag to the database
            try
            {
                var tag = this.StorageContext.Tags.Add(clientTag);
                if (tag == null || this.StorageContext.SaveChanges() < 1)
                {
                    LoggingHelper.TraceError("TagResource.Insert: Internal Server Error (database operation did not succeed)");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.InternalServerError);
                }
                else
                {
                    LoggingHelper.TraceInfo("TagResource.Insert: Created");
                    return new HttpResponseMessageWrapper<Tag>(req, tag, HttpStatusCode.Created);
                }
            }
            catch (Exception ex)
            {
                // check for the condition where the tag is already in the database
                // in that case, return 202 Accepted; otherwise, return 409 Conflict
                try
                {
                    var dbTag = this.StorageContext.Tags.Single(t => t.ID == clientTag.ID);
                    if (dbTag.Name == clientTag.Name)
                    {
                        LoggingHelper.TraceInfo("TagResource.Insert: Accepted (entity already in database); ex: " + ex.Message);
                        return new HttpResponseMessageWrapper<Tag>(req, dbTag, HttpStatusCode.Accepted);
                    }
                    else
                    {
                        LoggingHelper.TraceError("TagResource.Insert: Conflict (entity in database did not match); ex: " + ex.Message);
                        return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Conflict);
                    }
                }
                catch (Exception e)
                {
                    // tag not inserted - return 409 Conflict
                    LoggingHelper.TraceError(String.Format("TagResource.Insert: Conflict (entity was not in database); ex: {0}, ex {1}", ex.Message, e.Message));
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Conflict);
                }
            }
        }
    
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Tag> UpdateTag(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<Tag>(req, code);
            }

            // the body will be two Tags - the original and the new values.  Verify this
            List<Tag> clientTags = ProcessRequestBody(req, typeof(List<Tag>)) as List<Tag>;
            if (clientTags.Count != 2)
            {
                LoggingHelper.TraceError("TagResource.Update: Bad Request (malformed body)");
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);
            }

            // get the original and new Tags out of the message body
            Tag originalTag = clientTags[0];
            Tag newTag = clientTags[1];

            // make sure the tag ID's match
            if (originalTag.ID != newTag.ID)
            {
                LoggingHelper.TraceError("TagResource.Update: Bad Request (original and new entity ID's do not match)");
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);
            }
            if (originalTag.ID != id)
            {
                LoggingHelper.TraceError("TagResource.Update: Bad Request (ID in URL does not match entity body)");
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);
            }

            if (originalTag.UserID != CurrentUser.ID || newTag.UserID != CurrentUser.ID)
            {   // tag does not belong to the authenticated user, return 403 Forbidden
                LoggingHelper.TraceError("TagResource.Update: Forbidden (entity does not belong to current user)");
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
            }

            // update the tag
            try
            {
                Tag requestedTag = this.StorageContext.Tags.Single<Tag>(t => t.ID == id);

                // if the Tag does not belong to the authenticated user, return 403 Forbidden
                if (requestedTag.UserID != CurrentUser.ID)
                {
                    LoggingHelper.TraceError("TagResource.Update: Forbidden (entity does not belong to current user)");
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
                }
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalTag.UserID = requestedTag.UserID;
                newTag.UserID = requestedTag.UserID;
                
                bool changed = Update(requestedTag, originalTag, newTag);
                if (changed == true)
                {
                    if (this.StorageContext.SaveChanges() < 1)
                    {
                        LoggingHelper.TraceError("TagResource.Update: Internal Server Error (database operation did not succeed)");
                        return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        LoggingHelper.TraceInfo("TagResource.Update: Accepted");
                        return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.Accepted);
                    }
                }
                else
                {
                    LoggingHelper.TraceInfo("TagResource.Update: Accepted (no changes)");
                    return new HttpResponseMessageWrapper<Tag>(req, requestedTag, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {
                // tag not found - return 404 Not Found
                LoggingHelper.TraceError("TagResource.Update: Not Found; ex: " + ex.Message);
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