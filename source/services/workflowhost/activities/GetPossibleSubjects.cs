using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Protocols.OAuth;
using Microsoft.IdentityModel.Protocols.OAuth.Client;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.ADGraph;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceHost.Nlp;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class GetPossibleSubjects : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Who is this for?"; } }
        public override string OutputParameterName { get { return ActivityVariables.Contact; } }
        public override string SuggestionType { get { return SuggestionTypes.ChooseOneSubject; } }
        public override string TargetFieldName { get { return FieldNames.Contacts; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("Entity is not an Item");
                        return Status.Error; 
                    }

                    if (VerifyItemType(item, SystemItemTypes.Task) == false)
                        return Status.Error;

                    // if the Contacts field has been set and there are actual contacts in that sublist, a subject is already selected
                    // and this state can terminate
                    try
                    {
                        FieldValue contactsField = item.GetFieldValue(TargetFieldName);
                        if (contactsField != null && contactsField.Value != null)
                        {
                            Guid contactsListID = new Guid(contactsField.Value);

                            // use the first contact as the subject
                            var contact = UserContext.Items.Include("FieldValues").First(c => c.ParentID == contactsListID);
                            StoreInstanceData(workflowInstance, ActivityVariables.LastStateData, JsonSerializer.Serialize(contact));
                            StoreInstanceData(workflowInstance, OutputParameterName, JsonSerializer.Serialize(contact));
                            return Status.Complete;
                        }
                    }
                    catch (Exception)
                    {
                        // not an error condition if the Contacts field wasn't found or the list is empty
                    }

                    // if a user selected a suggestion, this state can terminate
                    if (data != null)
                    {
                        var status = ProcessActivityData(workflowInstance, data);
                        
                        // create the contact reference, and create or update the actual contact
                        if (status == Status.Complete)
                            status = CreateContact(workflowInstance, item);
                        return status;
                    }

                    // generate suggestions for the possible subjects
                    return CreateSuggestions(workflowInstance, entity, GenerateSuggestions);
                });
            }
        }

        private Status GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("Entity is not an Item");
                return Status.Error;
            }

            User user = UserContext.GetUser(item.UserID, true);
            if (user == null)
            {
                TraceLog.TraceError("Could not find the user associated with Item " + item.Name);
                return Status.Error;
            }

            ADGraphAPI adApi = new ADGraphAPI();
            string adRefreshToken = null;

            // check for FB and/or AD credentials
            UserCredential cred = user.GetCredential(UserCredential.FacebookConsent);
            if (cred != null && cred.AccessToken != null) { adApi.FacebookAccessToken = cred.AccessToken; }
            cred = user.GetCredential(UserCredential.CloudADConsent);
            if (cred != null && cred.RenewalToken != null) { adRefreshToken = cred.RenewalToken; }

            if (adApi.FacebookAccessToken == null && adRefreshToken == null)
            {   // user not having either token is not an error condition, but there is no way to generate suggestions
                // just move forward from this state
                return Status.Complete;
            }

            // if a refresh token exists for AD, get an access token from Azure ACS for the Azure AD service
            if (adRefreshToken != null)
            {
                try
                {
                    AccessTokenRequestWithRefreshToken request = new AccessTokenRequestWithRefreshToken(new Uri(AzureOAuthConfiguration.GetTokenUri()))
                    {
                        RefreshToken = adRefreshToken,
                        ClientId = AzureOAuthConfiguration.GetClientIdentity(),
                        ClientSecret = AzureOAuthConfiguration.ClientSecret,
                        Scope = AzureOAuthConfiguration.RelyingPartyRealm,
                    };
                    OAuthMessage message = OAuthClient.GetAccessToken(request);
                    AccessTokenResponse authzResponse = message as AccessTokenResponse;
                    adApi.ADAccessToken = authzResponse.AccessToken;
                    
                    // workaround for ACS trashing the refresh token
                    if (!String.IsNullOrEmpty(authzResponse.RefreshToken))
                    {
                        TraceLog.TraceInfo("Storing new CloudAD refresh token");
                        user.AddCredential(UserCredential.CloudADConsent, authzResponse.AccessToken, null, authzResponse.RefreshToken);
                        UserContext.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Could not contact ACS to get an access token", ex);
                    
                    // Facebook credentials are not available
                    if (adApi.FacebookAccessToken == null)
                        return Status.Pending;  // could be a temporary outage, do not move off this state
                }
            }

            // extract a subject hint if one hasn't been discovered yet
            string subjectHint = GetInstanceData(workflowInstance, ActivityVariables.SubjectHint);
            if (String.IsNullOrEmpty(subjectHint))
            {
                try
                {
                    Phrase phrase = new Phrase(item.Name);
                    if (phrase.Task != null)
                    {
                        subjectHint = phrase.Task.Subject;
                        if (!String.IsNullOrWhiteSpace(subjectHint))
                            StoreInstanceData(workflowInstance, ActivityVariables.SubjectHint, subjectHint);
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Could not initialize NLP engine", ex);
                }
            }

            // get contacts from Cloud AD and Facebook via the AD Graph Person service
            // TODO: also get local contacts from the Contacts folder
            try
            {
                var results = adApi.Query(subjectHint ?? "");  
                foreach (var subject in results)
                {
                    // serialize an existing contact corresponding to the subject, 
                    // or generate a new serialized contact if one wasn't found
                    Item contact = MakeContact(workflowInstance, item, subject);
                    suggestionList[contact.Name] = JsonSerializer.Serialize(contact);
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Could not contact Person Service", ex);
                return Status.Error;
            }

            // inexact match
            return Status.Pending;
        }

        #region Helpers

        private Status CreateContact(WorkflowInstance workflowInstance, Item item)
        {
            DateTime now = DateTime.UtcNow;
            FieldValue contactsField = item.GetFieldValue(TargetFieldName, true);
            Guid listID = contactsField.Value != null ? new Guid(contactsField.Value) : Guid.NewGuid();

            // if the contacts sublist under the item doesn't exist, create it now
            if (contactsField.Value == null)
            {
                Item list = new Item()
                {
                    ID = listID,
                    Name = TargetFieldName,
                    IsList = true,
                    FolderID = item.FolderID,
                    ItemTypeID = SystemItemTypes.Reference,
                    ParentID = item.ID,
                    UserID = item.UserID,
                    Created = now,
                    LastModified = now,
                };
                contactsField.Value = listID.ToString();
                try
                {
                    UserContext.Items.Add(list);
                    UserContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Creating Contact sublist failed", ex);
                    return Status.Error;
                }
            }

            // get the subject out of the InstanceData bag
            Item contact = null;
            try
            {
                var contactString = GetInstanceData(workflowInstance, OutputParameterName);
                contact = JsonSerializer.Deserialize<Item>(contactString);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Deserializing Contact failed", ex);
                return Status.Error;                
            }

            // update the contact if it already exists, otherwise add a new contact
            if (UserContext.Items.Any(c => c.ID == contact.ID))
            {
                try
                {
                    UserContext.SaveChanges();
                    Item dbContact = UserContext.Items.Include("FieldValues").Single(c => c.ID == contact.ID);
                    foreach (var fv in contact.FieldValues)
                    {
                        // add or update each of the fieldvalues
                        var dbfv = dbContact.GetFieldValue(fv.FieldName, true);
                        dbfv.Copy(fv);
                    }
                    dbContact.LastModified = now;
                    UserContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("Update Contact failed", ex);
                    return Status.Error;
                }
            }
            else
            {
                try
                {
                    Folder folder = FindDefaultFolder(contact.UserID, contact.ItemTypeID);
                    if (folder != null)
                        contact.FolderID = folder.ID;
                    UserContext.Items.Add(contact);
                    UserContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("CreateContact: creating contact failed", ex);
                    return Status.Error;
                }

                User user = UserContext.GetUser(item.UserID);
                if (user == null)
                {
                    TraceLog.TraceError("Could not find the user associated with Item " + item.Name);
                    return Status.Error;
                }

                // create an operation corresponding to the new contact creation
                var operation = UserContext.CreateOperation(user, "POST", (int?)System.Net.HttpStatusCode.Created, contact, null);
                if (operation == null)
                {
                    TraceLog.TraceError("Failed to create operation");
                    return Status.Error;
                }

                // kick off the New Contact workflow
                WorkflowHost.InvokeWorkflowForOperation(UserContext, SuggestionsContext, operation);
            }

            // add a contact reference to the contact list
            Guid refID = Guid.NewGuid();
            var contactRef = new Item()
            {
                ID = refID,
                Name = contact.Name,
                ItemTypeID = SystemItemTypes.Reference,
                FolderID = item.FolderID,
                ParentID = listID,
                UserID = contact.UserID,
                Created = now,
                LastModified = now,
                FieldValues = new List<FieldValue>()
                {
                    new FieldValue() { FieldName = FieldNames.EntityRef, ItemID = refID, Value = contact.ID.ToString() },
                    new FieldValue() { FieldName = FieldNames.EntityType, ItemID = refID, Value = EntityTypes.Item }
                }
            };
            try
            {
                UserContext.Items.Add(contactRef);
                UserContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Creating Contact reference failed", ex);
                return Status.Error;
            }

            // add a Suggestion with a RefreshEntity FieldName to the list, to tell the UI that the 
            // workflow changed the Item
            SignalEntityRefresh(workflowInstance, item);

            return Status.Complete;
        }

        private Folder FindDefaultFolder(Guid userID, Guid itemTypeID)
        {
            // TODO: support user defaults stored in hidden System folder
            try
            {
                return UserContext.Folders.First(f => f.UserID == userID && f.ItemTypeID == itemTypeID);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Item GetContact(Guid userid, ADQueryResult subject)
        {
            try
            {
                // try to get an existing contact by name
                var contact = UserContext.Items.
                    Include("FieldValues").
                    Single(i => i.UserID == userid && i.ItemTypeID == SystemItemTypes.Contact && i.Name == subject.Name);

                // ensure that if a facebook ID exists, it matches the FBID of the subject just retrieved
                var fbid = contact.GetFieldValue(FieldNames.FacebookID);
                var ids = subject.IDs.Where(id => id.Source == Sources.Facebook).ToList();
                if (ids.Count > 0 && fbid != null && fbid.Value != ids[0].Value)
                    return null;

                return contact;
            }
            catch (Exception)
            {
                // contact not found 
                return null;
            }
        }

        private Item MakeContact(WorkflowInstance workflowInstance, Item item, ADQueryResult subject)
        {
            DateTime now = DateTime.UtcNow;
            /*
            FieldValue contactsField = GetFieldValue(item, TargetFieldName, true);
            Guid listID = contactsField.Value != null ? new Guid(contactsField.Value) : Guid.NewGuid();

            // if the contacts sublist under the item doesn't exist, create it now
            if (contactsField.Value == null)
            {
                Item list = new Item()
                {
                    ID = listID,
                    Name = TargetFieldName,
                    IsList = true,
                    FolderID = item.FolderID,
                    ItemTypeID = SystemItemTypes.Reference,
                    ParentID = item.ID,
                    UserID = item.UserID,
                    Created = now,
                    LastModified = now,
                };
                contactsField.Value = listID.ToString();
                UserContext.Items.Add(list);
                UserContext.SaveChanges();

                // add a Suggestion with a RefreshEntity FieldName to the list, to tell the UI that the 
                // workflow changed the Item
                SignalEntityRefresh(workflowInstance, item);
            }
            */
            // try to find an existing contact using matching heuristic
            var contact = GetContact(item.UserID, subject);

            // if the contact wasn't found, create the new contact (detached) - it will be JSON-serialized and placed into 
            // the suggestion value field
            if (contact == null)
            {
                contact = new Item()
                {
                    ID = Guid.NewGuid(),
                    Name = subject.Name,
                    FolderID = item.FolderID,
                    ItemTypeID = SystemItemTypes.Contact,
                    ParentID = null /*listID*/,
                    UserID = item.UserID,
                    FieldValues = new List<FieldValue>(),
                    Created = now,
                    LastModified = now,
                };
            }

            // add various FieldValues to the contact if available
            try
            {
                // add sources
                string sources = String.Join(",", subject.IDs.Select(id => id.Source));
                contact.GetFieldValue(FieldNames.Sources, true).Value = sources;
                // add birthday
                if (subject.Birthday != null)
                    contact.GetFieldValue(FieldNames.Birthday, true).Value = ((DateTime)subject.Birthday).ToString("d");
                // add facebook ID
                string fbID = subject.IDs.Single(id => id.Source == ADQueryResultValue.FacebookSource).Value;
                if (fbID != null)
                    contact.GetFieldValue(FieldNames.FacebookID, true).Value = fbID;
            }
            catch (Exception) { }

            return contact;
        }

        #endregion Helpers
    }
}
