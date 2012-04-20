using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.ServiceHost;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
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

                    Folder userFolder = UserContext.GetOrCreateUserFolder(user);
                    Item possibleSubjectList = null;
                    DateTime now = DateTime.UtcNow;

                    try
                    {
                        // issue the query for user data against the Facebook Graph API
                        //var results = fbApi.Query("me", FBQueries.BasicInformation);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("ImportFromFacebook: Error calling Facebook Graph API", ex);
                        return Status.Error;
                    }

                    // retrieve the PossibleSubjects list inside the $User folder
                    try
                    {
                        // get the PossibleSubjects list
                        if (UserContext.Items.Any(i => i.UserID == user.ID && i.FolderID == userFolder.ID && i.Name == SystemEntities.PossibleSubjects))
                            possibleSubjectList = UserContext.Items.Single(i => i.UserID == user.ID && i.FolderID == userFolder.ID && i.Name == SystemEntities.PossibleSubjects);
                        else
                        {
                            // create PossibleSubjects list
                            possibleSubjectList = new Item()
                            {
                                ID = Guid.NewGuid(),
                                Name = SystemEntities.PossibleSubjects,
                                FolderID = userFolder.ID,
                                UserID = user.ID,
                                IsList = true,
                                ItemTypeID = SystemItemTypes.NameValue,
                                ParentID = null,
                                Created = now,
                                LastModified = now
                            };
                            UserContext.Items.Add(possibleSubjectList);
                            UserContext.SaveChanges();
                            TraceLog.TraceInfo("ImportFromFacebook: created PossibleSubjects list for user " + user.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("ImportFromFacebook: could not find or create PossibleSubjects list", ex);
                        return Status.Error;
                    }

                    // get the current list of all possible subjects for this user ($User.PossibleSubjects)
                    var currentPossibleSubjects = UserContext.Items.Include("FieldValues").Where(ps => ps.UserID == user.ID && ps.FolderID == userFolder.ID &&
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
                            if (currentPossibleSubjects.Any(ps => ps.Name == friend[FBQueryResult.Name] &&
                                    ps.FieldValues.Any(fv => fv.FieldName == FieldNames.FacebookID && fv.Value == friend[FBQueryResult.ID])))
                                continue;

                            bool process = true;
                            
                            // check if a contact by this name already exists
                            var existingContacts = currentContacts.Where(c => c.Name == friend[FBQueryResult.Name]).ToList();
                            foreach (var existingContact in existingContacts)
                            {
                                var fbFV = GetFieldValue(existingContact, FieldNames.FacebookID, true);
                                if (fbFV.Value == null)
                                {
                                    // contact by this name exists but facebook ID isn't set; assume this is a duplicate and set the FBID
                                    fbFV.Value = friend[FBQueryResult.ID];
                                    process = false;
                                    break;
                                }
                                if (fbFV.Value == friend[FBQueryResult.ID])
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
                                    Name = friend[FBQueryResult.Name],
                                    ParentID = null,
                                    UserID = user.ID,
                                    FolderID = folder.ID,
                                    ItemTypeID = SystemItemTypes.Contact,
                                    SortOrder = sort++,
                                    Created = now,
                                    LastModified = now,
                                    FieldValues = new List<FieldValue>(),
                                };
                                contact.FieldValues.Add(new FieldValue() { ItemID = contact.ID, FieldName = FieldNames.FacebookID, Value = friend[FBQueryResult.ID] });
                                string jsonContact = JsonSerializer.Serialize(contact);

                                // store the serialized contact in the value of a new NameValue item on the PossibleSubjects list
                                var nameValItem = new Item()
                                {
                                    ID = Guid.NewGuid(),
                                    Name = friend[FBQueryResult.Name],
                                    FolderID = userFolder.ID,
                                    ParentID = possibleSubjectList.ID,
                                    UserID = user.ID,
                                    ItemTypeID = SystemItemTypes.NameValue,
                                    Created = now,
                                    LastModified = now,
                                    FieldValues = new List<FieldValue>()
                                };
                                nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.Value, ItemID = nameValItem.ID, Value = jsonContact });
                                // also add the FBID as a fieldvalue on the namevalue item which corresponds to the possible subject, for easier dup handling
                                nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.FacebookID, ItemID = nameValItem.ID, Value = friend[FBQueryResult.ID] });

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
