﻿using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Microsoft.ApplicationServer.Http;
using Microsoft.IdentityModel.Protocols.OAuth;
using Microsoft.IdentityModel.Protocols.OAuth.Client;

using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Website.Models;
using BuiltSteady.Zaplify.Website.Models.AccessControl;
using BuiltSteady.Zaplify.Website.Resources;

namespace BuiltSteady.Zaplify.Website
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // map the MVC UI route (note the route constraint as the last controller passed in)
            // http://codebetter.com/howarddierking/2011/05/09/using-serviceroute-with-existing-mvc-routes/
            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Dashboard", action = "Home", id = UrlParameter.Optional }, // Parameter defaults
                new { controller = new NotInValuesConstraint(new[] { "constants", "folders", "items", "itemtypes", "operations", "speech", "suggestions", "tags", "trace", "users", "OAuthHandler.ashx" }) }
            );

            // map the WCF WebApi service routes
            HttpConfiguration config = new HttpConfiguration() { MaxBufferSize = 1024 * 1024, MaxReceivedMessageSize = 1024 * 1024 };
            RouteTable.Routes.MapServiceRoute<ConstantsResource>("constants", null);
            RouteTable.Routes.MapServiceRoute<FolderResource>("folders", null);
            RouteTable.Routes.MapServiceRoute<ItemResource>("items", config);
            RouteTable.Routes.MapServiceRoute<ItemTypeResource>("itemtypes", null);
            RouteTable.Routes.MapServiceRoute<OperationResource>("operations", null);
            RouteTable.Routes.MapServiceRoute<SpeechResource>("speech",
                new HttpConfiguration
                {
                    MaxReceivedMessageSize = 1048576, // 1MB == 32seconds of speech
                    MaxBufferSize = 1048576, // 1MB == 32seconds of speech
                });
            RouteTable.Routes.MapServiceRoute<SuggestionResource>("suggestions", null);
            RouteTable.Routes.MapServiceRoute<TagResource>("tags", null);
            RouteTable.Routes.MapServiceRoute<TraceResource>("trace", null);
            RouteTable.Routes.MapServiceRoute<UserResource>("users", config);
        }

        public class NotInValuesConstraint : IRouteConstraint
        {
            public NotInValuesConstraint(params string[] values) { _values = values; }
            private string[] _values;
            public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
            {
                string value = values[parameterName].ToString();
                return !_values.Contains(value, StringComparer.CurrentCultureIgnoreCase);
            }
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            RegisterOAuthHandler();
        }

        private void RegisterOAuthHandler()
        {
            Uri tokenUri = new Uri(AzureOAuthConfiguration.GetTokenUri());

            // set up the ServerRegistry
            InMemoryAuthorizationServerRegistry serverRegistry = new InMemoryAuthorizationServerRegistry();
            AuthorizationServerRegistration registrationInfo = new AuthorizationServerRegistration(
                tokenUri,
                new Uri(AzureOAuthConfiguration.EndUserEndPoint),
                AzureOAuthConfiguration.GetClientIdentity(),
                AzureOAuthConfiguration.ClientSecret);

            serverRegistry.AddOrUpdate(registrationInfo);
            OAuthClientSettings.AuthorizationServerRegistry = serverRegistry;

            // set up the ResourceRegistry
            InMemoryResourceScopeMappingRegistry resourceRegistry = new InMemoryResourceScopeMappingRegistry();

            resourceRegistry.AddOrUpdate(AzureOAuthConfiguration.ProtectedResourceUrl,
                tokenUri,
                new Uri(AzureOAuthConfiguration.EndUserEndPoint),
                null);
            OAuthClientSettings.ResourceScopeMappingRegistry = resourceRegistry;

            // Handle the requesting access token event
            OAuthClientSettings.RequestingAccessToken += new EventHandler<RequestingAccessTokenEventArgs>(OAuthClientSettings_RequestingAccessToken);

            // Handle the token received event
            OAuthClientSettings.AccessTokenReceived += new EventHandler<AccessTokenReceivedEventArgs>(OAuthClientSettings_AccessTokenReceived);

            // Handle the event when the user denies consent.
            OAuthClientSettings.EndUserAuthorizationFailed += new EventHandler<EndUserAuthorizationFailedEventArgs>(OAuthClientSettings_EndUserAuthorizationFailed);

            OAuthClientSettings.AuthorizationCodeReceived += new EventHandler<AuthorizationCodeReceivedEventArgs>(OAuthClientSettings_AuthorizationCodeReceived);

            //register the Authentication Module
            AuthenticationManager.Register(new OAuthAuthenticationModule());
        }

        void OAuthClientSettings_AuthorizationCodeReceived(object sender, AuthorizationCodeReceivedEventArgs e)
        {
        }

        /// <summary>
        /// Event handler for the enduser authorization failed event. This method should take any corrective measure needed.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="authorizationFailedEventArgs">Event arguments.</param>
        void OAuthClientSettings_EndUserAuthorizationFailed(object sender, EndUserAuthorizationFailedEventArgs authorizationFailedEventArgs)
        {
            // cancelling this event will cause the user to be redirected to the initial page.
            authorizationFailedEventArgs.Cancel = true;
        }

        /// <summary>
        /// Event handler for the requesting access token event. This method can modify the outgoing 
        /// token request before it is sent.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="requestingTokenEventArgs">Event arguments.</param>
        void OAuthClientSettings_RequestingAccessToken(object sender, RequestingAccessTokenEventArgs requestingTokenEventArgs)
        {
            requestingTokenEventArgs.AccessTokenRequest.Scope = AzureOAuthConfiguration.RelyingPartyRealm;
        }

        /// <summary>
        /// Event handler for the  access token received event. This method should save the tokens so that
        /// they can be used by the application in its requests.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="tokenReceivedEventArgs">Event arguments.</param>
        void OAuthClientSettings_AccessTokenReceived(object sender, AccessTokenReceivedEventArgs tokenReceivedEventArgs)
        {
            string accessToken = tokenReceivedEventArgs.AuthorizationResponse.Parameters[OAuthConstants.AccessToken];
            Uri tokenUri = tokenReceivedEventArgs.TokenUri;
            string refreshToken = tokenReceivedEventArgs.AuthorizationResponse.Parameters[OAuthConstants.RefreshToken];

            if (tokenReceivedEventArgs.Resource.StartsWith(AzureOAuthConfiguration.ProtectedResourceUrl))
            {
                User user = null;
                UserStorageContext userStorage = Storage.NewUserContext;
                try
                {   // store token
                    var userid = new Guid(tokenReceivedEventArgs.State);
                    user = userStorage.GetUser(userid, true);
                    user.AddCredential(UserCredential.CloudADConsent, accessToken, null, refreshToken);
                    userStorage.SaveChanges();
                }
                catch (Exception ex) 
                {
                    TraceLog.TraceException("Failed to store CloudAD consent token for User", ex);
                    tokenReceivedEventArgs.HttpContext.Response.Redirect("dashboard/home?consentStatus=" + UserDataModel.CloudADConsentFail, true);
                }
     
                // redirect back to the dashboard
                tokenReceivedEventArgs.HttpContext.Response.Redirect("dashboard/home?consentStatus=" + UserDataModel.CloudADConsentSuccess, true);
            }
        }
    }
}