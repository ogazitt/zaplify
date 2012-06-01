using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost.Helpers
{
    public class PossibleContactHelper
    {
        /// <summary>
        /// Add a Contact to the possible contacts list
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="item"></param>
        /// <returns>false if errors were encountered, otherwise true</returns>
        public static bool AddContact(UserStorageContext userContext, Item item)
        {
            User user = userContext.CurrentUser(item);
            Item possibleContactsList = userContext.GetOrCreateUserItemTypeList(user, SystemItemTypes.Contact);
            if (possibleContactsList == null)
            {
                TraceLog.TraceError("PossibleContactProcessor.AddContact: could not retrieve or create the possible contacts list");
                return false;
            }

            try
            {
                // determine if a possible contact by this name already exists, and if so, skip adding it
                var nameValItem = userContext.Items.Include("FieldValues").FirstOrDefault(
                    ps => ps.UserID == user.ID && ps.FolderID == possibleContactsList.FolderID &&
                    ps.Name == item.Name && ps.ParentID == possibleContactsList.ID && ps.ItemTypeID == SystemItemTypes.NameValue);

                if (nameValItem == null)
                {
                    // store the serialized contact in the value of a new NameValue item on the PossibleContacts list
                    string jsonContact = JsonSerializer.Serialize(item);

                    Guid id = Guid.NewGuid();
                    DateTime now = DateTime.UtcNow;
                    nameValItem = new Item()
                    {
                        ID = id,
                        Name = item.Name,
                        FolderID = possibleContactsList.FolderID,
                        ParentID = possibleContactsList.ID,
                        UserID = user.ID,
                        ItemTypeID = SystemItemTypes.NameValue,
                        Created = now,
                        LastModified = now,
                        FieldValues = new List<FieldValue>() { new FieldValue() { FieldName = FieldNames.Value, ItemID = id, Value = jsonContact } }
                    };

                    // add the new possible contact to the DB
                    userContext.Items.Add(nameValItem);
                }

                // store a reference fieldvalue on the namevalue item to designate that this possible contact relates to an existing Contact
                nameValItem.GetFieldValue(FieldNames.EntityRef, true).Value = item.ID.ToString();
                nameValItem.GetFieldValue(FieldNames.EntityType, true).Value = EntityTypes.Item;

                // store the updated possible contact in the DB
                userContext.SaveChanges();
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("PossibleContactProcessor.AddContact: could not create a new PossibleContact", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove a Contact from the possible contacts list
        /// </summary>
        /// <param name="userContext"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool RemoveContact(UserStorageContext userContext, Item item)
        {
            // clean up any possible contacts that have this ID
            User user = userContext.CurrentUser(item);
            Item possibleContactsList = userContext.GetOrCreateUserItemTypeList(user, SystemItemTypes.Contact);
            if (possibleContactsList == null)
                return false;

            try
            {
                // find the item in the possible contacts list that corresponds to this contact
                var idstring = item.ID.ToString();
                var possibleContact = userContext.Items.Include("FieldValues").FirstOrDefault(ps => ps.UserID == user.ID && ps.FolderID == possibleContactsList.FolderID &&
                    ps.ParentID == possibleContactsList.ID && ps.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == idstring));
                if (possibleContact == null)
                    return true;

                // if this possible contact didn't come from facebook, remove it
                if (possibleContact.GetFieldValue(FieldNames.FacebookID) == null)
                {
                    userContext.Items.Remove(possibleContact);
                    return true;
                }

                // leave the possible contact, but remove the fieldvalues on this possible contact that relate it to the deleted contact
                var fieldVal = possibleContact.GetFieldValue(FieldNames.EntityRef);
                if (fieldVal != null)
                    userContext.FieldValues.Remove(fieldVal);
                fieldVal = possibleContact.GetFieldValue(FieldNames.EntityType);
                if (fieldVal != null)
                    userContext.FieldValues.Remove(fieldVal);
                userContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessDelete: could not delete the PossibleContact " + item.Name, ex);
                return false;
            }
        }
    }
}
