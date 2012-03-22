using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using Microsoft.IdentityModel.Protocols.OAuth;
using Microsoft.IdentityModel.Protocols.OAuth.Client;
using BuiltSteady.Zaplify.ServiceUtilities.ADGraph;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleSubjects : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetPossibleSubjects; } }
        public override string TargetFieldName { get { return FieldNames.Contacts; } }
        public override Func<WorkflowInstance, ServerEntity, object, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("GetPossibleSubjects: non-Item passed in to Function");
                        return true;  // this will terminate the state
                    }

                    if (VerifyItemType(item, SystemItemTypes.Task) == false)
                        return true;  // this will terminate the state

                    // if the Contacts field has been set and there are actual contacts in that sublist, a subject is already selected
                    // and this state can terminate
                    try
                    {
                        FieldValue contactsField = GetFieldValue(item, TargetFieldName, false);
                        if (contactsField != null && contactsField.Value != null)
                        {
                            Guid contactsListID = new Guid(contactsField.Value);

                            // use the first contact as the subject
                            var contact = WorkflowWorker.UserContext.Items.Include("FieldValues").First(c => c.ParentID == contactsListID);
                            StoreInstanceData(workflowInstance, Workflow.LastStateData, JsonSerializer.Serialize(contact));
                            StoreInstanceData(workflowInstance, TargetFieldName, JsonSerializer.Serialize(contact));
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        // not an error condition if the Contacts field wasn't found or the list is empty
                    }

                    // if a user selected a suggestion, this state can terminate
                    if (data != null)
                        return ProcessActivityData(workflowInstance, data);

                    // generate suggestions for the possible subjects
                    return CreateSuggestions(workflowInstance, entity, GenerateSuggestions);
                });
            }
        }

        private bool GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("GenerateSuggestions: non-Item passed in");
                return true;  // this will terminate the state
            }

            User user = CurrentUser(item);
            if (user == null)
            {
                TraceLog.TraceError("GenerateSuggestions: couldn't find the user associated with item " + item.Name);
                return true;  // this will terminate the state
            }

            ADGraphAPI adApi = new ADGraphAPI();
            string adRefreshToken = null;

            // try to retrieve FB and/or AD credentials
            try
            {
                UserCredential creds = user.UserCredentials.Single(uc => uc.ADConsentToken != null || uc.FBConsentToken != null);
                adApi.FacebookAccessToken = creds.FBConsentToken;
                adRefreshToken = creds.ADConsentToken;
            }
            catch (Exception)
            {
                // the user not having either token isn't an error condition, but there's no way to generate suggestions,
                // so we need to move forward from this state
                return true;
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

                }
                catch (Exception ex)
                {
                    TraceLog.TraceError("GenerateSuggestions: could not contact ACS to get an access token; ex: " + ex.Message);
                    
                    // if the FB credentials aren't available, there is nothing the Person service can do for us
                    if (adApi.FacebookAccessToken == null)
                        return false;  // this could be a temporary outage, so don't move off this state
                }
            }

            // get contacts from Cloud AD and Facebook via the AD Graph Person service
            // TODO: also get local contacts from the Contacts folder

            try
            {
                string subjectHint = GetInstanceData(workflowInstance, FieldNames.SubjectHint);
                var results = adApi.Query(subjectHint ?? "");  
                foreach (var subject in results)
                {
                    // Generate a new contact for any non-matching FB or AD contact in the contacts list for this item
                    // TODO: for now this code assumes that all contacts are new and so it creates a new Contact
                    Item contact = CreateContact(workflowInstance, item, subject);
                    suggestionList[contact.Name] = JsonSerializer.Serialize(contact);
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("GenerateSuggestions: could not contact Person Service; ex: " + ex.Message);
                return true;  // move forward from this state
            }

            // inexact match
            return false;
        }

        private Item CreateContact(WorkflowInstance workflowInstance, Item item, ADQueryResult subject)
        {
            DateTime now = DateTime.UtcNow;
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
                    ItemTypeID = SystemItemTypes.Contact,
                    ParentID = item.ID,
                    UserID = item.UserID,
                    Created = now,
                    LastModified = now,
                };
                contactsField.Value = listID.ToString();
                WorkflowWorker.UserContext.Items.Add(list);
                WorkflowWorker.UserContext.SaveChanges();

                // add a Suggestion with a RefreshEntity FieldName to the list, to tell the UI that the 
                // workflow changed the Item
                SignalEntityRefresh(workflowInstance, item);
            }

            // create an ID for the new contact
            var itemID = Guid.NewGuid();

            // get the facebook ID of the subject if available
            FieldValue fbfv = null;
            try
            {
                string fbID = subject.IDs.Single(id => id.Source == ADQueryResultValue.FacebookSource).Value;
                fbfv = new FieldValue()
                {
                    FieldID = new Guid("00000000-0000-0000-0000-000000000032"), // hardcode to the email field for now
                    ItemID = itemID,
                    Value = fbID  
                };
            }
            catch (Exception) { }

            // create the new contact (detached) - it will be JSON-serialized and placed into 
            // the suggestion value field
            Item contact = new Item()
            {
                ID = itemID,
                Name = subject.Name,
                FolderID = item.FolderID,
                ItemTypeID = SystemItemTypes.Contact,
                ParentID = listID,
                UserID = item.UserID,
                FieldValues = fbfv == null ? null : new List<FieldValue>() { fbfv },
                Created = now,
                LastModified = now,
            };

            return contact;
        }
    }
}
