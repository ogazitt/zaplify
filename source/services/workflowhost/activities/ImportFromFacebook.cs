using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.ServiceHost;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class ImportFromFacebook : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Folder folder = entity as Folder;
                    if (folder == null)
                    {
                        TraceLog.TraceError("ImportFromFacebook: non-Folder passed in");
                        return Status.Error;
                    }

                    User user = null;
                    if (UserContext.Users.Any(u => u.ID == folder.UserID))
                        user = UserContext.Users.Include("UserCredentials").Single(u => u.ID == folder.UserID);
                    else
                    {
                        TraceLog.TraceError("ImportFromFacebook: User not found");
                        return Status.Error;
                    }

                    // set up the FB API context
                    FBGraphAPI fbApi = new FBGraphAPI();

                    try
                    {
                        UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                        fbApi.AccessToken = cred.FBConsentToken;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("ImportFromFacebook: could not find Facebook credential or consent token", ex);
                        return Status.Error;
                    }

                    // get or create a entity ref item for the user in the $User folder
                    var entityRefItem = UserContext.GetOrCreateEntityRef(user, user);
                    if (entityRefItem == null)
                    {
                        TraceLog.TraceError("ImportFromFacebook: could not retrieve or create a entity ref item for this user");
                        return Status.Error;
                    }

                    // get or create the possible subjects list in the $User folder
                    Item possibleSubjectList = UserContext.GetOrCreatePossibleSubjectsList(user);
                    if (possibleSubjectList == null)
                    {
                        TraceLog.TraceError("ImportFromFacebook: could not retrieve or create the possible subjects list");
                        return Status.Error;
                    }

                    // import information about the current user
                    try
                    {
                        // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
                        foreach (var userInfo in fbApi.Query("me", FBQueries.BasicInformation))
                        {
                            // store the facebook ID
                            var fbid = (string) userInfo[FBQueryResult.ID];
                            entityRefItem.GetFieldValue(FieldNames.FacebookID, true).Value = fbid;
                            // augment the sources field with Facebook as a source
                            var sourcesFV = entityRefItem.GetFieldValue(FieldNames.Sources, true);
                            sourcesFV.Value = String.IsNullOrEmpty(sourcesFV.Value) ? 
                                Sources.Facebook : 
                                sourcesFV.Value.Contains(Sources.Facebook) ? 
                                    sourcesFV.Value : 
                                    String.Format("{0}:{1}", sourcesFV.Value, Sources.Facebook);
                            // store the picture URL
                            entityRefItem.GetFieldValue(FieldNames.Picture, true).Value = String.Format("https://graph.facebook.com/{0}/picture", fbid);
                            // augment with birthday and gender information if they don't yet exist
                            var birthday = (string) userInfo[FBQueryResult.Birthday];
                            if (birthday != null)
                                entityRefItem.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
                            var gender = (string) userInfo[FBQueryResult.Gender];
                            if (gender != null)
                                entityRefItem.GetFieldValue(FieldNames.Gender, true).Value = gender;
                            var location = (string) ((FBQueryResult)userInfo[FBQueryResult.Location])[FBQueryResult.Name];
                            if (location != null)
                                entityRefItem.GetFieldValue(FieldNames.Location, true).Value = location;
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("ImportFromFacebook: Facebook query for user's basic information failed", ex);
                        return Status.Error;
                    }

                    DateTime now = DateTime.UtcNow;

                    // get the current list of all possible subjects for this user ($User.PossibleSubjects)
                    var currentPossibleSubjects = UserContext.Items.Include("FieldValues").Where(ps => ps.UserID == user.ID && ps.FolderID == possibleSubjectList.FolderID &&
                        ps.ParentID == possibleSubjectList.ID && ps.ItemTypeID == SystemItemTypes.NameValue &&
                        ps.FieldValues.Any(fv => fv.FieldName == FieldNames.FacebookID)).ToList();

                    // get the current list of all Items that are Contacts for this user
                    var currentContacts = UserContext.Items.Include("FieldValues").
                                Where(c => c.UserID == user.ID && c.ItemTypeID == SystemItemTypes.Contact).ToList();

                    // get all the user's friends and add them as serialized contacts to the $User.PossibleSubjects list
                    float sort = 1f;
                    try
                    {
                        var results = fbApi.Query("me", FBQueries.Friends).ToList();
                        TraceLog.TraceInfo(String.Format("ImportFromFacebook: found {0} Facebook friends", results.Count));
                        foreach (var friend in results)
                        {
                            // check if a possible subject by this name and with this FBID already exists - and if so, skip it
                            if (currentPossibleSubjects.Any(ps => ps.Name == (string) friend[FBQueryResult.Name] &&
                                    ps.FieldValues.Any(fv => fv.FieldName == FieldNames.FacebookID && fv.Value == (string) friend[FBQueryResult.ID])))
                                continue;

                            bool process = true;
                            
                            // check if a contact by this name already exists
                            var existingContacts = currentContacts.Where(c => c.Name == (string) friend[FBQueryResult.Name]).ToList();
                            foreach (var existingContact in existingContacts)
                            {
                                var fbFV = existingContact.GetFieldValue(FieldNames.FacebookID, true);
                                if (fbFV.Value == null)
                                {
                                    // contact by this name exists but facebook ID isn't set; assume this is a duplicate and set the FBID
                                    fbFV.Value = (string) friend[FBQueryResult.ID];
                                    var sourcesFV = existingContact.GetFieldValue(FieldNames.Sources, true);
                                    sourcesFV.Value = string.IsNullOrEmpty(sourcesFV.Value) ? Sources.Facebook : string.Concat(sourcesFV.Value, ",", Sources.Facebook);
                                    process = false;
                                    break;
                                }
                                if (fbFV.Value == (string) friend[FBQueryResult.ID])
                                {
                                    // this is definitely a duplicate - don't add it
                                    process = false;
                                    break;
                                }
                                // getting here means that a contact by this name was found but had a different FBID - so this new subject is unique
                            }

                            // add the contact if it wasn't detected as a duplicate
                            if (process)
                            {
                                var contact = new Item()
                                {
                                    ID = Guid.NewGuid(),
                                    Name = (string) friend[FBQueryResult.Name],
                                    ParentID = null,
                                    UserID = user.ID,
                                    FolderID = folder.ID,
                                    ItemTypeID = SystemItemTypes.Contact,
                                    SortOrder = sort++,
                                    Created = now,
                                    LastModified = now,
                                    FieldValues = new List<FieldValue>(),
                                };
                                contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.FacebookID, Value = (string) friend[FBQueryResult.ID] });
                                contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.Sources, Value = Sources.Facebook });
                                string jsonContact = JsonSerializer.Serialize(contact);

                                // store the serialized contact in the value of a new NameValue item on the PossibleSubjects list
                                var nameValItem = new Item()
                                {
                                    ID = Guid.NewGuid(),
                                    Name = (string) friend[FBQueryResult.Name],
                                    FolderID = possibleSubjectList.FolderID,
                                    ParentID = possibleSubjectList.ID,
                                    UserID = user.ID,
                                    ItemTypeID = SystemItemTypes.NameValue,
                                    Created = now,
                                    LastModified = now,
                                    FieldValues = new List<FieldValue>()
                                };
                                nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.Value, ItemID = nameValItem.ID, Value = jsonContact });
                                // also add the FBID as a fieldvalue on the namevalue item which corresponds to the possible subject, for easier dup handling
                                nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.FacebookID, ItemID = nameValItem.ID, Value = (string) friend[FBQueryResult.ID] });

                                // add this new possible subject to the DB and to the working list of possible subjects
                                UserContext.Items.Add(nameValItem);
                                currentPossibleSubjects.Add(nameValItem);
                            }
                        }

                        UserContext.SaveChanges();
                        TraceLog.TraceInfo(String.Format("ImportFromFacebook: added {0} possible subjects to $User.PossibleSubjects", results.Count));
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("ImportFromFacebook: could not retrieve or create a new PossibleSubject", ex);
                        return Status.Error;
                    }

                    return Status.Complete;
                });
            }
        }
    }
}
