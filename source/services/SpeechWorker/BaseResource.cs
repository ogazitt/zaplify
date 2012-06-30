namespace BuiltSteady.Zaplify.SpeechWorker
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
    using System.Web.Http;
    using System.Web.Security;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;
    using System.Net.Http.Headers;

    public class BaseResource : ApiController
    {
        const string authorizationHeader = "Authorization";
        const string authRequestHeader = "Cookie";
        const string sessionHeader = "X-Zaplify-Session";
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
                    TraceLog.TraceError("Cookie is expired or invalid");
                    return HttpStatusCode.Unauthorized;
                }
                
                // auth headers not found, return 400 Bad Request
                TraceLog.TraceError("Bad request: no user information found");
                return HttpStatusCode.BadRequest;
            }

            try
            {   // authenticate the user
                if (Membership.ValidateUser(credentials.Name, credentials.Password) == false)
                {
                    TraceLog.TraceError("Invalid username or password for user " + credentials.Name);
                    return HttpStatusCode.Forbidden;
                }

                mu = Membership.GetUser(credentials.Name, true);
                this.currentUser = UserMembershipProvider.AsUser(mu);

                if (Membership.Provider is UserMembershipProvider)
                {   // add auth cookie to response (cookie includes user id)
                    HttpCookie authCookie = UserMembershipProvider.CreateAuthCookie(this.currentUser);
                    HttpContext.Current.Response.Cookies.Add(authCookie);
                }

                TraceLog.TraceInfo(String.Format("User {0} successfully logged in", credentials.Name));
                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {   // username not found - return 404 Not Found
                TraceLog.TraceException(String.Format("Username not found: {0}", credentials.Name), ex);
                return HttpStatusCode.NotFound;
            }
        }

        // extract username and password from authorization header (passed by devices)
        protected BasicAuthCredentials GetUserFromMessageHeaders(HttpRequestMessage req)
        {
            TraceLog.TraceFunction();

            IEnumerable<string> header = new List<string>();
            if (req.Headers.TryGetValues(authorizationHeader, out header))
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

        // return custom HttpResponseMessage over a typed MessageWrapper
        protected HttpResponseMessage CreateResponse<T>(HttpRequestMessage msg, HttpStatusCode statusCode)
        {
            MessageWrapper<T> messageWrapper = new MessageWrapper<T>() { StatusCode = statusCode };
            var resp = Request.CreateResponse(statusCode);

            if (IsWinPhone7(msg))
            {
                resp.StatusCode = HttpStatusCode.OK;

                // this constructor means no body, which indicates a non-200 series status code
                // since we switched the real HTTP status code to 200, we need to turn off caching 
                resp.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            }

            return resp;
        }

        protected HttpResponseMessage CreateResponse<T>(HttpRequestMessage msg, T type, HttpStatusCode statusCode)
        {
            MessageWrapper<T> messageWrapper = new MessageWrapper<T>() { StatusCode = statusCode, Value = type };
            var resp = Request.CreateResponse<MessageWrapper<T>>(statusCode, messageWrapper);
            if (IsWinPhone7(msg))
            {
                resp.StatusCode = HttpStatusCode.OK;
            }

            /*
            string messageText = resp.Content != null ? msg.Content.ReadAsStringAsync().Result : "(empty)";
            string tracemsg = String.Format(
                "\nWeb Response: Status: {0} {1}; Content-Type: {2}\n" +
                "Web Response Body: {3}",
                (int) statusCode,
                statusCode,
                msg.Content.Headers.ContentType,
                messageText);

            TraceLog.TraceLine(tracemsg, TraceLog.LogLevel.Detail);
             */
            
            return resp;
        }

        private static bool IsWinPhone7(HttpRequestMessage msg)
        {
            ProductInfoHeaderValue winPhone = null;

            try
            {
                if (msg.Headers.UserAgent != null)
                    winPhone = msg.Headers.UserAgent.FirstOrDefault(pi => pi.Product != null && pi.Product.Name == "Zaplify-WinPhone");
            }
            catch (Exception)
            {
                winPhone = null;
            }
            return winPhone != null ? true : false;
        }
    }

    // wrapper around the type which contains the status code
    public class MessageWrapper<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public T Value { get; set; }
    }
}