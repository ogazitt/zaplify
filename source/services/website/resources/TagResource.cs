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

            // get the new tag from the message body
            Tag clientTag = ProcessRequestBody(req, typeof(Tag)) as Tag;

            // make sure the Tag ID's match
            if (clientTag.ID != id)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);

            // get the tag to be deleted
            try
            {
                Tag requestedTag = this.StorageContext.Tags.Include("ItemTags").Single<Tag>(t => t.ID == id);

                // if the requested tag does not belong to the authenticated user, return 403 Forbidden
                if (requestedTag.UserID != CurrentUserID)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);

                // delete all the itemtags associated with this item
                if (requestedTag.ItemTags != null && requestedTag.ItemTags.Count > 0)
                {
                    foreach (var tt in requestedTag.ItemTags.ToList())
                        this.StorageContext.ItemTags.Remove(tt);
                }

                this.StorageContext.Tags.Remove(requestedTag);
                int rows = this.StorageContext.SaveChanges();
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

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Tag>> Get(HttpRequestMessage req)
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
                    Where(lt => lt.UserID == null || lt.UserID == CurrentUserID).
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
                if (requestedTag.UserID != null && requestedTag.UserID != CurrentUserID)
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
                clientTag.UserID = CurrentUserID;
            if (clientTag.UserID != CurrentUserID)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);

            // add the new tag to the database
            try
            {
                var tag = this.StorageContext.Tags.Add(clientTag);
                int rows = this.StorageContext.SaveChanges();
                if (tag == null || rows < 1)
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
                    var dbTag = this.StorageContext.Tags.Single(t => t.ID == clientTag.ID);
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
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);

            // get the original and new Tags out of the message body
            Tag originalTag = clientTags[0];
            Tag newTag = clientTags[1];

            // make sure the tag ID's match
            if (originalTag.ID != newTag.ID)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);
            if (originalTag.ID != id)
                return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.BadRequest);

            // update the tag
            try
            {
                Tag requestedTag = this.StorageContext.Tags.Single<Tag>(t => t.ID == id);

                // if the Tag does not belong to the authenticated user, return 403 Forbidden
                if (requestedTag.UserID != CurrentUserID)
                    return new HttpResponseMessageWrapper<Tag>(req, HttpStatusCode.Forbidden);
                // reset the UserID fields to the appropriate user, to ensure update is done in the context of the current user
                originalTag.UserID = requestedTag.UserID;
                newTag.UserID = requestedTag.UserID;
                
                bool changed = Update(requestedTag, originalTag, newTag);
                if (changed == true)
                {
                    int rows = this.StorageContext.SaveChanges();
                    if (rows < 1)
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