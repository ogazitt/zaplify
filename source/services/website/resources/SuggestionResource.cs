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
    public class SuggestionResource : BaseResource
    {
        SuggestionsStorageContext suggestionsContext;
        public SuggestionsStorageContext SuggestionsStorageContext
        {
            get
            {
                if (suggestionsContext == null)
                {
                    suggestionsContext = Storage.NewSuggestionsContext;
                }
                return suggestionsContext;
            }
        }

        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        [LogMessages]
        public HttpResponseMessageWrapper<Suggestion> DeleteSuggestion(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Suggestion>(req, operation, code);  
            }

            // DELETE is not supported for Suggestions
            return ReturnResult<Suggestion>(req, operation, HttpStatusCode.NotImplemented);  
        }

        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<Suggestion> GetSuggestion(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Suggestion>(req, operation, code);
            }

            try
            {
                Suggestion suggestion = this.SuggestionsStorageContext.Suggestions.Single<Suggestion>(s => s.ID == id);
                if (!ValidateEntityOwnership(suggestion.EntityID, suggestion.EntityType))
                {   // entity associated with suggestions does not belong to the authenticated user, return 403 Forbidden
                    TraceLog.TraceError("SuggestionResource.GetSuggestion: Forbidden (associated entity does not belong to current user)");
                    return ReturnResult<Suggestion>(req, operation, HttpStatusCode.Forbidden);
                }                
                return ReturnResult<Suggestion>(req, operation, suggestion, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {   // suggestion not found - return 404 Not Found
                TraceLog.TraceError("SuggestionResource.GetSuggestion: Not Found; ex: " + ex.Message);
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.NotFound);
            }
        }

        public class SuggestionFilter
        {
            public Guid EntityID { get; set; }
            public string EntityType { get; set; }
            public string FieldName { get; set; }   
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Suggestion>> QuerySuggestions(HttpRequestMessage req)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<List<Suggestion>>(req, operation, code);
            } 

            // get the filter from message body
            SuggestionFilter filter = ProcessRequestBody(req, typeof(SuggestionFilter), out operation, true) as SuggestionFilter;

            if (!ValidateEntityOwnership(filter.EntityID, filter.EntityType))
            {   // entity being queried does not belong to authenticated user, return 403 Forbidden
                TraceLog.TraceError("SuggestionResource.QuerySuggestions: Forbidden (queried entity does not belong to current user)");
                return ReturnResult<List<Suggestion>>(req, operation, HttpStatusCode.Forbidden);
            }

            try
            {
                List<Suggestion> suggestions;
                if (filter.FieldName == null)
                {
                    suggestions = this.SuggestionsStorageContext.Suggestions.
                        Where(s => s.EntityID == filter.EntityID && s.TimeSelected == null).
                        OrderBy(s => s.WorkflowInstanceID).OrderBy(s => s.State).
                        ToList<Suggestion>();
                }
                else
                {
                    suggestions = this.SuggestionsStorageContext.Suggestions.
                        Where(s => s.EntityID == filter.EntityID && s.FieldName == filter.FieldName && s.TimeSelected == null).
                        OrderBy(s => s.WorkflowInstanceID).OrderBy(s => s.State).
                        ToList<Suggestion>();
                }
                return ReturnResult<List<Suggestion>>(req, operation, suggestions, HttpStatusCode.OK);
            }
            catch (Exception)
            {   // suggestions not found - return 404 Not Found
                TraceLog.TraceError("SuggestionResource.QuerySuggestion: Internal Server Error (database operation did not succeed)");
                return ReturnResult<List<Suggestion>>(req, operation, HttpStatusCode.InternalServerError);
            }
        }
    
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        [LogMessages]
        public HttpResponseMessageWrapper<Suggestion> UpdateSuggestion(HttpRequestMessage req, Guid id)
        {
            Operation operation = null;
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return ReturnResult<Suggestion>(req, operation, code);
            } 

            // the body will contain two Suggestions - the original and the new values
            List<Suggestion> suggestions = ProcessRequestBody(req, typeof(List<Suggestion>), out operation, true) as List<Suggestion>;
            if (suggestions.Count != 2)
            {   // body should contain two Suggestions, the original and new values
                TraceLog.TraceError("SuggestionResource.UpdateSuggestion: Bad Request (malformed body)");
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.BadRequest);
            }

            Suggestion original = suggestions[0];
            Suggestion modified = suggestions[1];

            if (original.ID != modified.ID)
            {   // suggestion IDs must match
                TraceLog.TraceError("SuggestionResource.UpdateSuggestion: Bad Request (original and new suggestion ID's do not match)");
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.BadRequest);
            }
            if (original.ID != id)
            {
                TraceLog.TraceError("SuggestionResource.Update: Bad Request (ID in URL does not match suggestion body)");
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.BadRequest);
            }
            if (original.EntityID != modified.EntityID)
            {   // entity IDs must match
                TraceLog.TraceError("SuggestionResource.UpdateSuggestion: Bad Request (original and new entity ID's do not match)");
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.BadRequest);
            }

            if (!ValidateEntityOwnership(modified.EntityID, modified.EntityType))
            {   // entity associated with suggestions does not belong to the authenticated user, return 403 Forbidden
                TraceLog.TraceError("SuggestionResource.UpdateSuggestion: Forbidden (associated entity does not belong to current user)");
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.Forbidden);
            }

            try
            {
                Suggestion current = this.SuggestionsStorageContext.Suggestions.Single<Suggestion>(t => t.ID == id);
                bool changed = Update(current, original, modified);
                if (changed == true)
                {
                    if (this.SuggestionsStorageContext.SaveChanges() < 1)
                    {
                        TraceLog.TraceError("SuggestionResource.UpdateSuggestion: Internal Server Error (database operation did not succeed)");
                        return ReturnResult<Suggestion>(req, operation, HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        TraceLog.TraceInfo("SuggestionResource.UpdateSuggestion: Accepted");
                        return ReturnResult<Suggestion>(req, operation, current, HttpStatusCode.Accepted);
                    }
                }
                else
                {
                    TraceLog.TraceInfo("SuggestionResource.UpdateSuggestion: Accepted (no changes)");
                    return ReturnResult<Suggestion>(req, operation, current, HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {   // suggestion not found - return 404 Not Found
                TraceLog.TraceError("SuggestionResource.UpdateSuggestion: Not Found; ex: " + ex.Message);
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Suggestion current, Suggestion original, Suggestion modified)
        {
            bool updated = false;
            // only allow update of TimeSelected and ReasonSelected
            if (original.TimeSelected == null && current.TimeSelected == null && modified.TimeSelected.HasValue)
            {
                current.TimeSelected = modified.TimeSelected;
                updated = true;
            }
            if (original.TimeSelected.HasValue && current.TimeSelected.HasValue)
            {
                if (original.TimeSelected == current.TimeSelected &&
                    (modified.TimeSelected == null || modified.TimeSelected > current.TimeSelected))
                {
                    current.TimeSelected = modified.TimeSelected;
                    updated = true;
                }
            }
            if (original.ReasonSelected == current.ReasonSelected &&
                current.ReasonSelected != modified.ReasonSelected)
            {
                current.ReasonSelected = modified.ReasonSelected;
                updated = true;
            }
            return updated;
        }

        private bool ValidateEntityOwnership(Guid entityID, string entityType)
        {
            try
            {
                if (entityType.Equals(typeof(User).Name))
                {
                    return (entityID == this.CurrentUser.ID);
                }
                if (entityType.Equals(typeof(Folder).Name))
                {
                    Folder folder = this.StorageContext.Folders.Single<Folder>(f => f.ID == entityID);
                    return (folder.UserID == this.CurrentUser.ID);
                }
                if (entityType.Equals(typeof(Item).Name))
                {
                    Item item = this.StorageContext.Items.Single<Item>(i => i.ID == entityID);
                    return (item.UserID == this.CurrentUser.ID);
                }
            }
            catch (Exception) { }
            return false;
        }
    }
}