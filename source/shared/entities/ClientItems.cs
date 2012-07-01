using System;
#if CLIENT
using BuiltSteady.Zaplify.Devices.ClientEntities;
#else
using BuiltSteady.Zaplify.ServerEntities;
#endif

namespace BuiltSteady.Zaplify.Shared.Entities
{
    // ************************************************************************
    // Shared ClientItems
    //
    // These are strongly typed classes that wrap an Item using an ItemAccessor
    // These items are stored in the $Client folder and are accessible on
    // both client and server
    //
    // NOTE: metadata properties should only be accessible on server
    // ************************************************************************
     
    public class UserProfile
    {
        ItemAccessor accessor;
        public UserProfile(Item item)
        {
            this.accessor = new ItemAccessor(item);
        }

        public string FirstName
        {
            get { return accessor.Get("FirstName"); }
            set { accessor.Set("FirstName", value); }
        }

        public string LastName
        {
            get { return accessor.Get("LastName"); }
            set { accessor.Set("LastName", value); }
        }

        public string Gender
        {
            get { return accessor.Get("Gender"); }
            set { accessor.Set("Gender", value); }
        }

        public string Picture
        {
            get { return accessor.Get("Picture"); }
            set { accessor.Set("Picture", value); }
        }

        public string Birthday
        {   // TODO: should this be DateTime (string supports MM/dd which is not a valid date)
            get { return accessor.Get("Birthday"); }
            set { accessor.Set("Birthday", value); }
        }

        public string GeoLocation
        {
            get { return accessor.Get("GeoLocation"); }
            set { accessor.Set("GeoLocation", value); }
        }

#if !CLIENT
        // ********************************************************************
        // server can create and access metadata for UserProfile
        // ********************************************************************

        ItemAccessor metadata;
        public UserProfile(Item item, Item metaItem) : this(item)
        {
            this.metadata = new ItemAccessor(metaItem);
        }

        public string FacebookID
        {
            get { return metadata.Get("FacebookID"); }
            set { metadata.Set("FacebookID", value); }
        }

#endif

    }

    public class CalendarSettings
    {
        public const string Google = "Google";
        public const string WindowsLive = "WindowsLive";

        ItemAccessor accessor;
        public CalendarSettings(Item item)
        {
            this.accessor = new ItemAccessor(item);
        }

        public string CalendarProvider
        {   // Google or WindowsLive
            get { return accessor.Get("CalendarProvider"); }
            set { accessor.Set("CalendarProvider", value); }
        }

        public string CalendarID
        {
            get { return accessor.Get("CalendarID"); }
            set { accessor.Set("CalendarID", value); }
        }

#if !CLIENT
        // ********************************************************************
        // server can create and access metadata for UserProfile
        // ********************************************************************

        ItemAccessor metadata;
        public CalendarSettings(Item item, Item metaItem) : this(item)
        {
            this.metadata = new ItemAccessor(metaItem);
        }

        public DateTime? LastSync
        {
            get { return metadata.GetNullableDate("LastSync"); }
            set { metadata.SetDate("LastSync", (DateTime)value); }
        }
#endif

    }

}