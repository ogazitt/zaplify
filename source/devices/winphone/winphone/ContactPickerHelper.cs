using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;
using Microsoft.Phone.UserData;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public class ContactPickerHelper
    {
        const string ZaplifyContactHeader = "zaplify:";

        /// <summary>
        /// Processes the contact - creates a new contact if one doesn't exist, or refreshes the contact
        /// if it's already in the database
        /// </summary>
        /// <returns>
        /// A new ItemRef Item (already added to the Folder and queued up to the server) which points to the new or existing Contact Item
        /// </returns>
        /// <param name='selectedPerson'>
        /// Selected person from the people picker
        /// </param>
        public static Item ProcessContact(Contact selectedPerson, Item list = null)
        {
            var contact = GetExistingContact(selectedPerson);
            if (contact == null)
                contact = CreateNewContact(selectedPerson, list);

            // add the contact info from the phone address book to the new contact
            AddContactInfo(selectedPerson, contact);

            return contact;
        }

        public static void AddContactInfo(Contact contact, Item item)
        {
            if (contact == null)
                return;

            // make a copy of the item
            var itemCopy = new Item(item, true);

            // get more info from the address book
            var mobile = (from p in contact.PhoneNumbers where 
                p.Kind == PhoneNumberKind.Mobile
                select p.PhoneNumber).FirstOrDefault();
            var home = (from p in contact.PhoneNumbers where
                p.Kind == PhoneNumberKind.Home
                select p.PhoneNumber).FirstOrDefault();
            var work = (from p in contact.PhoneNumbers where
                p.Kind == PhoneNumberKind.Work
                select p.PhoneNumber).FirstOrDefault();
            var email = (from em in contact.EmailAddresses  
                select em.EmailAddress).FirstOrDefault();
            var website = (from w in contact.Websites  
                select w).FirstOrDefault();
            var birthday = (from b in contact.Birthdays
                            select b).FirstOrDefault();
            string FacebookPrefix = "fb://profile/";
            var facebook = (from w in contact.Websites
                            where w.Contains(FacebookPrefix)
                            select w).FirstOrDefault();
            var fbid = !String.IsNullOrWhiteSpace(facebook) ? facebook.Substring(facebook.IndexOf(FacebookPrefix) + FacebookPrefix.Length) : null;

            if (birthday != null && birthday.Ticks != 0)
                item.GetFieldValue(FieldNames.Birthday, true).Value = birthday.ToString("d");
            if (mobile != null)
                item.GetFieldValue(FieldNames.Phone, true).Value = mobile;
            if (home != null)
                item.GetFieldValue(FieldNames.HomePhone, true).Value = home;
            if (work != null)
                item.GetFieldValue(FieldNames.WorkPhone, true).Value = work;
            if (email != null)
                item.GetFieldValue(FieldNames.Email, true).Value = email;
            //if (website != null)
            //    item.GetFieldValue(FieldNames.Website, true).Value = website;
            if (fbid != null)
            {
                item.GetFieldValue(FieldNames.FacebookID, true).Value = fbid;
                var sourcesFV = item.GetFieldValue(FieldNames.Sources, true);
                if (!String.IsNullOrEmpty(sourcesFV.Value))
                {
                    if (!sourcesFV.Value.Contains(Sources.Facebook))
                        sourcesFV.Value = String.Format("{0},{1}", sourcesFV.Value, Sources.Facebook);
                }
                else
                    sourcesFV.Value = Sources.Facebook;
            }

            // save changes to local storage
            Folder folder = App.ViewModel.LoadFolder(item.FolderID);
            StorageHelper.WriteFolder(folder);

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { itemCopy, item },
                    BodyTypeName = "Item",
                    ID = item.ID
                });

            // TODO: add the zaplify contact header to a special Zaplify Note field 
            // can't find a way to do this currently :-(
        }

        private static Item CreateNewContact(Contact selectedPerson, Item list = null)
        {
            Guid id = Guid.NewGuid();
            // get the default list for contacts
            Guid folderID;
            Guid? parentID = null;

            ClientEntity defaultList = list ??  App.ViewModel.GetDefaultList(SystemItemTypes.Contact);
            if (defaultList == null)
            {
                TraceHelper.AddMessage("CreateNewContact: error - could not find default contact list");
                return null;
            }
            if (defaultList is Item)
            {
                folderID = ((Item)defaultList).FolderID;
                if (defaultList.ID != Guid.Empty)
                    parentID = defaultList.ID;
            }
            else
            {
                folderID = defaultList.ID;
                parentID = null;
            }
    
            Item newContact = new Item()
            {
                ID = id,
                Name = selectedPerson.ToString(),
                FolderID = folderID,
                ParentID = parentID,
                ItemTypeID = SystemItemTypes.Contact,
                FieldValues = new ObservableCollection<FieldValue>()
            };

            // add the new contact locally
            Folder folder = App.ViewModel.Folders.FirstOrDefault(f => f.ID == folderID);
            if (folder == null)
            {
                TraceHelper.AddMessage("CreateNewContact: error - could not find the folder for this item");
                return null;
            }
            folder.Items.Add(newContact);

            // save the current state of the folder
            StorageHelper.WriteFolder(folder);

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = newContact,
                });

            return newContact;
        }

        private static Item GetExistingContact(Contact selectedPerson)
        {
            if (selectedPerson == null || selectedPerson.Notes == null)
                return null;
            if (selectedPerson.Notes.Any(name => name.Contains(ZaplifyContactHeader)))
            {
                var zapField = selectedPerson.Notes.Single(name => name.Contains(ZaplifyContactHeader));
                var index = zapField.IndexOf(ZaplifyContactHeader);
                var idstring = zapField.Substring(index + ZaplifyContactHeader.Length, Guid.Empty.ToString().Length);
                Guid id = new Guid(idstring);
                return App.ViewModel.Items.FirstOrDefault(i => i.ID == id);
            }
            return null;
        }
    }
}

