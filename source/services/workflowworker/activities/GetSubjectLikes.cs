using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetSubjectLikes : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetSubjectLikes; } }
        public override string TargetFieldName { get { return FieldNames.Likes; } }
        public override Func<WorkflowInstance, ServerEntity, object, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("GetSubjectLikes: non-Item passed in to Function");
                        return true;  // this will terminate the state
                    }

                    if (item.ItemTypeID != SystemItemTypes.Contact)
                    {
                        TraceLog.TraceError("GetSubjectLikes: non-Task Item passed in to Function");
                        return true;  // this will terminate the state
                    }

                    // if the Likes field has been set, this state can terminate
                    try
                    {
                        FieldValue likesField = GetFieldValue(item, FieldNames.Likes, false);
                        if (likesField != null && likesField.Value != null)
                        {
                            StoreInstanceData(workflowInstance, Workflow.LastStateData, likesField.Value);
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        // not an error condition if the Likes field wasn't found or the list is empty
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

                    // analyze the contact for possible likes
                    var possibleLikes = new Dictionary<string, string>();
                    bool completed = GetLikes(item, possibleLikes);

                    // if a like was deciphered without user input, store it now and return
                    if (completed && possibleLikes.Count == 1)
                    {
                        string like = null;
                        foreach (var value in possibleLikes.Values)
                            like = value;
                        StoreInstanceData(workflowInstance, Workflow.LastStateData, like);
                        StoreInstanceData(workflowInstance, FieldNames.Likes, like);
                        return true;
                    }

                    // add suggestions received in possibleLikes
                    try
                    {
                        foreach (var s in possibleLikes.Keys)
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
                                Value = possibleLikes[s],
                                TimeSelected = null
                            };
                            WorkflowWorker.SuggestionsContext.Suggestions.Add(sugg);
                        }
                        
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("GetSubjectLikes Activity failed; ex: " + ex.Message);
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

        private bool GetLikes(Item item, Dictionary<string, string> possibleLikes)
        {
            // TODO: get likes from the Contacts folder, Facebook, and Cloud AD
            // Generate a new contact for any non-matching FB or AD contact in the contacts list for this item

            // HACK: hardcode names for now until the graph queries are in place
            foreach (var like in "Golf;Seattle Sounders;Malcolm Gladwell".Split(';'))
            {
                possibleLikes[like] = like;
            }

            // inexact match
            return false;
        }
    }
}
