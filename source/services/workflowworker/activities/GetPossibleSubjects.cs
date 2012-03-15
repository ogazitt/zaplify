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
                    var possibleSubjects = new Dictionary<string, Guid>();
                    bool completed = GetSubjects(item, possibleSubjects);

                    // if a subject was deciphered without user input, store it now and return
                    if (completed && possibleSubjects.Count == 1)
                    {
                        string subjectID = null;
                        foreach (var value in possibleSubjects.Values)
                            subjectID = value.ToString();
                        StoreInstanceData(workflowInstance, Workflow.LastStateData, subjectID);
                        StoreInstanceData(workflowInstance, FieldNames.Contacts, subjectID);
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
                                Value = possibleSubjects[s].ToString(),
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

        private bool GetSubjects(Item item, Dictionary<string, Guid> possibleSubjects)
        {
            // TODO: get contacts from the Contacts folder, Facebook, and Cloud AD
            // Generate a new contact for any non-matching FB or AD contact in the contacts list for this item
            
            // HACK: hardcode names for now.  The guids associated with these are random and not in the Contacts!!
            foreach (var subject in "Mike Maples;Mike Smith;Mike Abbott".Split(';'))
            {
                possibleSubjects[subject] = Guid.NewGuid();
            }

            // inexact match
            return false;
        }
    }
}
