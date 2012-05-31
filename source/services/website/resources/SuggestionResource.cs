namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Shared.Entities;
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
                var response = ReturnResult<Suggestion>(req, operation, suggestion, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception ex)
            {   // suggestion not found - return 404 Not Found
                TraceLog.TraceException("SuggestionResource.GetSuggestion: Not Found", ex);
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
            SuggestionFilter filter = null;
            code = ProcessRequestBody<SuggestionFilter>(req, out filter, out operation, true);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<List<Suggestion>>(req, operation, code);

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
                        Where(s => s.EntityID == filter.EntityID && (s.ReasonSelected == null || s.ReasonSelected == Reasons.Like)).
                        OrderBy(s => s.WorkflowInstanceID).OrderBy(s => s.GroupDisplayName).OrderBy(s => s.SortOrder).
                        ToList<Suggestion>();
                }
                else
                {
                    suggestions = this.SuggestionsStorageContext.Suggestions.
                        Where(s => s.EntityID == filter.EntityID && s.SuggestionType == filter.FieldName && (s.ReasonSelected == null || s.ReasonSelected == Reasons.Like)).
                        OrderBy(s => s.WorkflowInstanceID).OrderBy(s => s.GroupDisplayName).OrderBy(s => s.SortOrder).
                        ToList<Suggestion>();
                }
                var response = ReturnResult<List<Suggestion>>(req, operation, suggestions, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception ex)
            {   // suggestions not found - return 404 Not Found
                TraceLog.TraceException("SuggestionResource.QuerySuggestion: Internal Server Error", ex);
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
            List<Suggestion> suggestions = null;
            code = ProcessRequestBody<List<Suggestion>>(req, out suggestions, out operation);
            if (code != HttpStatusCode.OK)  // error encountered processing body
                return ReturnResult<Suggestion>(req, operation, code);

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
                        // if this suggestion was "Chosen", then invoke the workflow.  we don't need to invoke the workflow for
                        // suggestions that were ignored.
                        if (modified.ReasonSelected == Reasons.Chosen)
                            WorkflowHost.WorkflowHost.InvokeWorkflowForOperation(this.StorageContext, this.SuggestionsStorageContext, operation);

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
                TraceLog.TraceException("SuggestionResource.UpdateSuggestion: Not Found", ex);
                return ReturnResult<Suggestion>(req, operation, HttpStatusCode.NotFound);
            }
        }

        private bool Update(Suggestion current, Suggestion original, Suggestion modified)
        {
            bool updated = false;
            // only allow update of TimeSelected and ReasonSelected
            if (original.TimeSelected == null && current.TimeSelected == null && modified.TimeSelected.HasValue)
            {   // web client sets Date(0) to get server timestamp (ticks since 1970)
                if (modified.TimeSelected.Value.Year == 1970)
                {   current.TimeSelected = DateTime.UtcNow; }
                else
                {   current.TimeSelected = modified.TimeSelected; }
                updated = true;
            }
            if (original.TimeSelected.HasValue && current.TimeSelected.HasValue &&
                (original.TimeSelected == current.TimeSelected))
            {
                if (modified.TimeSelected.HasValue)
                {   // web client sets Date(0) to get server timestamp (ticks since 1970)
                    if (modified.TimeSelected.Value.Year == 1970)
                    { 
                        current.TimeSelected = DateTime.UtcNow;
                        updated = true;
                    }
                    else if (modified.TimeSelected.Value > current.TimeSelected.Value)
                    { 
                        current.TimeSelected = modified.TimeSelected;
                        updated = true;
                    }
                }
                else
                {
                    current.TimeSelected = null;
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