using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;

namespace BuiltSteady.Zaplify.ServiceHost.Helpers
{
    public class FacebookHelper
    {
        public const string TRACE_NO_FB_TOKEN = "Facebook access token is not available";
        public const string TRACE_NO_CONTACT_ENTITYREF = "Could not retrieve or create an EntityRef for Contact";
        public const string TRACE_NO_SAVE_FBCONTACTINFO = "Could not save Facebook information for Contact";


        public static bool AddContactInfo(UserStorageContext userContext, Item item)
        {  
            FieldValue fbfv = item.GetFieldValue(FieldNames.FacebookID);
            if (fbfv == null)
                return true;
            
            User user = userContext.GetUser(item.UserID, true);
            if (user == null)
                return false;

            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();
            UserCredential cred = user.GetCredential(UserCredential.FacebookConsent);
            if (cred != null && cred.AccessToken != null)
            {
                fbApi.AccessToken = cred.AccessToken;
            }
            else
            {
                TraceLog.TraceError(TRACE_NO_FB_TOKEN);
                return false;
            }

            // get or create an EntityRef in the UserFolder EntityRefs list
            var entityRefItem = userContext.UserFolder.GetEntityRef(user, item);
            if (entityRefItem == null)
            {
                TraceLog.TraceError(TRACE_NO_CONTACT_ENTITYREF);
                return false;
            }

            try
            {   // get the Contact information from Facebook
                // using foreach because the Query API returns an IEnumerable, but there is only one result
                foreach (var contact in fbApi.Query(fbfv.Value, FBQueries.BasicInformation))
                {
                    item.GetFieldValue(FieldNames.Picture, true).Value = String.Format("https://graph.facebook.com/{0}/picture", fbfv.Value);
                    var birthday = (string)contact[FBQueryResult.Birthday];
                    if (birthday != null)
                        item.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
                    var gender = (string)contact[FBQueryResult.Gender];
                    if (gender != null)
                        entityRefItem.GetFieldValue(FieldNames.Gender, true).Value = gender;
                }
                userContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(TRACE_NO_SAVE_FBCONTACTINFO, ex);
                return false;
            }

            return true;
        }

        public static bool GetUserInfo(User user, UserStorageContext storage)
        {
            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();
            UserCredential cred = user.GetCredential(UserCredential.FacebookConsent);
            if (cred != null && cred.AccessToken != null)
            {
                fbApi.AccessToken = cred.AccessToken;
            }
            else
            {
                TraceLog.TraceError(TRACE_NO_FB_TOKEN);
                return false;
            }

            // store user information from Facebook in UserProfile
            UserProfile userProfile = storage.ClientFolder.GetUserProfile(user);
            if (userProfile == null)
            {
                TraceLog.TraceError("Could not access UserProfile to import Facebook information into.");
                return false;
            }

            try
            {   // import information about the current user
                // using foreach because the Query API returns an IEnumerable, but there is only one result
                foreach (var userInfo in fbApi.Query("me", FBQueries.BasicInformation))
                {   
                    // import FacebookID
                    userProfile.FacebookID = (string)userInfo[FBQueryResult.ID];
                    // import name if not already set
                    if (userProfile.FirstName == null)
                        userProfile.FirstName = (string)userInfo["first_name"];
                    if (userProfile.LastName == null)
                        userProfile.LastName = (string)userInfo["last_name"];
                    // import picture if not already set
                    if (userProfile.Picture == null)
                        userProfile.Picture = String.Format("https://graph.facebook.com/{0}/picture", userProfile.FacebookID);
                    // import birthday if not already set
                    if (userProfile.Birthday == null)
                        userProfile.Birthday = (string)userInfo[FBQueryResult.Birthday];
                    // import gender if not already set
                    if (userProfile.Gender == null)
                        userProfile.Gender = (string)userInfo[FBQueryResult.Gender];
                    // import geolocation if not already set
                    if (userProfile.GeoLocation == null)
                        userProfile.GeoLocation = (string)((FBQueryResult)userInfo[FBQueryResult.Location])[FBQueryResult.Name];
                    TraceLog.TraceInfo("Imported Facebook information into UserProfile");
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Facebook query for basic User information failed", ex);
                return false;
            }
            return true;
        }

        public static bool ImportFriendsAsPossibleContacts(User user, UserStorageContext userContext)
        {
            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();
            UserCredential cred = user.GetCredential(UserCredential.FacebookConsent);
            if (cred != null && cred.AccessToken != null)
            {
                fbApi.AccessToken = cred.AccessToken;
            }
            else
            {
                TraceLog.TraceError(TRACE_NO_FB_TOKEN);
                return false;
            }

            // get or create the list for Contact item types in the UserFolder
            Item possibleContactsList = userContext.UserFolder.GetListForItemType(user, SystemItemTypes.Contact);
            if (possibleContactsList == null)
            {
                TraceLog.TraceError("Could not retrieve or create the possible contacts list");
                return false;
            }

            // get the current list of all possible contacts for this user
            var currentPossibleContacts = userContext.Items.Include("FieldValues").Where(ps => ps.UserID == user.ID && ps.FolderID == possibleContactsList.FolderID &&
                ps.ParentID == possibleContactsList.ID && ps.ItemTypeID == SystemItemTypes.NameValue &&
                ps.FieldValues.Any(fv => fv.FieldName == FieldNames.FacebookID)).ToList();

            // get the current list of all Items that are Contacts for this user
            var currentContacts = userContext.Items.Include("FieldValues").
                        Where(c => c.UserID == user.ID && c.ItemTypeID == SystemItemTypes.Contact).ToList();

            // get all the user's friends and add them as serialized contacts to the possible contacts list
            DateTime now = DateTime.UtcNow;
            try
            {
                var results = fbApi.Query("me", FBQueries.Friends).ToList();
                TraceLog.TraceInfo(String.Format("Found {0} Facebook friends", results.Count));
                foreach (var friend in results)
                {
                    // check if a possible contact by this name and with this FBID already exists - and if so, skip it
                    if (currentPossibleContacts.Any(
                            ps => ps.Name == (string)friend[FBQueryResult.Name] &&
                            ps.FieldValues.Any(fv => fv.FieldName == FieldNames.FacebookID && fv.Value == (string)friend[FBQueryResult.ID])))
                        continue;

                    bool process = true;

                    // check if a contact by this name already exists
                    var existingContacts = currentContacts.Where(c => c.Name == (string)friend[FBQueryResult.Name]).ToList();
                    foreach (var existingContact in existingContacts)
                    {
                        var fbFV = existingContact.GetFieldValue(FieldNames.FacebookID, true);
                        if (fbFV.Value == null)
                        {
                            // contact with this name exists but no FacebookID, assume same and set the FacebookID
                            fbFV.Value = (string)friend[FBQueryResult.ID];
                            var sourcesFV = existingContact.GetFieldValue(FieldNames.Sources, true);
                            sourcesFV.Value = string.IsNullOrEmpty(sourcesFV.Value) ? Sources.Facebook : string.Concat(sourcesFV.Value, ",", Sources.Facebook);
                            process = false;
                            break;
                        }
                        if (fbFV.Value == (string)friend[FBQueryResult.ID])
                        {   // FacebookIDs are same, definitely a duplicate, do not add
                            process = false;
                            break;
                        }
                        // contact with same name was found but had a different FacebookID, add as a new contact
                    }

                    // add contact if not a duplicate
                    if (process)
                    {
                        var contact = new Item()
                        {
                            ID = Guid.NewGuid(),
                            Name = (string)friend[FBQueryResult.Name],
                            UserID = user.ID,
                            ItemTypeID = SystemItemTypes.Contact,
                            FieldValues = new List<FieldValue>(),
                        };
                        contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.FacebookID, Value = (string)friend[FBQueryResult.ID] });
                        contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.Sources, Value = Sources.Facebook });
                        string jsonContact = JsonSerializer.Serialize(contact);

                        // store the serialized json contact in the value of a new NameValue item in possible contacts list
                        var nameValItem = new Item()
                        {
                            ID = Guid.NewGuid(),
                            Name = (string)friend[FBQueryResult.Name],
                            FolderID = possibleContactsList.FolderID,
                            ParentID = possibleContactsList.ID,
                            UserID = user.ID,
                            ItemTypeID = SystemItemTypes.NameValue,
                            Created = now,
                            LastModified = now,
                            FieldValues = new List<FieldValue>()
                        };
                        nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.Value, ItemID = nameValItem.ID, Value = jsonContact });
                        // add the FacebookID as a fieldvalue on the namevalue item which corresponds to the possible contact, for easier duplicate detection
                        nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.FacebookID, ItemID = nameValItem.ID, Value = (string)friend[FBQueryResult.ID] });

                        // add new possible subject to the storage and to the working list of possible contacts
                        userContext.Items.Add(nameValItem);
                        currentPossibleContacts.Add(nameValItem);
                    }
                }

                userContext.SaveChanges();
                TraceLog.TraceInfo(String.Format("Added {0} possible contacts to list", results.Count));
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Could not retrieve or create a new possible Contact", ex);
                return false;
            }
            return true;
        }
    }
}
