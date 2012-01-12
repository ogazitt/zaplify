using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ApplicationServer.Http;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using BuiltSteady.Zaplify.Website.Helpers;
using BuiltSteady.Zaplify.Website.Models;
using System.Configuration;
using System.Web.Configuration;
using System.Web.Security;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.Website.Resources
{
    [ServiceContract]
    [LogMessages]
    public class UserResource
    {
        private ZaplifyStore ZaplifyStore
        {
            get
            {
                return new ZaplifyStore();
            }
        }

        /// <summary>
        /// Create a user 
        /// </summary>
        /// <returns>new User info</returns>
        [WebInvoke(Method = "POST", UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<User> CreateUser(HttpRequestMessage req)
        {
            ZaplifyStore zaplifystore = ZaplifyStore;

            // get the new user from the message body
            User user = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(User)) as User;

            try
            {
                // try to find the user - if already exists, return 409 Conflict
                var dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name);

                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.Conflict);
            }
            catch (Exception)
            {
                // this is the expected path - the user doesn't yet exist
                MembershipCreateStatus createStatus;
                HttpStatusCode status = ResourceHelper.CreateUser(zaplifystore, user, out createStatus);
                if (status == HttpStatusCode.Created)
                    return new HttpResponseMessageWrapper<User>(req, user, HttpStatusCode.Created);
                else
                    return new HttpResponseMessageWrapper<User>(req, status);
            }
        }

        /// <summary>
        /// Delete a user 
        /// </summary>
        /// <param name="id">id for the user to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "DELETE")]
        public HttpResponseMessageWrapper<User> DeleteUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<User>(req, code);  // user not authenticated

            // get the User from the message body
            User clientUser = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(User)) as User;

            // make sure the User ID's match
            if (clientUser.ID != id)
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;
            User user = ResourceHelper.GetUserPassFromMessage(req);

            try
            {
                // remove the user from the membership service
                if (Membership.DeleteUser(user.Name) == false)
                    return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);

                // remove the user data from the ZaplifyStore database
                User dbUser = zaplifystore.Users.
                    Include("ItemTypes.Fields").
                    Include("Tags").
                    Include("ItemLists.Items.ItemTags").
                    Single<User>(u => u.Name == user.Name);
                zaplifystore.Users.Remove(dbUser);
                int rows = zaplifystore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<User>(req, dbUser, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get the current user's info
        /// </summary>
        /// <returns>User structure</returns>
        [WebGet(UriTemplate = "")]
        [LogMessages]
        public HttpResponseMessageWrapper<User> GetCurrentUser(HttpRequestMessage req)
        {
            User user = null;

            try
            {
                MembershipUser mu = Membership.GetUser();

                // if authenticated by asp.net (meaning, logged in through website), use membership 
                // user info.  otherwise get authentication information from message
                if (mu != null)
                {
                    user = new User() { Name = mu.UserName };
                }
                else
                {
                    user = ResourceHelper.GetUserPassFromMessage(req);
                    HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
                    if (code != HttpStatusCode.OK)
                        return new HttpResponseMessageWrapper<User>(req, code);  // user not authenticated
                }
            }
            catch (Exception)
            {
                // membership database is unreachable
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
            }

            ZaplifyStore zaplifystore = ZaplifyStore;

            try
            {
                // get the user and all of their top-level objects
                User dbUser = zaplifystore.Users.
                    Include("ItemTypes.Fields").
                    Include("Tags").
                    Include("ItemLists.Items.ItemTags").
                    Single<User>(u => u.Name == user.Name);
                
                // make sure the response isn't cached
                var response = new HttpResponseMessageWrapper<User>(req, dbUser, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                return response;
            }
            catch (Exception)
            {
                // couldn't find user - return 404 Not Found
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.NotFound);               
            }
        }

        /// <summary>
        /// Get a user by id
        /// </summary>
        /// <param name="id">UserID of the requested user</param>
        /// <returns>The User info (password elided if not authenticated user)</returns>
        [WebGet(UriTemplate = "{id}")]
        [LogMessages]
        public HttpResponseMessageWrapper<User> GetUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<User>(req, code);  // user not authenticated

            ZaplifyStore zaplifystore = ZaplifyStore;
            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            try
            {
                User requestedUser = zaplifystore.Users.Single<User>(u => u.ID == id);

                // if the requested user is not the same as the authenticated user, blank out password field
                if (requestedUser.ID != dbUser.ID)
                    requestedUser.Password = null;
                return new HttpResponseMessageWrapper<User>(req, requestedUser, code);
            }
            catch (Exception)
            {
                // user not found - return 404 Not Found
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Get all itemlists for a user
        /// </summary>
        /// <param name="id">ID for the user</param>
        /// <returns>List of itemlists for the user</returns>
        [WebGet(UriTemplate = "{id}/itemlists")]
        [LogMessages]
        public HttpResponseMessageWrapper<List<ItemList>> GetItemListsForUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<List<ItemList>>(req, code);  // user not authenticated

            ZaplifyStore zaplifystore = ZaplifyStore;
            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            try
            {
                User requestedUser = zaplifystore.Users.Single<User>(u => u.ID == id);

                // if the requested user is not the same as the authenticated user, return 403 Forbidden
                if (requestedUser.ID != dbUser.ID)
                    return new HttpResponseMessageWrapper<List<ItemList>>(req, HttpStatusCode.Forbidden);
                else
                {
                    try
                    {
                        var itemlists = zaplifystore.ItemLists.Include("User").Include("Items").Where(tl => tl.UserID == id).ToList();
                        var response = new HttpResponseMessageWrapper<List<ItemList>>(req, itemlists, HttpStatusCode.OK);
                        response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                        return response;
                    }
                    catch (Exception)
                    {
                        // itemlists not found - return 404 Not Found
                        return new HttpResponseMessageWrapper<List<ItemList>>(req, HttpStatusCode.NotFound);
                    }
                }
            }
            catch (Exception)
            {
                // user not found - return 404 Not Found
                return new HttpResponseMessageWrapper<List<ItemList>>(req, HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// Update a user 
        /// </summary>
        /// <param name="id">id for the user to delete</param>
        /// <returns></returns>
        [WebInvoke(UriTemplate = "{id}", Method = "PUT")]
        public HttpResponseMessageWrapper<User> UpdateUser(HttpRequestMessage req, Guid id)
        {
            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<User>(req, code);  // user not authenticated

            // the body will be two Users - the original and the new values.  Verify this
            List<User> clientUsers = ResourceHelper.ProcessRequestBody(req, ZaplifyStore, typeof(List<User>)) as List<User>;
            if (clientUsers.Count != 2)
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);

            // get the original and new items out of the message body
            User originalUser = clientUsers[0];
            User newUser = clientUsers[1];

            // make sure the item ID's match
            if (originalUser.ID != newUser.ID)
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);
            if (originalUser.ID != id)
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.BadRequest);

            ZaplifyStore zaplifystore = ZaplifyStore;

            User user = ResourceHelper.GetUserPassFromMessage(req);
            User dbUser = zaplifystore.Users.Single<User>(u => u.Name == user.Name && u.Password == user.Password);

            // check to make sure the old password in the message matches what's in the database
            if (originalUser.Password != dbUser.Password)
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.Forbidden);
            
            try
            {
                // get the membership user
                MembershipUser mu = Membership.GetUser(originalUser.Name);
                
                // change the password
                string usrpwd = mu.ResetPassword();
                mu.ChangePassword(usrpwd, newUser.Password);
                
                // change the e-mail
                mu.Email = newUser.Email;

                // update the membership provider
                Membership.UpdateUser(mu);

                // update the user data in the ZaplifyStore database
                dbUser.Password = newUser.Password;
                dbUser.Email = newUser.Email;
                int rows = zaplifystore.SaveChanges();
                if (rows < 1)
                    return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
                else
                    return new HttpResponseMessageWrapper<User>(req, dbUser, HttpStatusCode.Accepted);
            }
            catch (Exception)
            {
                return new HttpResponseMessageWrapper<User>(req, HttpStatusCode.InternalServerError);
            }
        }
    }
}