namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization.Json;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web;
    using System.Web.Security;

    using Newtonsoft.Json;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;

    public class BaseResource
    {
        const string authorizationHeader = "Authorization";
        const string authRequestHeader = "Cookie";
        protected StorageContext storageContext = null;
        User currentUser = null;

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

        public StorageContext StorageContext
        {
            get
            {
                if (storageContext == null)
                {
                    storageContext = Storage.NewContext;
                }
                return storageContext;
            }
        }

        protected HttpStatusCode AuthenticateUser(HttpRequestMessage req)
        {
            LoggingHelper.TraceFunction();

            // this should work if auth cookie has been provided
            MembershipUser mu = Membership.GetUser();
            if (mu != null && Membership.Provider is UserMembershipProvider)
            {   // get user id from authenticated identity (cookie)
                this.currentUser = UserMembershipProvider.AsUser(mu);
                return HttpStatusCode.OK;                
            }

            UserCredential credentials = GetUserFromMessageHeaders(req);
            if (credentials == null)
            {
                if (HttpContext.Current.Request.Headers[authRequestHeader] != null)
                {   // cookie is no longer valid, return 401 Unauthorized
                    LoggingHelper.TraceError("AuthenticateUser: Unauthorized: cookie is expired or invalid");
                    return HttpStatusCode.Unauthorized;
                }
                
                // auth headers not found, return 400 Bad Request
                LoggingHelper.TraceError("AuthenticateUser: Bad request: no user information found");
                return HttpStatusCode.BadRequest;
            }

            try
            {   // authenticate the user
                if (Membership.ValidateUser(credentials.Name, credentials.Password) == false)
                {
                    LoggingHelper.TraceError("AuthenticateUser: Unauthorized: invalid username or password for user " + credentials.Name);
                    return HttpStatusCode.Forbidden;
                }

                this.currentUser = credentials.AsUser();
                if (this.currentUser.ID == null || this.currentUser.ID == Guid.Empty)
                {   // ensure ID is included
                    mu = Membership.GetUser(currentUser.Name, true);
                    this.currentUser = UserMembershipProvider.AsUser(mu);
                }

                if (Membership.Provider is UserMembershipProvider)
                {   // add auth cookie to response (cookie includes user id)
                    HttpCookie authCookie = UserMembershipProvider.CreateAuthCookie(credentials);
                    HttpContext.Current.Response.Cookies.Add(authCookie);
                }

                LoggingHelper.TraceInfo(String.Format("AuthenticateUser: User {0} successfully logged in", credentials.Name));
                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {   // username not found - return 404 Not Found
                LoggingHelper.TraceError(String.Format("AuthenticateUser: Username not found: {0}; ex: {1}", credentials.Name, ex.Message));
                return HttpStatusCode.NotFound;
            }
        }

        // extract username and password from authorization header (passed by devices)
        protected UserCredential GetUserFromMessageHeaders(HttpRequestMessage req)
        {
            LoggingHelper.TraceFunction();

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
                    return new UserCredential() { Name = username.ToLower(), Password = password };
                }
            }
            return null;
        }

        // base code to process message and deserialize body to expected type
        protected object ProcessRequestBody(HttpRequestMessage req, Type t)
        {
            LoggingHelper.TraceFunction();

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
                        LoggingHelper.TraceError("ProcessRequestBody: content-type unrecognized: " + contentType);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.TraceError("ProcessRequestBody: deserialization failed: " + ex.Message);
            }

            try
            {   // log the operation in the operations table, using new storage context
                StorageContext storage = Storage.NewContext;

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

                Guid id = (Guid)bodyType.GetProperty("ID").GetValue(body, null);
                string name = (string)bodyType.GetProperty("Name").GetValue(body, null);

                // record the operation in the Operations table
                Operation op = new Operation()
                {
                    ID = Guid.NewGuid(),
                    UserID = CurrentUser.ID,
                    Username = CurrentUser.Name,
                    EntityID = id,
                    EntityName = name,
                    EntityType = bodyType.Name,
                    OperationType = req.Method.Method,
                    Body = JsonSerialize(body),
                    OldBody = JsonSerialize(oldBody),
                    Timestamp = DateTime.Now
                };
                storage.Operations.Add(op);
                if (storage.SaveChanges() < 1)
                {   // log failure to record operation
                    LoggingHelper.TraceError("ProcessRequestBody: failed to record operation: " + req.Method.Method);
                }
            }
            catch (Exception ex)
            {   // log failure to record operation
                LoggingHelper.TraceError("ProcessRequestBody: failed to record operation: " + ex.Message);
            }

            return value;
        }

        private static string JsonSerialize(object body)
        {
            return JsonConvert.SerializeObject(body);
        }
    }
}