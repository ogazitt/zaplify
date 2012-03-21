using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class AzureOAuthConfiguration
    {
        // ACS service Namespace
        public const string ServiceNamespace = "graph-sts-prod";

        // Identifier for protected resource - this is quite arbitrary, but convention holds it to be the protected resource's address
        public const string ProtectedResourceUrl = "https://graph.windows.net/PeopleGraph";

        // Authorization Server Endpoint.
        public const string EndUserEndPoint = "http://persongraph.cloudapp.net/Authorization.aspx";

        // Client Configuration Information.
        public const string ClientIdentity = "zaplify.app.local";
        public const string ClientSecret = "P0rsche911";

        // Relying Party Configuration.
        public const string RelyingPartyRealm = "http://applications.graph.windows.net/";

        // The Uri the client is redirected to after user authentication. (OAuthHandler.ashx is supplied by Microsoft.IdentityModel.Protocols.OAuth.Client.dll)
        public const string RedirectUrlAfterEndUserConsent = "http://localhost:8888/OAuthHandler.ashx";

        public static string GetTokenUri()
        {
            return String.Format("https://{0}.accesscontrol.windows.net/v2/OAuth2-13", AzureOAuthConfiguration.ServiceNamespace);        
        }
    }
}
