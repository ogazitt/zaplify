using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class FakeGetPossibleSubjects : WorkflowActivity
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
                            var contact = WorkflowWorker.UserContext.Items.First(c => c.ParentID == contactsListID);
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

            // TODO: get contacts from the Contacts folder, Facebook, and Cloud AD
            // Generate a new contact for any non-matching FB or AD contact in the contacts list for this item

            // HACK: hardcode names for now until the graph queries are in place
            foreach (var subject in "Mike Maples;Mike Smith;Mike Abbott".Split(';'))
            {
                Item contact = CreateContact(workflowInstance, item, subject);
                suggestionList[subject] = JsonSerializer.Serialize(contact);
            }

            // inexact match
            return false;
        }

        private Item CreateContact(WorkflowInstance workflowInstance, Item item, string name)
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

            // create the new contact (detached) - it will be JSON-serialized and placed into 
            // the suggestion value field
            var itemID = Guid.NewGuid();
            Item contact = new Item()
            {
                ID = itemID,
                Name = name,
                FolderID = item.FolderID,
                ItemTypeID = SystemItemTypes.Contact,
                ParentID = listID,
                UserID = item.UserID,
                FieldValues = new List<FieldValue>()
                {
                    new FieldValue() // facebook ID of the person
                    { 
                        FieldID = new Guid("00000000-0000-0000-0000-000000000032"), // hardcode email 
                        ItemID = itemID,
                        Value = "100003631822064"  // hardcode to Mike Abbott for now
                    }
                },
                Created = now,
                LastModified = now,
            };

            return contact;
        }
    }
}
