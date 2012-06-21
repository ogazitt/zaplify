﻿using System;
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
        const string UTCTimeZone = "Europe/Zurich";
        const string ExtensionItemID = "ZapItemID";
        const string GadgetIconPath = "/content/images/zaplogo.png";

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
            this.googleAuthenticator = CreateGoogleAuthenticator(GetAccessToken);
        }

        public OAuth2Authenticator<WebServerClient> Authenticator
        {
            get { return googleAuthenticator; }
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
                {   // TODO: check and store value in ClientSettings

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

                    CalendarListResource.ListRequest calListReq = this.CalendarService.CalendarList.List();
                    CalendarList calList = calListReq.Fetch();

                    string calendarId = calList.Items.First().Id;
                    CalendarsResource.GetRequest calReq = this.CalendarService.Calendars.Get(calendarId);
                    Calendar calendar = calReq.Fetch();

                    EventsResource.ListRequest eventListReq = this.CalendarService.Events.List(calendarId);
                    Events events = eventListReq.Fetch();

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

        public List<Event> GetModifiedCalendarEvents(DateTime utcModifiedAfter)
        {
            EventsResource.ListRequest eventListReq = this.CalendarService.Events.List(UserCalendar);
            eventListReq.UpdatedMin = XmlConvert.ToString(utcModifiedAfter, XmlDateTimeSerializationMode.Utc);
            Events events = eventListReq.Fetch();

            List<Event> zapEvents = events.Items.Where(e =>
                e.ExtendedProperties != null && e.ExtendedProperties.Private != null &&
                e.ExtendedProperties.Private.ContainsKey(ExtensionItemID)).ToList();
            return zapEvents;
        }

        public Event GetCalendarEvent(string id)
        {
            EventsResource.GetRequest eventGetReq = this.CalendarService.Events.Get(UserCalendar, id);
            Event calEvent = eventGetReq.Fetch();
            return calEvent;
        }

        public int SyncModifiedCalendarEvents(DateTime utcModifiedAfter)
        {
            int itemsUpdated = 0;
            var modifiedEvents = GetModifiedCalendarEvents(utcModifiedAfter);
            foreach (Event e in modifiedEvents)
            {
                Guid itemID = new Guid(e.ExtendedProperties.Private[ExtensionItemID]);
                Item item = storage.GetItem(user, itemID);
                if (item != null)
                {
                    if (item.Name != e.Summary) { item.Name = e.Summary; }
                    // TODO: only update dates if different
                    item.GetFieldValue(FieldNames.DueDate).Value = e.Start.DateTime;
                    item.GetFieldValue(FieldNames.EndDate).Value = e.End.DateTime;
                    itemsUpdated++;
                }
            }
            if (itemsUpdated > 0) { storage.SaveChanges(); }
            return itemsUpdated;
        }

        public string AddCalendarEvent(Item item)
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
                    Start = new EventDateTime() { DateTime = XmlConvert.ToString(utcStartTime, XmlDateTimeSerializationMode.Utc), TimeZone = UTCTimeZone },
                    End = new EventDateTime() { DateTime = XmlConvert.ToString(utcEndTime, XmlDateTimeSerializationMode.Utc), TimeZone = UTCTimeZone },
                    ExtendedProperties = new Event.ExtendedPropertiesData(),
                    //Gadget = new Event.GadgetData()
                };
                // attach Item.ID as private extended property
                calEvent.ExtendedProperties.Private = new Event.ExtendedPropertiesData.PrivateData();
                calEvent.ExtendedProperties.Private.Add(ExtensionItemID, item.ID.ToString());
                
                // TODO: override default reminders for day or longer events
                //calEvent.Reminders.Overrides.Add(new EventReminder() { Minutes = reminderMinutes });

                // use gadget properties to link back to Zaplify
                //Uri requestUri = System.Web.HttpContext.Current.Request.Url;
                //calEvent.Gadget.Link = string.Format("{0}://{1}", requestUri.Scheme, requestUri.Authority);
                //calEvent.Gadget.IconLink = calEvent.Gadget.Link + GadgetIconPath;
                //calEvent.Gadget.Display = "icon";

                EventsResource.InsertRequest eventInsertReq = this.CalendarService.Events.Insert(calEvent, UserCalendar);
                Event result = eventInsertReq.Fetch();
                
                // associate event id with Item in UserFolder EntityRefs
                Item metaItem = storage.UserFolder.GetEntityRef(user, item);
                FieldValue fvCalEventID = metaItem.GetFieldValue(ExtendedFieldNames.CalEventID, true);
                fvCalEventID.Value = result.Id;
                storage.SaveChanges();

                return result.Id;
            }
            return null;
        }

#if false
        public bool UpdateCalendarEvent(Item newItem, Item oldItem)
        {   // only update if Name, DueDate, or EndDate has changed
            FieldValue fvNewStart = newItem.GetFieldValue(FieldNames.DueDate);
            FieldValue fvNewEnd = newItem.GetFieldValue(FieldNames.EndDate);
            FieldValue fvOldStart = oldItem.GetFieldValue(FieldNames.DueDate);
            FieldValue fvOldEnd = oldItem.GetFieldValue(FieldNames.EndDate);
            if (newItem.Name != oldItem.Name ||
                (fvNewStart != null && !string.IsNullOrEmpty(fvNewStart.Value) && fvOldStart != null && fvNewStart.Value != fvOldStart.Value) ||
                (fvNewEnd != null && !string.IsNullOrEmpty(fvNewEnd.Value) && fvOldEnd != null && fvNewEnd.Value != fvOldEnd.Value))
            {
#endif
        public string UpdateCalendarEvent(Item item)
        {   // assumes check for changes was made before
            string eventID = null;
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
                        eventID = AddCalendarEvent(item);
                    }
                    else
                    {
                        Event calEvent = GetCalendarEvent(fvCalEventID.Value);
                        if (calEvent == null)
                        {   // may have been deleted, add another CalendarEvent for modified Item
                            eventID = AddCalendarEvent(item);
                        }
                        else
                        {   // update existing CalendarEvent
                            calEvent.Summary = item.Name;
                            calEvent.Start.DateTime = XmlConvert.ToString(utcStartTime, XmlDateTimeSerializationMode.Utc);
                            calEvent.End.DateTime = XmlConvert.ToString(utcEndTime, XmlDateTimeSerializationMode.Utc);
                            EventsResource.PatchRequest eventPatchReq = this.CalendarService.Events.Patch(calEvent, UserCalendar, calEvent.Id);
                            Event updatedCalEvent = eventPatchReq.Fetch();
                            eventID = updatedCalEvent.Id;
                        }
                    }
                }
            }
            return eventID;
        }

        static DateTime? lastCheck;
        public void ForceAuthentication()
        {
            var settings = CalendarSettings;
#if false
            // TEST CODE
            if (lastCheck == null)
            {
                UserStorageContext exStorage = Storage.NewUserContext;
                Item item = exStorage.Items.Include("FieldValues").Single<Item>(i => i.UserID == user.ID && i.Name == "ZaplifyTest" && i.ParentID == null);
                Item metaItem = exStorage.UserFolder.GetEntityRef(user, item);
                FieldValue fvCalEventID = metaItem.GetFieldValue(ExtendedFieldNames.CalEventID);
                if (fvCalEventID == null)
                {
                    AddCalendarEvent(item);
                }
                else
                {
                    FieldValue fvEndDate = item.GetFieldValue(FieldNames.EndDate);
                    DateTime endTime = DateTime.Parse(fvEndDate.Value);
                    endTime = endTime.AddHours(1);
                    fvEndDate.Value = XmlConvert.ToString(endTime, XmlDateTimeSerializationMode.Utc);
                    UpdateCalendarEvent(item);
                    exStorage.SaveChanges();
                }
                lastCheck = DateTime.UtcNow;
            }
            else
            {
                SyncModifiedCalendarEvents(lastCheck.Value);
                lastCheck = null;
            }
#endif
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
