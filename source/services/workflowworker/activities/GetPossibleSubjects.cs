using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

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

                    if (item.ItemTypeID != SystemItemTypes.Task)
                    {
                        TraceLog.TraceError("GetPossibleSubjects: non-Task Item passed in to Function");
                        return true;  // this will terminate the state
                    }

                    // if the Contacts field has been set and there are actual contacts in that sublist, a subject is already selected
                    // and this state can terminate
                    try
                    {
                        ItemType itemType = WorkflowWorker.UserContext.ItemTypes.Include("Fields").Single(it => it.ID == item.ItemTypeID);
                        Field field = itemType.Fields.Single(f => f.Name == TargetFieldName);
                        FieldValue contactsField = item.FieldValues.Single(fv => fv.FieldID == field.ID);
                        if (contactsField.Value != null)
                        {
                            Guid contactsListID = new Guid(contactsField.Value);
                            var contactsList = WorkflowWorker.UserContext.Items.Where(c => c.ParentID == contactsListID).ToList();
                            if (contactsList.Count > 0)
                            {
                                StoreInstanceData(workflowInstance, Workflow.LastStateData, contactsListID.ToString());
                                return true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // not an error condition if the Contacts field wasn't found or the list is empty
                    }

                    // if a user selected a suggestion, this state can terminate
                    if (data != null)
                    {
                        var suggList = data as List<Suggestion>;
                        if (suggList != null)
                        {
                            // return true if a user has selected an action
                            foreach (var sugg in suggList)
                                if (sugg.TimeSelected != null)
                                {
                                    StoreInstanceData(workflowInstance, Workflow.LastStateData, sugg.Value);
                                    return true;
                                }

                            // return false if the user hasn't yet selected an action but suggestions were already generated
                            // for the current state (we don't want a duplicate set of suggestions)
                            return false;
                        }
                    }

                    // analyze the item for possible subjects
                    var possibleSubjects = new Dictionary<string, string>();
                    bool completed = GetSubjects(item, possibleSubjects);

                    // if a subject was deciphered without user input, store it now and return
                    if (completed && possibleSubjects.Count == 1)
                    {
                        string serializedSubject = null;
                        foreach (var value in possibleSubjects.Values)
                            serializedSubject = value;
                        StoreInstanceData(workflowInstance, Workflow.LastStateData, serializedSubject);
                        StoreInstanceData(workflowInstance, FieldNames.Contacts, serializedSubject);
                        return true;
                    }

                    // add suggestions received in possibleSubjects
                    try
                    {
                        foreach (var s in possibleSubjects.Keys)
                        {
                            var sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                EntityID = item.ID,
                                EntityType = entity.GetType().Name,
                                WorkflowName = workflowInstance.Name,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                FieldName = TargetFieldName, 
                                DisplayName = s,
                                Value = possibleSubjects[s],
                                TimeSelected = null
                            };
                            WorkflowWorker.SuggestionsContext.Suggestions.Add(sugg);
                        }
                        
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("GetPossibleSubjects Activity failed; ex: " + ex.Message);
                        return false;
                    }
                });
            }
        }

        private FieldValue GetFieldValue(Item item, string fieldName, bool create)
        {
            Field field = null;
            try
            {
                ItemType itemType = WorkflowWorker.UserContext.ItemTypes.Include("Fields").Single(it => it.ID == item.ItemTypeID);
                field = itemType.Fields.Single(f => f.Name == fieldName);
            }
            catch (Exception)
            {
                return null;
            }
            try 
	        {	        
                FieldValue contactsField = item.FieldValues.Single(fv => fv.FieldID == field.ID);
                return contactsField;
	        }
	        catch (Exception)
            {
                if (create == true)
                {
                    FieldValue fv = new FieldValue()
                    {
                        FieldID = field.ID,
                        ItemID = item.ID,
                    };
                    item.FieldValues.Add(fv);
                    return fv;
                }
                return null;
            }
        }

        private bool GetSubjects(Item item, Dictionary<string, string> possibleSubjects)
        {
            // TODO: get contacts from the Contacts folder, Facebook, and Cloud AD
            // Generate a new contact for any non-matching FB or AD contact in the contacts list for this item

            // HACK: hardcode names for now until the graph queries are in place
            foreach (var subject in "Mike Maples;Mike Smith;Mike Abbott".Split(';'))
            {
                Item contact = CreateContact(item, subject);
                possibleSubjects[subject] = JsonSerializer.Serialize(contact);
            }

            // inexact match
            return false;
        }

        private Item CreateContact(Item item, string name)
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
            }

            // create the new contact (detached) - it will be JSON-serialized and placed into 
            // the suggestion value field
            Item contact = new Item()
            {
                ID = Guid.NewGuid(),
                Name = name,
                FolderID = item.FolderID,
                ItemTypeID = SystemItemTypes.Contact,
                ParentID = listID,
                UserID = item.ID,
                Created = now,
                LastModified = now,
            };

            return contact;
        }
    }
}
