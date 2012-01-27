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
    using System.Web.Security;

    using Newtonsoft.Json;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHelpers;

    public class BaseResource
    {
        StorageContext storageContext = null;
        User currentUser = null;

        protected StorageContext StorageContext
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

        protected Guid CurrentUserID
        {
            get
            {
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.Name))
                {
                    if ((currentUser.ID == null || currentUser.ID == Guid.Empty))
                    {   // retrieve ID from storage
                        if (this.StorageContext.Users.Any<User>(u => u.Name == currentUser.Name))
                        {
                            User user = this.StorageContext.Users.Single<User>(u => u.Name == currentUser.Name);
                            currentUser.ID = user.ID;
                            return currentUser.ID;
                        }
                    }
                    else
                    {
                        return currentUser.ID;
                    }
                }
                return Guid.Empty;
            }
        }

        protected string CurrentUserName
        {
            get
            {
                return (currentUser != null) ? currentUser.Name : null;
            }
        }

        protected HttpStatusCode AuthenticateUser(HttpRequestMessage req)
        {
            LoggingHelper.TraceFunction();

            // TODO: get cookie auth working for all clients
            // this should work if auth cookie has been issued
            MembershipUser mu = Membership.GetUser();
            if (mu != null)
            {
                this.currentUser = new User() { Name = mu.UserName.ToLower(), ID = (Guid)mu.ProviderUserKey };
                return HttpStatusCode.OK;
            }

            User user = GetUserFromMessageHeaders(req);
            if (user == null)
            {   // auth headers not found, return 400 Bad Request
                LoggingHelper.TraceError("Bad request: no user information found");
                return HttpStatusCode.BadRequest;
            }

            try
            {   // authenticate the user
                if (Membership.ValidateUser(user.Name, user.Password) == false)
                    return HttpStatusCode.Forbidden;

                this.currentUser = user;
                this.currentUser.Password = null;   // remove password after it is validated
                return HttpStatusCode.OK;
            }
            catch (Exception)
            {   // username not found - return 404 Not Found
                return HttpStatusCode.NotFound;
            }
        }

        // extract username and password from message headers (passed by devices)
        // TODO: figure out how to use auth cookie from devices
        protected User GetUserFromMessageHeaders(HttpRequestMessage req)
        {
            LoggingHelper.TraceFunction();

            IEnumerable<string> values = new List<string>();
            if (req.Headers.TryGetValues("Zaplify-Username", out values) == false)
            {
                return null;
            }
            string username = values.ToArray<string>()[0];

            if (req.Headers.TryGetValues("Zaplify-Password", out values) == false)
            {
                return null;
            }
            string password = values.ToArray<string>()[0];

            return new User() { Name = username.ToLower(), Password = password };
        }

        // base code to process message and deserialize body to expected type
        protected object ProcessRequestBody(HttpRequestMessage req, Type t)
        {
            LoggingHelper.TraceFunction();

            if (req == null)
                return null;

            object value = null;
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
            }

            if (value == null)
            {   
                LoggingHelper.TraceError("ProcessRequestBody: content-type unrecognized: " + contentType);
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
                    UserID = CurrentUserID,
                    Username = CurrentUserName,
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