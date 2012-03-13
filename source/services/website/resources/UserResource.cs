﻿namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.Security;
    using Microsoft.ApplicationServer.Http;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;

    [ServiceContract]
    [LogMessages]
    public class UserResource : BaseResource
    {

        [WebInvoke(Method = "POST", UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<User> CreateUser(HttpRequestMessage req)
        {
            HttpStatusCode status = HttpStatusCode.BadRequest;
            // get the new user from the message body (password is not deserialized)
            BasicAuthCredentials newUser = ProcessRequestBody(req, typeof(BasicAuthCredentials)) as BasicAuthCredentials;
            // get password from message headers
            BasicAuthCredentials userCreds = GetUserFromMessageHeaders(req);

            if (newUser.Name == userCreds.Name)
            {   // verify same name in both body and header
                newUser.Password = userCreds.Password;
                status = CreateUser(newUser);
            }

            if (status == HttpStatusCode.Created)
            {
                return new HttpResponseMessageWrapper<User>(req, newUser.AsUser(), HttpStatusCode.Created);
            }
            else
                return new HttpResponseMessageWrapper<User>(req, status);
        }

        private HttpStatusCode CreateUser(BasicAuthCredentials user)
        {
            MembershipCreateStatus createStatus;
            TraceLog.TraceFunction();  // log function entrance

            try
            {   // create new user account using the membership provider
                MembershipUser mu = Membership.CreateUser(user.Name, user.Password, user.Email, null, null, true, user.ID, out createStatus);
                if (createStatus == MembershipCreateStatus.Success)
                {
                    return HttpStatusCode.Created;
                }
                if (createStatus == MembershipCreateStatus.DuplicateUserName)
                {
                    return HttpStatusCode.Conflict;
                }                
                if (createStatus == MembershipCreateStatus.InvalidUserName ||
                    createStatus == MembershipCreateStatus.InvalidEmail || 
                    createStatus == MembershipCreateStatus.InvalidPassword)
                {
                    return HttpStatusCode.NotAcceptable;
                } 
            }
            catch (Exception)
            { }

            TraceLog.TraceError("Failed to create new user account: " + user.Name);
            return HttpStatusCode.Conflict;
        }


        // this would be way more efficient with a single sproc which validates all constraints
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        public HttpResponseMessageWrapper<User> DeleteUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user is not authenticated
                return new HttpResponseMessageWrapper<User>(req, code);
            }

            // get current user from the message body
            User requestedUser = ProcessRequestBody(req, typeof(User)) as User;
            if (requestedUser.ID != id)
            {   // verify user id in request body matches id being deleted
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);
            }

            // verify credentials passed in headers match the user in request body
            // disallows one user deleting another user (may want to allow in future with proper permissions)
            if (requestedUser.Name.Equals(CurrentUser.Name, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);
            }

            try
            {
                // verify user id in storage matches id being deleted
                User storedUser = this.StorageContext.Users.Single<User>(u => u.Name == requestedUser.Name.ToLower());
                if (storedUser.ID != requestedUser.ID)
                {
                    return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.Forbidden);
                }
                
                if (Membership.DeleteUser(CurrentUser.Name))
                {   // delete user and all related data
                    return new HttpResponseMessageWrapper<User>(req, requestedUser, HttpStatusCode.Accepted);
                }
            }
            catch (Exception)
            { }
            return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
        }

        [WebGet(UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<User> GetCurrentUser(HttpRequestMessage req)
        {

            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<User>(req, code);  
            }

            try
            {
                UserDataModel model = new UserDataModel(this);
                // make sure the response isn't cached
                var response = new HttpResponseMessageWrapper<User>(req, model.UserData, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception)
            {   // no such user account - return 404 Not Found
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.NotFound);               
            }
        }

        // TODO: why would we allow one user to access data for another user? Can we remove this method?
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<User> GetUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<User>(req, code);
            }

            try
            {
                User requestedUser = this.StorageContext.Users.Single<User>(u => u.ID == id);
                return new HttpResponseMessageWrapper<User>(req, requestedUser, code);
            }
            catch (Exception)
            {   // no such user account - return 404 Not Found
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "{id}/folderitems")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<Folder>> GetFoldersForUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<List<Folder>>(req, code);  
            }

            try
            {
                User requestedUser = this.StorageContext.Users.Single<User>(u => u.ID == id);

                // if the requested user is not the same as the authenticated user, return 403 Forbidden
                if (requestedUser.ID != CurrentUser.ID)
                {
                    return new HttpResponseMessageWrapper<List<Folder>>(req, HttpStatusCode.Forbidden);
                }
                else
                {
                    try
                    {
                        var folderitems = this.StorageContext.Folders.
                            Include("FolderUsers").
                            Include("Items.ItemTags").
                            Include("Items.FieldValues").
                            Where(f => f.FolderUsers.Any(fu => fu.UserID == id)).
                            ToList();
                        var response = new HttpResponseMessageWrapper<List<Folder>>(req, folderitems, HttpStatusCode.OK);
                        response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                        return response;
                    }
                    catch (Exception)
                    {   // items not found - return 404 Not Found
                        return new HttpResponseMessageWrapper<List<Folder>>(req, HttpStatusCode.NotFound);
                    }
                }
            }
            catch (Exception)
            {   // no such user account - return 404 Not Found
                return new HttpResponseMessageWrapper<List<Folder>>(req, HttpStatusCode.NotFound);
            }
        }

        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        public HttpResponseMessageWrapper<User> UpdateUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<User>(req, code);
            }

            // verify body contains two sets of user data - the original values and the new values
            List<BasicAuthCredentials> userCreds = ProcessRequestBody(req, typeof(List<BasicAuthCredentials>)) as List<BasicAuthCredentials>;
            if (userCreds.Count != 2)
            {
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);
            }

            // get the original and new items out of the message body
            BasicAuthCredentials originalUserData = userCreds[0];
            BasicAuthCredentials newUserData = userCreds[1];

            // make sure the item ID's match
            if (originalUserData.ID != newUserData.ID || originalUserData.ID != id)
            {
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);
            }
       
            try
            {   // TODO: should we allow changing of username?

                // check to make sure the old password passed in the message is valid
                if (!Membership.ValidateUser(CurrentUser.Name, originalUserData.Password))
                {   
                    return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.Forbidden);
                }

                MembershipUser mu = Membership.GetUser(originalUserData.Name);
                if (!mu.Email.Equals(newUserData.Email, StringComparison.OrdinalIgnoreCase))
                {   
                    mu.Email = newUserData.Email;
                    Membership.UpdateUser(mu);   
                }
                if (originalUserData.Password != newUserData.Password)
                {   
                    mu.ChangePassword(originalUserData.Password, newUserData.Password);    
                }

                User currentUser = this.StorageContext.Users.Single<User>(u => u.ID == CurrentUser.ID);
                return new HttpResponseMessageWrapper<User>(req, currentUser, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
            }
        }
    }
}