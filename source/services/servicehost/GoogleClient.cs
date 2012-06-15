using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using BuiltSteady.Zaplify.ServerEntities;

using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class GoogleClient
    {
        User user;
        UserStorageContext storage;
        OAuth2Authenticator<WebServerClient> googleAuthenticator;
        CalendarService calService;
        Settings calSettings;

        public GoogleClient()
        {   // for getting initial tokens
            googleAuthenticator = CreateGoogleAuthenticator(GetGoogleTokens);
        }

        public GoogleClient(User user, UserStorageContext storage)
        {   // for using existing access token with renewal
            this.user = user;
            this.storage = storage;
            if (user.UserCredentials == null || user.UserCredentials.Count == 0)
            {   // ensure UserCredentials are present
                this.user = storage.GetUser(user.ID, true);
            }
            googleAuthenticator = CreateGoogleAuthenticator(GetAccessToken);
        }

        public OAuth2Authenticator<WebServerClient> Authenticator
        {
            get { return this.googleAuthenticator; }
        }

        public CalendarService CalendarService
        {
            get 
            {
                if (calService == null)
                {
                    calService = new CalendarService(googleAuthenticator);
                }
                return calService;
            }
        }

        public Settings CalendarSettings
        {
            get
            {
                if (calSettings == null)
                {
                    SettingsResource.ListRequest settingsList = this.CalendarService.Settings.List();
                    calSettings = settingsList.Fetch();
                }
                return calSettings;
            }
        }

        public void ForceAuthentication()
        {
            var settings = CalendarSettings;
        }

        IAuthorizationState GetAccessToken(WebServerClient client)
        {
            IAuthorizationState state = new AuthorizationState(GoogleClient.Scopes);
            UserCredential googleConsent = user.GetCredential(UserCredential.GoogleConsent);
            if (googleConsent != null)
            {
                TimeSpan difference = googleConsent.AccessTokenExpiration.Value - DateTime.UtcNow;
                if (difference.TotalMinutes < 5)
                {   // token is expired or will expire within 5 minutes, refresh token
                    googleConsent = RenewAccessToken(googleConsent);
                }
                state.AccessToken = googleConsent.AccessToken;
            }
            else
            {
                TraceLog.TraceError("Google access token is not available");
            }
            return state;
        }

        struct JsonGoogleToken
        {
            public string token_type;
            public string access_token;
            public int expires_in;
        }

        UserCredential RenewAccessToken(UserCredential googleConsent)
        {
            string format = "client_id={0}&client_secret={1}&refresh_token={2}&grant_type=refresh_token";
            string formParams = string.Format(format,
                    System.Web.HttpContext.Current.Server.UrlEncode(GoogleClient.ID),
                    System.Web.HttpContext.Current.Server.UrlEncode(GoogleClient.Secret),
                    System.Web.HttpContext.Current.Server.UrlEncode(googleConsent.RenewalToken));

            byte[] byteArray = Encoding.ASCII.GetBytes(formParams);
            const string googleOAuth2TokenServiceUrl = "https://accounts.google.com/o/oauth2/token";
            WebRequest request = WebRequest.Create(googleOAuth2TokenServiceUrl);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            Stream outStream = request.GetRequestStream();
            outStream.Write(byteArray, 0, byteArray.Length);
            outStream.Close();
            try
            {
                WebResponse response = request.GetResponse();
                HttpStatusCode responseStatus = ((HttpWebResponse)response).StatusCode;
                Stream inStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(inStream);
                string jsonToken = reader.ReadToEnd();
                JsonGoogleToken token = JsonSerializer.Deserialize<JsonGoogleToken>(jsonToken);

                googleConsent.AccessToken = token.access_token;
                googleConsent.AccessTokenExpiration = DateTime.UtcNow.AddSeconds(token.expires_in);
                storage.SaveChanges();

                reader.Close();
                inStream.Close();
                response.Close();
            }
            catch (Exception e)
            {
                TraceLog.TraceException("Could not refresh Google access token", e);
            }
            return googleConsent;
        }

        // for getting initial access and renewal tokens via OAuth handshake
        IAuthorizationState GetGoogleTokens(WebServerClient client)
        {
            // check if authorization request already is in progress
            IAuthorizationState state = client.ProcessUserAuthorization(new HttpRequestInfo(System.Web.HttpContext.Current.Request));
            if (state != null && (!string.IsNullOrEmpty(state.AccessToken) || !string.IsNullOrEmpty(state.RefreshToken)))
            {   // store refresh token  
                string username = System.Web.HttpContext.Current.User.Identity.Name;
                UserStorageContext storage = Storage.NewUserContext;
                User user = storage.Users.Include("UserCredentials").Single<User>(u => u.Name == username);
                user.AddCredential(UserCredential.GoogleConsent, state.AccessToken, state.AccessTokenExpirationUtc, state.RefreshToken);
                storage.SaveChanges();
                return state;
            }

            // otherwise make a new authorization request
            OutgoingWebResponse response = client.PrepareRequestUserAuthorization(GoogleClient.Scopes);
            response.Headers["Location"] += "&access_type=offline&approval_prompt=force";
            response.Send();    // will throw a ThreadAbortException to prevent sending another response
            return null;
        }


        static OAuth2Authenticator<WebServerClient> CreateGoogleAuthenticator(Func<WebServerClient, IAuthorizationState> authProvider)
        {   // create the authenticator
            var provider = new WebServerClient(GoogleAuthenticationServer.Description);
            provider.ClientIdentifier = GoogleClient.ID;
            provider.ClientSecret = GoogleClient.Secret;
            var authenticator = new OAuth2Authenticator<WebServerClient>(provider, authProvider) { NoCaching = true };
            return authenticator;
        }

        static string[] Scopes
        {
            get
            {
                string calendarScope = "https://www.googleapis.com/auth/calendar";
                return new[] { calendarScope };
            }
        }

        static string googleClientID;
        static string ID
        {
            get
            {
                if (googleClientID == null) { googleClientID = ConfigurationSettings.Get("GoogleClientID"); }
                return googleClientID;
            }
        }

        static string googleClientSecret;
        public static string Secret
        {
            get
            {
                if (googleClientSecret == null) { googleClientSecret = ConfigurationSettings.Get("GoogleClientSecret"); }
                return googleClientSecret;
            }
        }
    }
}
