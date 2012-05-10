namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Web;
    using System.Web.Security;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;

    public class BaseResource
    {
        const string authorizationHeader = "Authorization";
        const string authRequestHeader = "Cookie";
        protected UserStorageContext storageContext = null;
        User currentUser = null;

        public class BasicAuthCredentials 
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }

            public User AsUser()
            {
                return new User() { ID = this.ID, Name = this.Name, Email = this.Email };
            }
        }

        public User CurrentUser
        {
            get
            {
                if (currentUser == null)
                {   // get current user, ensure the ID is included
                    MembershipUser mu = Membership.GetUser();
                    currentUser = UserMembershipProvider.AsUser(mu);
                }
                return currentUser;
            }
        }

        public UserStorageContext StorageContext
        {
            get
            {
                if (storageContext == null)
                {
                    storageContext = Storage.NewUserContext;
                }
                return storageContext;
            }
        }

        protected HttpStatusCode AuthenticateUser(HttpRequestMessage req)
        {
            TraceLog.TraceFunction();

            // this should work if auth cookie has been provided
            MembershipUser mu = Membership.GetUser();
            if (mu != null && Membership.Provider is UserMembershipProvider)
            {   // get user id from authenticated identity (cookie)
                this.currentUser = UserMembershipProvider.AsUser(mu);
                return HttpStatusCode.OK;                
            }

            BasicAuthCredentials credentials = GetUserFromMessageHeaders(req);
            if (credentials == null)
            {
                if (HttpContext.Current.Request.Headers[authRequestHeader] != null)
                {   // cookie is no longer valid, return 401 Unauthorized
                    TraceLog.TraceError("AuthenticateUser: Unauthorized: cookie is expired or invalid");
                    return HttpStatusCode.Unauthorized;
                }
                
                // auth headers not found, return 400 Bad Request
                TraceLog.TraceError("AuthenticateUser: Bad request: no user information found");
                return HttpStatusCode.BadRequest;
            }

            try
            {   // authenticate the user
                if (Membership.ValidateUser(credentials.Name, credentials.Password) == false)
                {
                    TraceLog.TraceError("AuthenticateUser: Unauthorized: invalid username or password for user " + credentials.Name);
                    return HttpStatusCode.Forbidden;
                }

                mu = Membership.GetUser(credentials.Name, true);
                this.currentUser = UserMembershipProvider.AsUser(mu);

                if (Membership.Provider is UserMembershipProvider)
                {   // add auth cookie to response (cookie includes user id)
                    HttpCookie authCookie = UserMembershipProvider.CreateAuthCookie(this.currentUser);
                    HttpContext.Current.Response.Cookies.Add(authCookie);
                }

                TraceLog.TraceInfo(String.Format("AuthenticateUser: User {0} successfully logged in", credentials.Name));
                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {   // username not found - return 404 Not Found
                TraceLog.TraceException(String.Format("AuthenticateUser: Username not found: {0}", credentials.Name), ex);
                return HttpStatusCode.NotFound;
            }
        }

        // extract username and password from authorization header (passed by devices)
        protected BasicAuthCredentials GetUserFromMessageHeaders(HttpRequestMessage req)
        {
            TraceLog.TraceFunction();

            IEnumerable<string> header = new List<string>();
            if (!req.Headers.TryGetValues(authorizationHeader, out header) == false)
            {   // process basic authorization header
                string[] headerParts = header.ToArray<string>()[0].Split(' ');
                if (headerParts.Length > 1 && headerParts[0].Equals("Basic", StringComparison.OrdinalIgnoreCase))
                {
                    string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(headerParts[1]));
                    int firstColonIndex = credentials.IndexOf(':');
                    string username = credentials.Substring(0, firstColonIndex);
                    string password = credentials.Substring(firstColonIndex + 1);
                    return new BasicAuthCredentials() { Name = username.ToLower(), Password = password };
                }
            }
            return null;
        }

        // base code to process message and deserialize body to expected type
        protected HttpStatusCode ProcessRequestBody<T>(HttpRequestMessage req, out T entity, out Operation operation, bool skipOperation = false)
        {
            TraceLog.TraceFunction();
            operation = null;
            entity = default(T);
            Type t = typeof(T);
            if (req == null)
            {
                TraceLog.TraceError("ProcessRequestBody: null HttpRequestMessage");
                return HttpStatusCode.BadRequest;
            }

            try
            {
                string contentType = req.Content.Headers.ContentType.MediaType;
                switch (contentType)
                {
                    case "application/json":
                        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(t);
                        entity = (T) dcjs.ReadObject(req.Content.ReadAsStreamAsync().Result);
                        break;
                    case "text/xml":
                    case "application/xml":
                        DataContractSerializer dc = new DataContractSerializer(t);
                        entity = (T)dc.ReadObject(req.Content.ReadAsStreamAsync().Result);
                        break;
                    default:
                        TraceLog.TraceError("ProcessRequestBody: content-type unrecognized: " + contentType);
                        break;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessRequestBody: deserialization failed", ex);
            }

            if (skipOperation) return HttpStatusCode.OK;

            try
            {

                // initialize the body / oldbody
                object body = entity;
                object oldBody = null;

                // if this is an update, get the payload as a list
                switch (req.Method.Method)
                {
                    case "DELETE":
                    case "POST":
                        if (entity is UserOwnedEntity)
                        {
                            UserOwnedEntity currentEntity = entity as UserOwnedEntity;
                            // if the entity doesn't have a userid, assign it now to the current user
                            if (currentEntity.UserID == null || currentEntity.UserID == Guid.Empty)
                                currentEntity.UserID = CurrentUser.ID;
                            // if the entity does not belong to the authenticated user, return 403 Forbidden
                            if (currentEntity.UserID != CurrentUser.ID)
                            {
                                TraceLog.TraceError("ProcessRequestBody: Forbidden (entity does not belong to current user)");
                                return HttpStatusCode.Forbidden;
                            }

                            // fill out the ID if it's not set (e.g. from a javascript client)
                            if (currentEntity.ID == null || currentEntity.ID == Guid.Empty)
                                currentEntity.ID = Guid.NewGuid();
                        }
                        break;
                    case "PUT":
                        // body should contain two entities, the original and new values
                        IList list = (IList)entity;
                        if (list.Count != 2)
                        {
                            TraceLog.TraceError("ProcessRequestBody: Bad Request (malformed body)");
                            return HttpStatusCode.BadRequest;
                        }

                        oldBody = list[0];
                        body = list[1];
                        if (body is UserOwnedEntity && oldBody is UserOwnedEntity)
                        {
                            UserOwnedEntity oldEntity = oldBody as UserOwnedEntity;
                            UserOwnedEntity newEntity = body as UserOwnedEntity;

                            // make sure the entity ID's match
                            if (oldEntity.ID != newEntity.ID)
                            {
                                TraceLog.TraceError("ProcessRequestBody: Bad Request (original and new entity ID's do not match)");
                                return HttpStatusCode.BadRequest;
                            }

                            // if the entity doesn't have a userid, assign it now to the current user
                            if (oldEntity.UserID == null || oldEntity.UserID == Guid.Empty)
                                oldEntity.UserID = CurrentUser.ID;
                            if (newEntity.UserID == null || newEntity.UserID == Guid.Empty)
                                newEntity.UserID = CurrentUser.ID;
                            // make sure the entity belongs to the authenticated user
                            if (oldEntity.UserID != CurrentUser.ID || newEntity.UserID != CurrentUser.ID)
                            {
                                TraceLog.TraceError("ProcessRequestBody: Forbidden (entity does not belong to current user)");
                                return HttpStatusCode.Forbidden;
                            }
                        }
                        break;
                }

                // if the body is a BasicAuthCredentials, this likely means that we are in the process
                // of creating the user, and the CurrentUser property is null
                User user = null;
                if (body is BasicAuthCredentials)
                {
                    // create a user from the body so that the CreateOperation call can succeed
                    var userCred = body as BasicAuthCredentials;
                    user = userCred.AsUser();
                    // make sure the password doesn't get traced
                    userCred.Password = "";
                    // create an operation with the User as the body, not the BasicAuthCredentials entity 
                    body = user;
                }
                else
                    user = CurrentUser;
                if (user != null)
                    operation = this.StorageContext.CreateOperation(user, req.Method.Method, null, body, oldBody);

                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessRequestBody: request body processing failed", ex);
                return HttpStatusCode.BadRequest;
            }
        }

        protected HttpResponseMessageWrapper<T> ReturnResult<T>(HttpRequestMessage req, Operation operation, HttpStatusCode code)
        {
            try
            {
                if (operation != null)
                {
                    operation.StatusCode = (int?)code;
                    this.StorageContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ReturnResult: could not log operation status", ex);
            }
            return new HttpResponseMessageWrapper<T>(req, code);
        }

        protected HttpResponseMessageWrapper<T> ReturnResult<T>(HttpRequestMessage req, Operation operation, T t, HttpStatusCode code)
        {
            try
            {
                if (operation != null)
                {
                    operation.StatusCode = (int?)code;

                    // fix the EntityID (some clients like the web-client have the server assign the ID for new entities)
                    if (operation.EntityID == Guid.Empty)
                    {
                        ServerEntity entity = t as ServerEntity;
                        if (entity != null)
                            operation.EntityID = entity.ID;
                    }
                    
                    this.StorageContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ReturnResult: could not log operation status", ex);
            }
            return new HttpResponseMessageWrapper<T>(req, t, code);
        }
    }
}