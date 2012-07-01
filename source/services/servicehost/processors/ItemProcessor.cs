using System;

using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public abstract class ItemProcessor
    {
        protected UserStorageContext storage;
        protected User user;

        // Factory method to create a new item processor based on the item type
        public static ItemProcessor Create(User user, UserStorageContext storage, Guid itemTypeID)
        {
            if (itemTypeID == SystemItemTypes.Task)
                return new TaskProcessor(user, storage);
            if (itemTypeID == SystemItemTypes.Appointment)
                return new AppointmentProcessor(user, storage);
            if (itemTypeID == SystemItemTypes.Grocery)
                return new GroceryProcessor(user, storage);
            if (itemTypeID == SystemItemTypes.Contact)
                return new ContactProcessor(user, storage);
            return null;
        }

        // Process a new item that is being created  
        // Extracts the intent based on ItemType and extends as FieldValue on Item
        // return true to indicate to sub-classes that processing is complete
        public virtual bool ProcessCreate(Item item)
        {
            var intent = ExtractIntent(item);
            if (intent != null)
                CreateIntentFieldValue(item, intent);
            return false;
        }

        // Process an item that is being deleted.  
        // Default implementation does nothing.
        // return true to indicate to sub-classes that processing is complete
        public virtual bool ProcessDelete(Item item)
        {
            return false;
        }

        // Process an item being updated.  
        public virtual bool ProcessUpdate(Item oldItem, Item newItem)
        {
            if (newItem.ItemTypeID != oldItem.ItemTypeID)
            {   // ItemType changed, create correct Processor for newItem and ProcessCreate
                ItemProcessor ip = ItemProcessor.Create(user, storage, newItem.ItemTypeID);
                ip.ProcessCreate(newItem);
                return true;
            }        
            return false;
        }

        // Create and add an Intent FieldValue to the item
        protected void CreateIntentFieldValue(Item item, string intent)
        {
            item.GetFieldValue(ExtendedFieldNames.Intent, true).Value = intent;
            TraceLog.TraceDetail(String.Format("Assigned {0} intent to item {1}", intent, item.Name));
        }

        // The Intent of an Item is a normalized string that used to infer meaning for the item
        // Intent is may be determined differently based on ItemType
        // For example, simple NLP is used to determine Intent for a Task to help select a Workflow 
        // Default implementation for the Intent of an item is to lowercase the item name.
        protected virtual string ExtractIntent(Item item)
        {
            return item.Name.ToLower();
        }
    }

}
