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
        /// <summary>
        /// Add facebook contact info into a contact Item
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="suggestionsContext"></param>
        /// <param name="item"></param>
        /// <returns>false if errors were encountered, otherwise true</returns>
        public static bool AddContactInfo(UserStorageContext userContext, Item item)
        {
            // if the contact has no facebook ID, there's nothing else to do
            FieldValue fbfv = item.GetFieldValue(FieldNames.FacebookID);
            if (fbfv == null)
                return true;

            User user = userContext.CurrentUser(item);
            if (user == null)
                return false;

            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();
            try
            {
                UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                fbApi.AccessToken = cred.FBConsentToken;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("FacebookProcessor.GetContactInfo: could not find Facebook credential or consent token", ex);
                return false;
            }

            // get or create an entityref in the entity ref list in the $User folder
            var entityRefItem = userContext.GetOrCreateEntityRef(user, item);
            if (entityRefItem == null)
            {
                TraceLog.TraceError("FacebookProcessor.GetContactInfo: could not retrieve or create an entity ref for this contact");
                return false;
            }

            // get the contact's profile information from facebook
            try
            {
                // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
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
                TraceLog.TraceException("FacebookProcessor.GetContactInfo: could not save Facebook information to Contact", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the user's basic information from Facebook
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="user"></param>
        /// <returns>false if errors were encountered, otherwise true</returns>
        public static bool GetUserInfo(UserStorageContext userContext, User user)
        {
            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();

            try
            {
                UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                fbApi.AccessToken = cred.FBConsentToken;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("FacebookProcessor.GetUserInfo: could not find Facebook credential or consent token", ex);
                return false;
            }

            // get or create a entity ref item for the user in the $User folder
            var entityRefItem = userContext.GetOrCreateEntityRef(user, user);
            if (entityRefItem == null)
            {
                TraceLog.TraceError("FacebookProcessor.GetUserInfo: could not retrieve or create a entity ref item for this user");
                return false;
            }

            // import information about the current user
            try
            {
                // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
                foreach (var userInfo in fbApi.Query("me", FBQueries.BasicInformation))
                {
                    // store the facebook ID
                    var fbid = (string)userInfo[FBQueryResult.ID];
                    entityRefItem.GetFieldValue(FieldNames.FacebookID, true).Value = fbid;
                    // augment the sources field with Facebook as a source
                    var sourcesFV = entityRefItem.GetFieldValue(FieldNames.Sources, true);
                    sourcesFV.Value = String.IsNullOrEmpty(sourcesFV.Value) ?
                        Sources.Facebook :
                        sourcesFV.Value.Contains(Sources.Facebook) ?
                            sourcesFV.Value :
                            String.Format("{0}:{1}", sourcesFV.Value, Sources.Facebook);
                    // store the picture URL
                    entityRefItem.GetFieldValue(FieldNames.Picture, true).Value = String.Format("https://graph.facebook.com/{0}/picture", fbid);
                    // augment with birthday and gender information if they don't yet exist
                    var birthday = (string)userInfo[FBQueryResult.Birthday];
                    if (birthday != null)
                        entityRefItem.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
                    var gender = (string)userInfo[FBQueryResult.Gender];
                    if (gender != null)
                        entityRefItem.GetFieldValue(FieldNames.Gender, true).Value = gender;
                    var location = (string)((FBQueryResult)userInfo[FBQueryResult.Location])[FBQueryResult.Name];
                    if (location != null)
                        entityRefItem.GetFieldValue(FieldNames.Location, true).Value = location;
                    TraceLog.TraceInfo("FacebookProcessor.GetUserInfo: added user birthday, gender, location");
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("FacebookProcessor.GetUserInfo: Facebook query for user's basic information failed", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Import facebook friends as possible contacts
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="user"></param>
        /// <returns>false if errors were encountered, otherwise true</returns>
        public static bool ImportFriendsAsPossibleContacts(UserStorageContext userContext, User user, Folder folder)
        {
            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();

            try
            {
                UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                fbApi.AccessToken = cred.FBConsentToken;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("FacebookProcessor.ImportFriendsAsPossibleContacts: could not find Facebook credential or consent token", ex);
                return false;
            }

            // get or create the possible contacts list in the $User folder
            Item possibleContactsList = userContext.GetOrCreateUserItemTypeList(user, SystemItemTypes.Contact);
            if (possibleContactsList == null)
            {
                TraceLog.TraceError("FacebookProcessor.ImportFriendsAsPossibleContacts: could not retrieve or create the possible contacts list");
                return false;
            }

            // get the current list of all possible contacts for this user ($User/PossibleContacts)
            var currentPossibleContacts = userContext.Items.Include("FieldValues").Where(ps => ps.UserID == user.ID && ps.FolderID == possibleContactsList.FolderID &&
                ps.ParentID == possibleContactsList.ID && ps.ItemTypeID == SystemItemTypes.NameValue &&
                ps.FieldValues.Any(fv => fv.FieldName == FieldNames.FacebookID)).ToList();

            // get the current list of all Items that are Contacts for this user
            var currentContacts = userContext.Items.Include("FieldValues").
                        Where(c => c.UserID == user.ID && c.ItemTypeID == SystemItemTypes.Contact).ToList();

            // get all the user's friends and add them as serialized contacts to the $User/PossibleContacts list
            float sort = 1f;
            DateTime now = DateTime.UtcNow;
            try
            {
                var results = fbApi.Query("me", FBQueries.Friends).ToList();
                TraceLog.TraceInfo(String.Format("FacebookProcessor.ImportFriendsAsPossibleContacts: found {0} Facebook friends", results.Count));
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
                            // contact by this name exists but facebook ID isn't set; assume this is a duplicate and set the FBID
                            fbFV.Value = (string)friend[FBQueryResult.ID];
                            var sourcesFV = existingContact.GetFieldValue(FieldNames.Sources, true);
                            sourcesFV.Value = string.IsNullOrEmpty(sourcesFV.Value) ? Sources.Facebook : string.Concat(sourcesFV.Value, ",", Sources.Facebook);
                            process = false;
                            break;
                        }
                        if (fbFV.Value == (string)friend[FBQueryResult.ID])
                        {
                            // this is definitely a duplicate - don't add it
                            process = false;
                            break;
                        }
                        // getting here means that a contact by this name was found but had a different FBID - so this new contact is unique
                    }

                    // add the contact if it wasn't detected as a duplicate
                    if (process)
                    {
                        var contact = new Item()
                        {
                            ID = Guid.NewGuid(),
                            Name = (string)friend[FBQueryResult.Name],
                            ParentID = null,
                            UserID = user.ID,
                            FolderID = folder.ID,
                            ItemTypeID = SystemItemTypes.Contact,
                            SortOrder = sort++,
                            Created = now,
                            LastModified = now,
                            FieldValues = new List<FieldValue>(),
                        };
                        contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.FacebookID, Value = (string)friend[FBQueryResult.ID] });
                        contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.Sources, Value = Sources.Facebook });
                        string jsonContact = JsonSerializer.Serialize(contact);

                        // store the serialized contact in the value of a new NameValue item on the PossibleContacts list
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
                        // also add the FBID as a fieldvalue on the namevalue item which corresponds to the possible contact, for easier dup handling
                        nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.FacebookID, ItemID = nameValItem.ID, Value = (string)friend[FBQueryResult.ID] });

                        // add this new possible subject to the DB and to the working list of possible contacts
                        userContext.Items.Add(nameValItem);
                        currentPossibleContacts.Add(nameValItem);
                    }
                }

                userContext.SaveChanges();
                TraceLog.TraceInfo(String.Format("FacebookProcessor.ImportFriendsAsPossibleContacts: added {0} possible contacts to $User.PossibleContacts", results.Count));
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("FacebookProcessor.ImportFriendsAsPossibleContacts: could not retrieve or create a new PossibleContact", ex);
                return false;
            }

            return true;
        }
    }
}
