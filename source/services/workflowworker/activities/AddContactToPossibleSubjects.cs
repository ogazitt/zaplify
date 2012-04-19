﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class AddContactToPossibleSubjects : WorkflowActivity
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
                        TraceLog.TraceError("AddContactToPossibleSubject: non-Item passed in");
                        return Status.Error;
                    }

                    User user = CurrentUser(item);
                    Item possibleSubjectList = null;
                    Folder userFolder = UserContext.GetOrCreateUserFolder(user);
                    DateTime now = DateTime.UtcNow;

                    try
                    {
                        // get the PossibleSubjects list
                        if (UserContext.Items.Any(i => i.UserID == user.ID && i.FolderID == userFolder.ID && i.Name == SystemFolders.PossibleSubjects))
                            possibleSubjectList = UserContext.Items.Single(i => i.UserID == user.ID && i.FolderID == userFolder.ID && i.Name == SystemFolders.PossibleSubjects);
                        else
                        {
                            // create PossibleSubjects list
                            possibleSubjectList = new Item()
                            {
                                ID = Guid.NewGuid(),
                                Name = SystemFolders.PossibleSubjects,
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
                        TraceLog.TraceException("AddContactToPossibleSubjects: could not find or create PossibleSubjects list", ex);
                        return Status.Error;
                    }

                    try
                    {
                        // determine if a possible subject by this name already exists, and if so, skip adding it
                        if (UserContext.Items.Include("FieldValues").Any(ps => ps.UserID == user.ID && ps.FolderID == userFolder.ID &&
                            ps.Name == item.Name && ps.ParentID == possibleSubjectList.ID && ps.ItemTypeID == SystemItemTypes.NameValue))
                        {
                            TraceLog.TraceInfo("AddContactToPossibleSubjects: contact by this name already exists in the possible subjects list");
                            return Status.Complete;
                        }

                        // create a reference item to the new contact (but do not attach it to the DB)
                        var contactRef = new Item()
                        {
                            ID = Guid.NewGuid(),
                            Name = item.Name,
                            FolderID = item.FolderID,
                            UserID = user.ID,
                            ItemTypeID = SystemItemTypes.Reference,
                            Created = now,
                            LastModified = now,
                            FieldValues = new List<FieldValue>()
                        };
                        contactRef.FieldValues.Add(new FieldValue() { FieldName = FieldNames.ItemRef, ItemID = contactRef.ID, Value = item.ID.ToString() });
                        string jsonContact = JsonSerializer.Serialize(contactRef);

                        // store the serialized contact in the value of a new NameValue item on the PossibleSubjects list
                        var nameValItem = new Item()
                        {
                            ID = Guid.NewGuid(),
                            Name = item.Name,
                            FolderID = userFolder.ID,
                            ParentID = possibleSubjectList.ID,
                            UserID = user.ID,
                            ItemTypeID = SystemItemTypes.NameValue,
                            Created = now,
                            LastModified = now,
                            FieldValues = new List<FieldValue>()
                        };
                        nameValItem.FieldValues.Add(new FieldValue() { FieldName = FieldNames.Value, ItemID = nameValItem.ID, Value = jsonContact });

                        // store this new possible subject in the DB
                        UserContext.Items.Add(nameValItem);
                        UserContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("AddContactToPossibleSubjects: could not create a new PossibleSubject", ex);
                        return Status.Error;
                    }

                    return Status.Complete;
                });
            }
        }
    }
}