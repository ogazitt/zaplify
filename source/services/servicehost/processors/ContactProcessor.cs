using System;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost.Helpers;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class ContactProcessor : ItemProcessor
    {
        public ContactProcessor(User user, UserStorageContext storage)
        {
            this.user = user;
            this.storage = storage;
        }

        public override bool ProcessCreate(Item item)
        {
            if (base.ProcessCreate(item))
                return true;

            var fbret = FacebookHelper.AddContactInfo(storage, item);
            var pcret = PossibleContactHelper.AddContact(storage, item);
            return fbret && pcret;
        }

        public override bool ProcessDelete(Item item)
        {
            if (base.ProcessDelete(item))
                return true;
            return PossibleContactHelper.RemoveContact(storage, item);
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            // base handles ItemType changing
            if (base.ProcessUpdate(oldItem, newItem))
                return true;

            if (newItem.Name != oldItem.Name)
            {   // name changed, process like new item
                ProcessCreate(newItem);
                return true;
            }

            // if the facebook ID is set or changed, retrieve FB info
            var fbfv = newItem.GetFieldValue(FieldNames.FacebookID);
            if (fbfv != null && fbfv.Value != null)
            {
                // the old category must not exist or the value must have changed
                var oldfbfv = oldItem.GetFieldValue(FieldNames.FacebookID);
                if (oldfbfv == null || oldfbfv.Value != fbfv.Value)
                {
                    FBGraphAPI fbApi = new FBGraphAPI();
                    User userWithCreds = storage.GetUser(user.ID, true);
                    UserCredential cred = userWithCreds.GetCredential(UserCredential.FacebookConsent);
                    if (cred != null && cred.AccessToken != null)
                    {
                        fbApi.AccessToken = cred.AccessToken;
                    }
                    else
                    {
                        TraceLog.TraceError(FacebookHelper.TRACE_NO_FB_TOKEN);
                        return false;
                    }

                    // get or create an EntityRef in the UserFolder EntityRefs list
                    var entityRefItem = storage.UserFolder.GetEntityRef(user, newItem);
                    if (entityRefItem == null)
                    {
                        TraceLog.TraceError(FacebookHelper.TRACE_NO_CONTACT_ENTITYREF);
                        return false;
                    }

                    // get the contact's profile information from facebook
                    try
                    {
                        // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
                        foreach (var contact in fbApi.Query(fbfv.Value, FBQueries.BasicInformation))
                        {
                            newItem.GetFieldValue(FieldNames.Picture, true).Value = String.Format("https://graph.facebook.com/{0}/picture", fbfv.Value);
                            var birthday = (string)contact[FBQueryResult.Birthday];
                            if (birthday != null)
                                newItem.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
                            var gender = (string)contact[FBQueryResult.Gender];
                            if (gender != null)
                                entityRefItem.GetFieldValue(FieldNames.Gender, true).Value = gender;
                        }
                        //userContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException(FacebookHelper.TRACE_NO_SAVE_FBCONTACTINFO, ex);
                    }

                    return true;
                }
            }
            return false;
        }    
    }

}
