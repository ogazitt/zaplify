using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
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

                    // get or create an entityref in the entity ref list in the $User folder
                    var entityRefItem = UserContext.GetOrCreateEntityRef(user, item);
                    if (entityRefItem == null)
                    {
                        TraceLog.TraceError("GetContactInfoFromFacebook: could not retrieve or create an entity ref for this contact");
                        return Status.Error;
                    }

                    // get the contact's profile information from facebook
                    try
                    {
                        // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
                        foreach (var contact in fbApi.Query(fbfv.Value, FBQueries.BasicInformation))
                        {
                            item.GetFieldValue(FieldNames.Picture, true).Value = String.Format("https://graph.facebook.com/{0}/picture", fbfv.Value);
                            var birthday = (string) contact[FBQueryResult.Birthday];
                            if (birthday != null)
                                item.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
                            var gender = (string) contact[FBQueryResult.Gender];
                            if (gender != null)
                                entityRefItem.GetFieldValue(FieldNames.Gender, true).Value = gender;
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
