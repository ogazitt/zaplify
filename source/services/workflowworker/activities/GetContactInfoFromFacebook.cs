using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetContactInfoFromFacebook : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("GetContactInfoFromFacebook: non-Item passed in to Function");
                        return Status.Error;
                    }

                    if (VerifyItemType(item, SystemItemTypes.Contact) == false)
                        return Status.Error;

                    // if the contact has no facebook ID, there's nothing else to do
                    FieldValue fbfv = item.GetFieldValue(FieldNames.FacebookID);
                    if (fbfv == null)
                        return Status.Complete;

                    User user = CurrentUser(item);
                    if (user == null)
                        return Status.Error;

                    // set up the FB API context
                    FBGraphAPI fbApi = new FBGraphAPI();
                    try
                    {
                        UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                        fbApi.AccessToken = cred.FBConsentToken;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("GetContactInfoFromFacebook: could not find Facebook credential or consent token", ex);
                        return Status.Error;
                    }

                    // get the contact's profile information from facebook
                    try
                    {
                        // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
                        foreach (var contact in fbApi.Query(fbfv.Value, FBQueries.BasicInformation))
                        {
                            var birthday = contact[FBQueryResult.Birthday];
                            if (birthday != null)
                            {
                                FieldValue birthdayFV = item.GetFieldValue(FieldNames.Birthday, true);
                                if (birthdayFV != null) birthdayFV.Value = birthday;
                            }
                        }
                        UserContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("GetContactInfoFromFacebook: could not save Facebook information to Contact", ex);
                    }

                    return Status.Complete;
                });
            }
        }
    }
}
