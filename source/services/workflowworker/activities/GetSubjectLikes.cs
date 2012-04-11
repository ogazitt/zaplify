using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetSubjectLikes : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Choose from {$(" + FieldNames.SubjectHint + ")'s }Facebook interests"; } }
        public override string OutputParameterName { get { return ActivityParameters.Likes; } }
        public override string SuggestionType { get { return SuggestionTypes.ChooseOne; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    return Execute(
                        workflowInstance, 
                        entity,
                        data,
                        SystemItemTypes.Task,
                        (instance, e, dict) => { return GenerateSuggestions(instance, e, dict); });
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

            return Status.Pending;
        }
    }
}
