using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost.Helpers;
using BuiltSteady.Zaplify.ServiceHost.Nlp;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.ServiceUtilities.Grocery;
using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class AppointmentProcessor : ItemProcessor
    {
        public AppointmentProcessor(User user, UserStorageContext storage)
        {
            this.user = user;
            this.storage = storage;
        }

        public override bool ProcessCreate(Item item)
        {
            // base method extracts intent
            if (base.ProcessCreate(item))
                return true;

            // add new appointment to Calendar (if available)
            GoogleClient client = new GoogleClient(user, storage);
            if (client.ConnectToCalendar)
            {
                return client.AddCalendarEvent(item);
            }
            return false;
        }

        public override bool ProcessDelete(Item item)
        {
            if (base.ProcessDelete(item))
                return true;

            // delete appointment from Calendar (if available)
            GoogleClient client = new GoogleClient(user, storage);
            if (client.ConnectToCalendar)
            {
                return client.RemoveCalendarEvent(item);
            }
            return false;
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            GoogleClient client = new GoogleClient(user, storage);
            if (newItem.ItemTypeID != oldItem.ItemTypeID)
            {   // remove appointment from Calendar if ItemType changes
                ProcessDelete(oldItem);
                // base handles ItemType changing
                return base.ProcessUpdate(oldItem, newItem);
            }

            // update appointment on Calendar (if available)
            if (client.ConnectToCalendar)
            {   // update if Name, DueDate, EndDate, or Description have changed
                FieldValue fvOldStart = oldItem.GetFieldValue(FieldNames.DueDate);
                FieldValue fvNewStart = newItem.GetFieldValue(FieldNames.DueDate);
                FieldValue fvOldEnd = oldItem.GetFieldValue(FieldNames.EndDate);
                FieldValue fvNewEnd = newItem.GetFieldValue(FieldNames.EndDate);
                FieldValue fvOldDesc = oldItem.GetFieldValue(FieldNames.Description);
                FieldValue fvNewDesc = newItem.GetFieldValue(FieldNames.Description);
                string oldDesc = (fvOldDesc == null) ? null : fvOldDesc.Value;
                string newDesc = (fvNewDesc == null) ? null : fvNewDesc.Value;
                if (newItem.Name != oldItem.Name || newDesc != oldDesc ||
                    (fvNewStart != null && !string.IsNullOrEmpty(fvNewStart.Value) && (fvOldStart == null || fvNewStart.Value != fvOldStart.Value)) ||
                    (fvNewEnd != null && !string.IsNullOrEmpty(fvNewEnd.Value) && (fvOldEnd == null || fvNewEnd.Value != fvOldEnd.Value)))
                {
                    if (fvNewStart != null && fvNewEnd != null)
                    {
                        return client.UpdateCalendarEvent(newItem);
                    }
                }
            }

            return false;
        }

        // TODO: Do we want to use NLP to ExtractIntent for Appointments?
    }

}
