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
                TraceLog.TraceError(String.Format("AuthenticateUser: Username not found: {0}; ex: {1}", credentials.Name, ex.Message));
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
        protected object ProcessRequestBody(HttpRequestMessage req, Type t, out Operation operation, bool skipOperation = false)
        {
            TraceLog.TraceFunction();
            operation = null;
            if (req == null)
                return null;

            object value = null;

            try
            {
                string contentType = req.Content.Headers.ContentType.MediaType;
                switch (contentType)
                {
                    case "application/json":
                        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(t);
                        value = dcjs.ReadObject(req.Content.ReadAsStreamAsync().Result);
                        break;
                    case "text/xml":
                    case "application/xml":
                        DataContractSerializer dc = new DataContractSerializer(t);
                        value = dc.ReadObject(req.Content.ReadAsStreamAsync().Result);
                        break;
                    default:
                        TraceLog.TraceError("ProcessRequestBody: content-type unrecognized: " + contentType);
                        break;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("ProcessRequestBody: deserialization failed: " + ex.Message);
            }

            if (skipOperation) return value;

            try
            {   
                // log the operation in the operations table

                // initialize the body / oldbody
                object body = value;
                object oldBody = null;
                Type bodyType = t;

                // if this is an update, get the payload as a list
                if (req.Method == HttpMethod.Put)
                {
                    IList list = (IList)value;
                    oldBody = list[0];
                    body = list[1];
                    bodyType = body.GetType();
                }

                string name;
                Guid id = (Guid)bodyType.GetProperty("ID").GetValue(body, null);
                if (body is Suggestion)
                {   // Suggestion does not have a Name property, use State property
                    name = (string)bodyType.GetProperty("GroupDisplayName").GetValue(body, null);
                }
                else
                {
                    name = (string)bodyType.GetProperty("Name").GetValue(body, null);
                }


                // record the operation in the Operations table
                operation = new Operation()
                {
                    ID = Guid.NewGuid(),
                    UserID = CurrentUser.ID,
                    Username = CurrentUser.Name,
                    EntityID = id,
                    EntityName = name,
                    EntityType = bodyType.Name,
                    OperationType = req.Method.Method,
                    Body = JsonSerializer.Serialize(body),
                    OldBody = JsonSerializer.Serialize(oldBody),
                    Timestamp = DateTime.Now
                };
                this.StorageContext.Operations.Add(operation);
                if (this.StorageContext.SaveChanges() < 1)
                {   // log failure to record operation
                    TraceLog.TraceError("ProcessRequestBody: failed to record operation: " + req.Method.Method);
                }
            }
            catch (Exception ex)
            {   // log failure to record operation
                TraceLog.TraceError("ProcessRequestBody: failed to record operation: " + ex.Message);
            }

            return value;
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