using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class PossibleContactProcessor
    {
        /// <summary>
        /// Add a Contat to the possible subjects list
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="item"></param>
        /// <returns>false if errors were encountered, otherwise true</returns>
        public static bool AddContact(UserStorageContext userContext, Item item)
        {
            User user = userContext.CurrentUser(item);
            Item possibleSubjectList = userContext.GetOrCreatePossibleSubjectsList(user);
            if (possibleSubjectList == null)
            {
                TraceLog.TraceError("PossibleContactProcessor.AddContact: could not retrieve or create the possible subjects list");
                return false;
            }

            DateTime now = DateTime.UtcNow;

            try
            {
                // determine if a possible subject by this name already exists, and if so, skip adding it
                var nameValItem = userContext.Items.Include("FieldValues").FirstOrDefault(
                    ps => ps.UserID == user.ID && ps.FolderID == possibleSubjectList.FolderID &&
                    ps.Name == item.Name && ps.ParentID == possibleSubjectList.ID && ps.ItemTypeID == SystemItemTypes.NameValue);

                if (nameValItem == null)
                {
                    // store the serialized contact in the value of a new NameValue item on the PossibleSubjects list
                    string jsonContact = JsonSerializer.Serialize(item);

                    Guid id = Guid.NewGuid();
                    nameValItem = new Item()
                    {
                        ID = id,
                        Name = item.Name,
                        FolderID = possibleSubjectList.FolderID,
                        ParentID = possibleSubjectList.ID,
                        UserID = user.ID,
                        ItemTypeID = SystemItemTypes.NameValue,
                        Created = now,
                        LastModified = now,
                        FieldValues = new List<FieldValue>() { new FieldValue() { FieldName = FieldNames.Value, ItemID = id, Value = jsonContact } }
                    };

                    // add the new possible subject to the DB
                    userContext.Items.Add(nameValItem);
                }

                // store a reference fieldvalue on the namevalue item to designate that this possible subject relates to an existing Contact
                nameValItem.GetFieldValue(FieldNames.EntityRef, true).Value = item.ID.ToString();
                nameValItem.GetFieldValue(FieldNames.EntityType, true).Value = EntityTypes.Item;

                // store the updated possible subject in the DB
                userContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("PossibleContactProcessor.AddContact: could not create a new PossibleSubject", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove a Contact from the possible subjects list
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool RemoveContact(UserStorageContext userContext, Item item)
        {
            // clean up any possible subjects that have this ID
            User user = userContext.CurrentUser(item);
            Item possibleSubjectList = userContext.GetOrCreatePossibleSubjectsList(user);
            if (possibleSubjectList == null)
                return false;

            try
            {
                // find the item in the possible subjects list that corresponds to this contact
                var idstring = item.ID.ToString();
                var possibleSubject = userContext.Items.Include("FieldValues").FirstOrDefault(ps => ps.UserID == user.ID && ps.FolderID == possibleSubjectList.FolderID &&
                    ps.ParentID == possibleSubjectList.ID && ps.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == idstring));
                if (possibleSubject == null)
                    return true;

                // if this possible subject didn't come from facebook, remove it
                if (possibleSubject.GetFieldValue(FieldNames.FacebookID) == null)
                {
                    userContext.Items.Remove(possibleSubject);
                    return true;
                }

                // leave the possible subject, but remove the fieldvalues on this possible subject that relate it to the deleted contact
                var fieldVal = possibleSubject.GetFieldValue(FieldNames.EntityRef);
                if (fieldVal != null)
                    userContext.FieldValues.Remove(fieldVal);
                fieldVal = possibleSubject.GetFieldValue(FieldNames.EntityType);
                if (fieldVal != null)
                    userContext.FieldValues.Remove(fieldVal);
                userContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessDelete: could not delete the PossibleSubject " + item.Name, ex);
                return false;
            }
        }
    }
}
