using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

using BuiltSteady.Zaplify.ServerEntities;

using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class GoogleClient
    {
        const string ExtensionItemID = "ZapItemID";
        //const string GadgetIconPath = "/content/images/zaplogo.png";

        User user;
        UserStorageContext storage;
        OAuth2Authenticator<WebServerClient> googleAuthenticator;
        CalendarService calService;
        Settings calSettings;
        string userCalendar;

        public GoogleClient()
        {   // for getting initial tokens
            this.googleAuthenticator = CreateGoogleAuthenticator(GetGoogleTokens);
        }

        public GoogleClient(User user, UserStorageContext storage)
        {   // for using existing access token with renewal
            this.user = user;
            this.storage = storage;
            if (user.UserCredentials == null || user.UserCredentials.Count == 0)
            {   // ensure UserCredentials are present
                this.user = storage.GetUser(user.ID, true);
            }
            UserCredential googleConsent = this.user.GetCredential(UserCredential.GoogleConsent);
            if (googleConsent != null)
            {
                this.googleAuthenticator = CreateGoogleAuthenticator(GetAccessToken);
            }
        }

        public OAuth2Authenticator<WebServerClient> Authenticator
        {
            get { return googleAuthenticator; }
        }

        public bool ConnectToCalendar
        {   // TODO: add setting that allows user to opt-in or out
            get { return (Authenticator != null); }
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

        public IList<CalendarListEntry> UserCalendars
        {
            get
            {
                CalendarListResource.ListRequest calListReq = this.CalendarService.CalendarList.List();
                CalendarList calList = calListReq.Fetch();
                return calList.Items;
            }
        }

        // get the ID for the user calendar to manage with Zaplify
        // TODO: allow user to choose this in ClientSettings
        public string UserCalendar
        {
            get
            {
                if (userCalendar == null)
                {   // TODO: temporarily attach as ExtendedFieldName to UserProfile list
                    // Need to resolve how UserProfile information is stored (ItemType, Expando, or Json)
                    Item userProfile = storage.ClientFolder.GetUserProfile(user);
                    FieldValue fvCalendarID = userProfile.GetFieldValue(ExtendedFieldNames.CalendarID, true);
                    if (fvCalendarID == null || string.IsNullOrEmpty(fvCalendarID.Value))
                    {   // find best candidate in list of user calendars
                        var userCalendars = UserCalendars;
                        foreach (var cal in userCalendars)
                        {
                            if (cal.AccessRole == "owner")
                            {   // must have owner access
                                if (user.Email.Equals(cal.Id, StringComparison.OrdinalIgnoreCase))
                                {   // user.email matches calendar id (this is best match)
                                    userCalendar = cal.Id;
                                    break;
                                }
                                if (user.Email.Equals(cal.Summary, StringComparison.OrdinalIgnoreCase))
                                {   // user.email matches calendar summary (this is next best match)
                                    userCalendar = cal.Id;
                                }
                                if (userCalendar == null)
                                {   // use first owner calendar if user.email cannot be matched
                                    userCalendar = cal.Id;
                                }
                            }
                        }
                        // save CalendarID
                        fvCalendarID.Value = userCalendar;
                        storage.SaveChanges();
                    }
                    userCalendar = fvCalendarID.Value;
                }
                return userCalendar;
            }
        }

        public Settings CalendarSettings
        {
            get
            {
                if (calSettings == null)
                {
                    SettingsResource.ListRequest calSettingReq = this.CalendarService.Settings.List();
                    calSettings = calSettingReq.Fetch();
                }
                return calSettings;
            }
        }

        public List<Event> GetCalendarEvents(bool onlyZapEvents = true, DateTime? utcStartTime = null, DateTime? utcEndTime = null)
        {   // by default, filters events to past month and next 3 months
            if (utcStartTime == null) { utcStartTime = DateTime.UtcNow.AddDays(-30); }
            if (utcEndTime == null) { utcEndTime = DateTime.UtcNow.AddDays(90); }

            EventsResource.ListRequest eventListReq = this.CalendarService.Events.List(UserCalendar);
            eventListReq.TimeMin = XmlConvert.ToString(utcStartTime.Value, XmlDateTimeSerializationMode.Utc);
            eventListReq.TimeMax = XmlConvert.ToString(utcEndTime.Value, XmlDateTimeSerializationMode.Utc);
            Events events = eventListReq.Fetch();

            if (onlyZapEvents)
            {
                List<Event> zapEvents = events.Items.Where(e =>
                    e.ExtendedProperties != null && e.ExtendedProperties.Private != null &&
                    e.ExtendedProperties.Private.ContainsKey(ExtensionItemID)).ToList();
                return zapEvents;
            }

            return new List<Event>(events.Items);
        }

        public Event GetCalendarEvent(string id)
        {
            Event calEvent = null;
            try
            {
                EventsResource.GetRequest eventGetReq = this.CalendarService.Events.Get(UserCalendar, id);
                calEvent = eventGetReq.Fetch();
            }
            catch (Exception e)
            {
                TraceLog.TraceException(string.Format("Could not get Calendar event with id: '{0}'", id), e);
            }
            return calEvent;
        }

        public int SynchronizeCalendar()
        {
            int itemsUpdated = 0;
            if (ConnectToCalendar)
            {
                Item userProfile = storage.ClientFolder.GetUserProfile(user);
                Item metaItem = storage.UserFolder.GetEntityRef(user, userProfile);
                FieldValue fvCalLastSync = metaItem.GetFieldValue(ExtendedFieldNames.CalLastSync, true);
                DateTime lastSyncTime;
                if (fvCalLastSync.Value == null)
                {   // first-time, use consent token last modified date
                    UserCredential googleConsent = user.GetCredential(UserCredential.GoogleConsent);
                    lastSyncTime = googleConsent.LastModified;
                }
                else
                {
                    lastSyncTime = DateTime.Parse(fvCalLastSync.Value);
                }
                fvCalLastSync.Value = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);
                itemsUpdated = SynchronizeCalendar(lastSyncTime);
            }
            return itemsUpdated;
        }

        int SynchronizeCalendar(DateTime utcModifiedAfter)
        {
            int itemsUpdated = 0;
            try
            {
                var modifiedEvents = GetModifiedCalendarEvents(utcModifiedAfter);
                foreach (Event e in modifiedEvents)
                {
                    Guid itemID = new Guid(e.ExtendedProperties.Private[ExtensionItemID]);
                    Item item = storage.GetItem(user, itemID);
                    if (item != null)
                    {   // Name, DueDate, EndDate, and Description (support Location?)
                        item.Name = e.Summary;
                        item.GetFieldValue(FieldNames.DueDate).Value = e.Start.DateTime;
                        item.GetFieldValue(FieldNames.EndDate).Value = e.End.DateTime;
                        FieldValue fvDescription = item.GetFieldValue(FieldNames.Description, (e.Description != null));
                        if (fvDescription != null) { fvDescription.Value = e.Description; }
                        itemsUpdated++;
                    }
                }
                if (itemsUpdated > 0) { storage.SaveChanges(); }
            }
            catch (Exception e)
            {
                TraceLog.TraceException("Could not get modified Calendar events", e);
            }
            return itemsUpdated;
        }

        List<Event> GetModifiedCalendarEvents(DateTime utcModifiedAfter)
        {
            EventsResource.ListRequest eventListReq = this.CalendarService.Events.List(UserCalendar);
            eventListReq.UpdatedMin = XmlConvert.ToString(utcModifiedAfter, XmlDateTimeSerializationMode.Utc);
            Events events = eventListReq.Fetch();
            if (events.Items != null)
            {
                return events.Items.Where(e =>
                e.ExtendedProperties != null && e.ExtendedProperties.Private != null &&
                e.ExtendedProperties.Private.ContainsKey(ExtensionItemID)).ToList();
            }
            return new List<Event>();
        }

        public bool AddCalendarEvent(Item item)
        {
            DateTime utcStartTime, utcEndTime;
            FieldValue fvStartTime = item.GetFieldValue(FieldNames.DueDate);
            FieldValue fvEndTime = item.GetFieldValue(FieldNames.EndDate);
            if (fvStartTime != null && !string.IsNullOrEmpty(fvStartTime.Value) && 
                fvEndTime != null && !string.IsNullOrEmpty(fvEndTime.Value) && 
                DateTime.TryParse(fvStartTime.Value, out utcStartTime) &&
                DateTime.TryParse(fvEndTime.Value, out utcEndTime))
            {
                Event calEvent = new Event()
                {
                    Summary = item.Name,
                    Start = new EventDateTime() { DateTime = XmlConvert.ToString(utcStartTime, XmlDateTimeSerializationMode.Utc) },
                    End = new EventDateTime() { DateTime = XmlConvert.ToString(utcEndTime, XmlDateTimeSerializationMode.Utc) },
                    ExtendedProperties = new Event.ExtendedPropertiesData(),
                };
                // add item id as private extended property for event
                calEvent.ExtendedProperties.Private = new Event.ExtendedPropertiesData.PrivateData();
                calEvent.ExtendedProperties.Private.Add(ExtensionItemID, item.ID.ToString());

                FieldValue fvDescription = item.GetFieldValue(FieldNames.Description);
                if (fvDescription != null && !string.IsNullOrEmpty(fvDescription.Value))
                {  
                    calEvent.Description = fvDescription.Value;
                }                

                // TODO: investigate using Gadget to support link back to Zaplify

                try
                {
                    EventsResource.InsertRequest eventInsertReq = this.CalendarService.Events.Insert(calEvent, UserCalendar);
                    Event result = eventInsertReq.Fetch();

                    if (result.HtmlLink != null)
                    {   // add event HtmlLink as a WebLink in item
                        FieldValue fvWebLinks = item.GetFieldValue(FieldNames.WebLinks, true);
                        JsonWebLink webLink = new JsonWebLink() { Name = "Calendar Event", Url = result.HtmlLink };
                        List<JsonWebLink> webLinks = (string.IsNullOrEmpty(fvWebLinks.Value)) ?
                            new List<JsonWebLink>() : JsonSerializer.Deserialize<List<JsonWebLink>>(fvWebLinks.Value);
                        //var webLink = new { Name = "Calendar Event", Url = result.HtmlLink };
                        //var webLinks = (string.IsNullOrEmpty(fvWebLinks.Value)) ?
                        //    new List<object>() : JsonSerializer.Deserialize<List<object>>(fvWebLinks.Value);
                        webLinks.Add(webLink);
                        fvWebLinks.Value = JsonSerializer.Serialize(webLinks);
                    }

                    // add event id to UserFolder EntityRefs for item
                    Item metaItem = storage.UserFolder.GetEntityRef(user, item);
                    FieldValue fvCalEventID = metaItem.GetFieldValue(ExtendedFieldNames.CalEventID, true);
                    fvCalEventID.Value = result.Id;
                    storage.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    TraceLog.TraceException("Could not add appointment to Calendar", e);
                }
            }
            return false;
        }

        public bool UpdateCalendarEvent(Item item)
        {   // assumes check for changes was made before
            FieldValue fvStartTime = item.GetFieldValue(FieldNames.DueDate);
            FieldValue fvEndTime = item.GetFieldValue(FieldNames.EndDate);
            if (fvStartTime != null && !string.IsNullOrEmpty(fvStartTime.Value) &&
                fvEndTime != null && !string.IsNullOrEmpty(fvEndTime.Value))
            {
                DateTime utcStartTime, utcEndTime;
                if (DateTime.TryParse(fvStartTime.Value, out utcStartTime) && DateTime.TryParse(fvEndTime.Value, out utcEndTime))
                {
                    Item metaItem = storage.UserFolder.GetEntityRef(user, item);
                    FieldValue fvCalEventID = metaItem.GetFieldValue(ExtendedFieldNames.CalEventID, true);
                    if (string.IsNullOrEmpty(fvCalEventID.Value))
                    {   // add CalendarEvent for this Item
                        return AddCalendarEvent(item);
                    }
                    else
                    {
                        Event calEvent = GetCalendarEvent(fvCalEventID.Value);
                        if (calEvent == null)
                        {   // may have been deleted, add another CalendarEvent for modified Item
                            return AddCalendarEvent(item);
                        }
                        else
                        {   // update existing CalendarEvent
                            TimeSpan duration = utcEndTime - utcStartTime;
                            if (duration.TotalMinutes >= 0)
                            {   // ensure startTime is BEFORE endTime
                                calEvent.Summary = item.Name;
                                calEvent.Start.DateTime = XmlConvert.ToString(utcStartTime, XmlDateTimeSerializationMode.Utc);
                                calEvent.End.DateTime = XmlConvert.ToString(utcEndTime, XmlDateTimeSerializationMode.Utc);
                                FieldValue fvDescription = item.GetFieldValue(FieldNames.Description);
                                if (fvDescription != null) { calEvent.Description = fvDescription.Value; }
     
                                if (calEvent.Status == "cancelled") { calEvent.Status = "confirmed"; }
                                try
                                {
                                    EventsResource.PatchRequest eventPatchReq = this.CalendarService.Events.Patch(calEvent, UserCalendar, calEvent.Id);
                                    Event updatedCalEvent = eventPatchReq.Fetch();
                                    return true;
                                }
                                catch (Exception e)
                                {
                                    TraceLog.TraceException("Could not update appointment to Calendar", e);
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public bool RemoveCalendarEvent(Item item)
        {   // remove CalendarEvent
            Item metaItem = storage.UserFolder.GetEntityRef(user, item);
            FieldValue fvCalEventID = metaItem.GetFieldValue(ExtendedFieldNames.CalEventID, true);
            if (!string.IsNullOrEmpty(fvCalEventID.Value))
            {   // CalendarEvent has been added for this Item
                try
                {                
                    Event calEvent = GetCalendarEvent(fvCalEventID.Value);
                    if (calEvent != null)
                    {   // remove existing CalendarEvent
                        EventsResource.DeleteRequest eventDeleteReq = this.CalendarService.Events.Delete(UserCalendar, calEvent.Id);
                        string result = eventDeleteReq.Fetch();
                        // EntityRef holding association will get cleaned up when Item is deleted
                        return (result == string.Empty);
                    }
                }
                catch (Exception e)
                {
                    TraceLog.TraceException("Could not remove appointment from Calendar", e);
                }
            }
            return false;
        }

        public void ForceAuthentication()
        {   // attempt to access Calendar settings to force authentication
            SettingsResource.ListRequest calSettingReq = this.CalendarService.Settings.List();
            calSettingReq.Fetch();
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
