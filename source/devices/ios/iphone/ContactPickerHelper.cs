using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MonoTouch.AddressBook;
using Xamarin.Contacts;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
    public class ContactPickerHelper
    {
        const string ZaplifyContactHeader = "zaplify";

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
        public static Item ProcessContact(ABPerson selectedPerson)
        {
            var contact = GetExistingContact(selectedPerson);
            if (contact == null)
                contact = CreateNewContact(selectedPerson);

            // add the contact info from the phone address book to the new contact
            AddContactInfo(selectedPerson, contact);

            return contact;
        }

        private static void AddContactInfo(ABPerson selectedPerson, Item item)
        {
            // find the contact in the address book
            var book = new AddressBook(); 
            var contact = book.FirstOrDefault(c => c.Id == selectedPerson.Id.ToString());

            if (contact == null)
                return;

            // make a copy of the item
            var itemCopy = new Item(item, true);

            // get more info from the address book
            var mobile = (from p in contact.Phones where 
                p.Type == Xamarin.Contacts.PhoneType.Mobile
                select p.Number).FirstOrDefault();
            var home = (from p in contact.Phones where 
                p.Type == Xamarin.Contacts.PhoneType.Home
                select p.Number).FirstOrDefault();
            var work = (from p in contact.Phones where 
                p.Type == Xamarin.Contacts.PhoneType.Work
                select p.Number).FirstOrDefault();
            var email = (from em in contact.Emails  
                select em.Address).FirstOrDefault();
            //var website = (from w in contact.Websites  
            //    select w.Address).FirstOrDefault();

            string birthday = null;
            if (selectedPerson.Birthday != null)
                birthday = ((DateTime)selectedPerson.Birthday).ToString("d");    

            if (birthday != null)
                item.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
            if (mobile != null)
                item.GetFieldValue(FieldNames.Phone, true).Value = mobile;
            if (home != null)
                item.GetFieldValue(FieldNames.HomePhone, true).Value = home;
            if (work != null)
                item.GetFieldValue(FieldNames.WorkPhone, true).Value = work;
            if (email != null)
                item.GetFieldValue(FieldNames.Email, true).Value = email;
            /*
            if (website != null)
                item.GetFieldValue(FieldNames.Website, true).Value = website
                */

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

            // add the zaplify contact header to a special Zaplify "related name" field 
            // use the native address book because the cross-platform AddressBook class is read-only
            var ab = new ABAddressBook();
            var contactToModify = ab.GetPerson(selectedPerson.Id);
            var relatedNames = contactToModify.GetRelatedNames().ToMutableMultiValue();
            if (relatedNames.Any(name => name.Label == ZaplifyContactHeader))
            {
                // remove the existing one (can't figure out a way to get a mutable ABMultiValueEntry out of zapField)
                var zapField = relatedNames.Single(name => name.Label == ZaplifyContactHeader);
                relatedNames.RemoveAt(relatedNames.GetIndexForIdentifier(zapField.Identifier));
            }
            // add the Zaplify related name field with the itemID value
            relatedNames.Add(item.ID.ToString(), new MonoTouch.Foundation.NSString(ZaplifyContactHeader));
            contactToModify.SetRelatedNames(relatedNames);

            // save changes to the address book
            ab.Save();
        }

        private static Item CreateNewContact(ABPerson selectedPerson)
        {
            Guid id = Guid.NewGuid();
            // get the default list for contacts
            Guid folderID;
            Guid? parentID = null;
            ClientEntity defaultList = App.ViewModel.GetDefaultList(SystemItemTypes.Contact);
            if (defaultList == null)
            {
                TraceHelper.AddMessage("CreateNewContact: error - could not find default contact list");
                return null;
            }
            if (defaultList is Item)
            {
                folderID = ((Item)defaultList).FolderID;
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

        private static Item GetExistingContact(ABPerson selectedPerson)
        {
            var list = selectedPerson.GetRelatedNames().ToList();
            if (list.Any(name => name.Label == ZaplifyContactHeader))
            {
                var zapField = list.Single(name => name.Label == ZaplifyContactHeader);
                Guid id = new Guid(zapField.Value);
                return App.ViewModel.Items.FirstOrDefault(i => i.ID == id);
            }
            return null;
        }
    }
}

