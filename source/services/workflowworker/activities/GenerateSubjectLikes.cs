using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GenerateSubjectLikes : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Choose from {$(" + FieldNames.SubjectHint + ")'s }Facebook interests"; } }
        public override string OutputParameterName { get { return ActivityParameters.LikeSuggestionList; } }
        public override string SuggestionType { get { return SuggestionTypes.ChooseMany; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("Execute: non-Item passed in");
                        return Status.Error;
                    }

                    if (VerifyItemType(item, SystemItemTypes.Task) == false)
                        return Status.Error;

                    // check for user selection
                    if (data != null)
                        return ProcessActivityData(workflowInstance, data);

                    // analyze the item for possible suggestions
                    var suggestions = new Dictionary<string, string>();
                    Status status = GenerateSuggestions(workflowInstance, entity, suggestions);

                    // if the function completed with an error, or without generating any data, return (this is typically a fail-fast state)
                    if (status == Status.Error || suggestions.Count == 0)
                        return status;

                    // construct the group display name
                    string groupDisplayName = GroupDisplayName;
                    if (groupDisplayName == null)
                        groupDisplayName = workflowInstance.State;
                    else
                        groupDisplayName = FormatParameterString(workflowInstance, groupDisplayName);

                    // add suggestions received from the suggestion function
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        int num = 0;
                        foreach (var s in suggestions.Keys)
                        {
                            // limit to four suggestions
                            if (num++ == 4)
                                break;

                            var sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                EntityID = entity.ID,
                                EntityType = entity.GetType().Name,
                                WorkflowType = workflowInstance.WorkflowType,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                SuggestionType = SuggestionType,
                                DisplayName = s,
                                GroupDisplayName = groupDisplayName,
                                SortOrder = num,
                                Value = suggestions[s],
                                TimeSelected = null
                            };
                            SuggestionsContext.Suggestions.Add(sugg);

                            // build the output list
                            if (!String.IsNullOrEmpty(sb.ToString()))
                                sb.Append(";");
                            sb.Append(String.Format("{0}={1},{2}={3}", 
                                ActivityParameters.Like, s, // Like=suggestion name
                                ActivityParameters.ParentID, sugg.ID));  // ParentID=suggestion guid
                        }

                        SuggestionsContext.SaveChanges();
                        
                        // set the results of this state
                        string likeList = sb.ToString();
                        StoreInstanceData(workflowInstance, OutputParameterName, likeList);
                        StoreInstanceData(workflowInstance, ActivityParameters.LastStateData, likeList);

                        return status;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("Execute: Activity execution failed", ex);
                        return Status.Error;
                    }
                });
            }
        }

        private Status GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("GenerateSuggestions: non-Item passed in");
                return Status.Error;
            }

            // make sure the subject was identified - if not move the state forward 
            string subjectItem = GetInstanceData(workflowInstance, ActivityParameters.Contact);
            if (subjectItem == null)
                return Status.Complete;

            // set up the FB API context
            FBGraphAPI fbApi = new FBGraphAPI();

            // get the current user
            User user = CurrentUser(item);
            if (user == null)
            {
                TraceLog.TraceError("GenerateSuggestions: couldn't find the user associated with item " + item.Name);
                return Status.Error;
            }

            try 
	        {	        
                UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                fbApi.AccessToken = cred.FBConsentToken;
	        }
	        catch (Exception)
	        {
                // the user not having a FB token isn't an error condition, but there's no way to generate suggestions,
                // so we need to move forward from this state
                return Status.Complete;
	        }

            Item subject = null;
            try
            {
                subject = JsonSerializer.Deserialize<Item>(subjectItem);
                
                // if the subjectItem is a reference, chase it down
                while (subject.ItemTypeID == SystemItemTypes.Reference)
                {
                    FieldValue refID = GetFieldValue(subject, FieldNames.ItemRef, false);
                    Guid refid = new Guid(refID.Value);
                    subject = UserContext.Items.Include("FieldValues").Single(i => i.ID == refid);
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("GenerateSuggestions: could not deserialize subject Item", ex);
                return Status.Error;
            }

            FieldValue fbID = GetFieldValue(subject, FieldNames.FacebookID, false);
            if (fbID == null || fbID.Value == null)
            {
                TraceLog.TraceError(String.Format("GenerateSuggestions: could not find Facebook ID for contact {0}", subject.Name));
                return Status.Complete;
            }

            try
            {
                // issue the query against the Facebook Graph API
                var results = fbApi.Query(fbID.Value, FBQueries.Likes);
                foreach (var like in results)
                {
                    string name = like.Name;
                    suggestionList[name] = name;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("GenerateSuggestions: Error calling Facebook Graph API", ex);
                return Status.Complete;
            }

            return Status.Complete;
        }
    }
}
