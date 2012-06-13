namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.IdentityModel.Protocols.OAuth.Client;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.Website.Models.AccessControl;

    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.OAuth2;
    using Google.Apis.Authentication.OAuth2;
    using Google.Apis.Calendar.v3;
    using Google.Apis.Calendar.v3.Data;

    public class OAuthConsentController : BaseController
    {

        public ActionResult Facebook(string code)
        {
            const string fbRedirectPath = "oauthconsent/facebook";
            string uriTemplate = "https://graph.facebook.com/oauth/access_token?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}";

            var requestUrl = this.HttpContext.Request.Url;
            var redirectUrl = string.Format("{0}://{1}/{2}", requestUrl.Scheme, requestUrl.Authority, fbRedirectPath);
            string encodedRedirect = HttpUtility.UrlEncode(redirectUrl);
            string uri = string.Format(uriTemplate, FBAppID, encodedRedirect, FBAppSecret, code);

            WebRequest req = WebRequest.Create(uri);
            WebResponse resp = req.GetResponse();

            string token = null;
            DateTime? expires = null;

            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string data = reader.ReadToEnd();

                string[] parts = data.Split('&');
                foreach (var s in parts)
                {
                    string[] kv = s.Split('=');
                    if (kv[0].Equals("access_token", StringComparison.Ordinal))
                        token = kv[1];
                    else if (kv[0].Equals("expires"))
                        expires = DateTime.UtcNow.AddSeconds(int.Parse(kv[1]));
                }
            }

            var renewed = false;
            try
            {   // store token
                renewed = UserMembershipProvider.SaveCredential(this.CurrentUser.Name, UserCredential.FB_CONSENT, token, expires);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Failed to store Facebook consent token for User", ex);
                return RedirectToAction("Home", "Dashboard", new { consentStatus = UserDataModel.FBConsentFail });
            }
            if (renewed) 
            { return RedirectToAction("Home", "Dashboard"); }
            else 
            { return RedirectToAction("Home", "Dashboard", new { consentStatus = UserDataModel.FBConsentSuccess }); }
        }

        static OAuth2Authenticator<WebServerClient> googleAuthenticator;
        static CalendarService calService;
        public ActionResult Google()
        {
            if (calService == null)
            {   // attempt to access Google Calendar API to force authentication
               calService = new CalendarService(googleAuthenticator = CreateGoogleAuthenticator());
            }
            
            if (HttpContext.Request["code"] != null)
            {   // load access tokens 
                googleAuthenticator.LoadAccessToken();
            }

            try
            {   // try to access user calendar settings
                SettingsResource.ListRequest calSettings = calService.Settings.List();
                Settings settings = calSettings.Fetch();
                return RedirectToAction("Home", "Dashboard", new { consentStatus = UserDataModel.GoogleConsentSuccess });
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception)
            {
                return RedirectToAction("Home", "Dashboard", new { consentStatus = UserDataModel.GoogleConsentFail });
            }
        }

        OAuth2Authenticator<WebServerClient> CreateGoogleAuthenticator()
        {   // create the authenticator.
            var provider = new WebServerClient(GoogleAuthenticationServer.Description);
            provider.ClientIdentifier = BaseController.GoogleClientID;
            provider.ClientSecret = BaseController.GoogleClientSecret;
            var authenticator = new OAuth2Authenticator<WebServerClient>(provider, GetGoogleAuthorization) { NoCaching = true };
            return authenticator;
        }

        private IAuthorizationState GetGoogleAuthorization(WebServerClient client)
        {
            // check if authorization request already is in progress
            IAuthorizationState state = client.ProcessUserAuthorization(new HttpRequestInfo(System.Web.HttpContext.Current.Request));
            if (state != null && (!string.IsNullOrEmpty(state.AccessToken) || !string.IsNullOrEmpty(state.RefreshToken)))
            {   // store refresh token  
                string username = System.Web.HttpContext.Current.User.Identity.Name;
                UserMembershipProvider.SaveCredential(username, UserCredential.GOOGLE_CONSENT, state.AccessToken, state.AccessTokenExpirationUtc, state.RefreshToken);
                return state;
            }

            // otherwise make a new authorization request
            string scope = "https://www.googleapis.com/auth/calendar";
            OutgoingWebResponse response = client.PrepareRequestUserAuthorization(new[] { scope });
            response.Headers["Location"] += "&access_type=offline&approval_prompt=force"; 
            response.Send();    // will throw a ThreadAbortException to prevent sending another response
            return null;
        }

        public ActionResult CloudAD()
        {
            OAuthClient.RedirectToEndUserEndpoint(
                AzureOAuthConfiguration.ProtectedResourceUrl,
                AuthorizationResponseType.Code,
                new Uri(AzureOAuthConfiguration.GetRedirectUrlAfterEndUserConsent(this.HttpContext.Request.Url)),
                CurrentUser.ID.ToString(),
                null);

            return new EmptyResult();
        }

    }
}
