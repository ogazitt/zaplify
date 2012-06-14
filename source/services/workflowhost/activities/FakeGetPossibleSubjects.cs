using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class FakeGetPossibleSubjects : WorkflowActivity
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
                return Status.Complete;
            }

            // HACK: hardcode names for now until the graph queries are in place
            foreach (var subject in "Mike Maples;Mike Smith;Mike Abbott".Split(';'))
            {
                Item contact = MakeContact(workflowInstance, item, subject);
                suggestionList[subject] = JsonSerializer.Serialize(contact);
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
                TraceLog.TraceException("Deserializing contact failed", ex);
                return Status.Error;
            }

            // update the contact if it already exists, otherwise add a new contact
            try
            {
                Item dbContact = UserContext.Items.Include("FieldValues").Single(c => c.ID == contact.ID);
                foreach (var fv in contact.FieldValues)
                {
                    // add or update each of the fieldvalues
                    var dbfv = dbContact.GetFieldValue(fv.FieldName, true);
                    dbfv.Copy(fv);
                }
                dbContact.LastModified = now;
            }
            catch (Exception)
            {
                Folder folder = FindDefaultFolder(contact.UserID, contact.ItemTypeID);
                if (folder != null)
                    contact.FolderID = folder.ID;
                UserContext.Items.Add(contact);
            }
            try
            {
                UserContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Creating or adding Contact failed", ex);
                return Status.Error;
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

        private Item MakeContact(WorkflowInstance workflowInstance, Item item, string name)
        {
            DateTime now = DateTime.UtcNow;

            // create the new contact (detached) - it will be JSON-serialized and placed into 
            // the suggestion value field
            var itemID = Guid.NewGuid();
            Item contact = new Item()
            {
                ID = itemID,
                Name = name,
                FolderID = item.FolderID,
                ItemTypeID = SystemItemTypes.Contact,
                ParentID = null,
                UserID = item.UserID,
                FieldValues = new List<FieldValue>()
                {
                    new FieldValue() // sources
                    { 
                        FieldName = FieldNames.Sources,
                        ItemID = itemID,
                        Value = name == "Mike Abbott" ? "Facebook,Directory" : name == "Mike Smith" ? "Facebook" : name == "Mike Maples" ? "Directory" : "" // hardcode for now
                    },
                    new FieldValue() // facebook ID of the person
                    { 
                        FieldName = FieldNames.FacebookID,
                        ItemID = itemID,
                        Value = "100003631822064"  // hardcode to Mike Abbott for now
                    },
                },
                Created = now,
                LastModified = now,
            };

            return contact;
        }

        #endregion Helpers
    }
}
