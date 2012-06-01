﻿using System;
using System.Collections.Generic;
#if CLIENT
using System.Collections.ObjectModel;
using BuiltSteady.Zaplify.Devices.ClientEntities;
#else
using BuiltSteady.Zaplify.ServerEntities;
#endif

/*---------------------------------------------------------------------------

    Fields are described using the following properties:
    
        ItemType    - the ItemType the field is part of
        FieldName   - semantic type for the field, must be unique per ItemType
        FieldType   - data type for the field
        DisplayName - name displayed in UX for the field
        DisplayType - control type used to to display the field in the UX 

    ItemTypes are extensible and consist of a set of Fields.
        There are a set of system ItemTypes defined below.
    FieldNames are extensible and imply a semantic for the Field.
        There are a set of system FieldNames defined below.
    FieldTypes are static and denote the type to convert to/from a string.
    DisplayNames are simply the string that is rendered in the UX.
    DisplayTypes are static and denote the type of control to display.
 
    ActionNames are static and associated with a FieldName (semantic)
 
----------------------------------------------------------------------------*/
namespace BuiltSteady.Zaplify.Shared.Entities
{

    public class SystemUsers
    {
        public static Guid System = new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid User = new Guid("00000000-0000-0000-0000-000000000002");
    }

    public class SystemItemTypes
    {   
        // standard item types
        public static Guid Task = new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid Location = new Guid("00000000-0000-0000-0000-000000000002");
        public static Guid Contact = new Guid("00000000-0000-0000-0000-000000000003");
        public static Guid ListItem = new Guid("00000000-0000-0000-0000-000000000004");
        public static Guid ShoppingItem = new Guid("00000000-0000-0000-0000-000000000005");
        // system item types
        public static Guid System = new Guid("00000000-0000-0000-0000-000000000000");
        public static Guid Reference = new Guid("00000000-0000-0000-0000-000000000006");
        public static Guid NameValue = new Guid("00000000-0000-0000-0000-000000000007");

        public static Dictionary<Guid, string> Names = new Dictionary<Guid, string>() 
        { 
            { System, ItemTypeNames.System},
            { Task, ItemTypeNames.Task },
            { Location, ItemTypeNames.Location },
            { Contact, ItemTypeNames.Contact },
            { ListItem, ItemTypeNames.ListItem },
            { ShoppingItem, ItemTypeNames.ShoppingItem },
            { Reference, ItemTypeNames.Reference },
            { NameValue,  ItemTypeNames.NameValue }
        };
    }

    public class ItemTypeNames
    {
        // standard item types
        public const string Task = "Task";
        public const string Location = "Location";
        public const string Contact = "Contact";
        public const string ListItem = "List Item";
        public const string ShoppingItem = "Shopping Item";
        // system item types
        public const string System = "System";
        public const string Reference = "Reference";
        public const string NameValue = "NameValue";
    }

    public class ActionNames
    {                                                       // FieldNames:
        public const string Navigate = "Navigate";          // Contacts, Locations
        public const string Postpone = "Postpone";          // DueDate
        public const string AddToCalendar = "AddToCalendar";// DueDate
        public const string Map = "Map";                    // Address
        public const string Call = "Call";                  // Phone, HomePhone, WorkPhone
        public const string TextMessage = "TextMessage";    // Phone
        public const string Browse = "Browse";              // WebLink
        public const string SendEmail = "SendEmail";        // Email
        //public const string PostToFacebook = "PostToFacebook";  
        //public const string Tweet = "Tweet";             
    }

    public class FieldNames
    {                                                       // FieldType:
        public const string Name = "Name";                  // String
        public const string Description = "Description";    // String
        public const string Priority = "Priority";          // Integer
        public const string Complete = "Complete";          // Boolean 
        public const string CompletedOn = "CompletedOn";    // DateTime 
        public const string DueDate = "DueDate";            // DateTime
        public const string Duration = "Duration";          // TimeSpan
        public const string Birthday = "Birthday";          // DateTime
        public const string Address = "Address";            // Address
        public const string WebLink = "WebLink";            // Url
        public const string WebLinks = "WebLinks";          // Json
        public const string Email = "Email";                // Email
        public const string Phone = "Phone";                // Phone
        public const string HomePhone = "HomePhone";        // Phone
        public const string WorkPhone = "WorkPhone";        // Phone
        public const string Amount = "Amount";              // String
        public const string Cost = "Cost";                  // Currency
        public const string ItemTags = "ItemTags";          // TagIDs
        public const string EntityRef = "EntityRef";        // EntityID
        public const string EntityType = "EntityType";      // String ( User, Folder, Item )
        public const string Contacts = "Contacts";          // EntityID
        public const string Locations = "Locations";        // EntityID
        public const string Value = "Value";                // String (value of NameValue - e.g. SuggestionID)
        public const string Category = "Category";          // String (grocery category)

        // Hidden FieldName extensions
        public const string Intent = "Intent";              // lowercase string 

        // Hidden FieldName extensions for Task
        public const string SubjectHint = "SubjectHint";    // String
        
        // Hidden FieldName extensions for Contact
        public const string FacebookID = "FacebookID";      // String
        public const string Gender = "Gender";              // String
        public const string Picture = "Picture";            // ImageUrl
        public const string Sources = "Sources";            // String (comma-delimited) 
        public const string Location = "Location";          // String

        // Hidden FieldName extensions for Location
        public const string LatLong = "LatLong";            // String (comma-delimited)

        // FieldNames for EntityRef extensions
        public const string SelectedCount = "SelectedCount";// Integer
        public const string SortBy = "SortBy";              // String (ClientSettings: field name to sort a folder/list by)
    }

    public class SuggestionTypes
    {
        public const string ChooseOne = "ChooseOne";
        public const string ChooseOneSubject = "ChooseOneSubject";
        public const string ChooseMany = "ChooseMany";
        public const string ChooseManyWithChildren = "ChooseManyWithChildren";
        public const string GetFBConsent = "GetFBConsent";
        public const string GetADConsent = "GetADConsent";
        public const string NavigateLink = "NavigateLink";
        public const string RefreshEntity = "RefreshEntity";
    }

    public class Reasons
    {
        public const string Chosen = "Chosen";
        public const string Ignore = "Ignore";
        public const string Like = "Like";
        public const string Dislike = "Dislike";
    }

    public class FieldTypes
    {
        public const string String = "String";
        public const string Boolean = "Boolean";
        public const string Integer = "Integer";
        public const string DateTime = "DateTime";
        public const string TimeSpan = "TimeSpan";
        public const string Phone = "Phone";
        public const string Email = "Email";
        public const string Url = "Url";
        public const string Address = "Address";
        public const string Currency = "Currency";
        public const string Guid = "Guid";
        public const string Json = "Json";
        public const string TagIDs = "TagIDs";

        public static string DefaultValue(string ft)
        {
            switch (ft)
            {
                case Boolean: return "false";
                default: return null;
            }
        }
    }

    public class DisplayTypes
    {
        public const string Hidden = "Hidden";
        public const string Text = "Text";
        public const string TextArea = "TextArea";
        public const string Checkbox = "Checkbox";
        public const string DatePicker = "DatePicker";
        public const string DateTimePicker = "DateTimePicker";
        public const string TimeSpanPicker = "TimeSpanPicker";
        public const string Phone = "Phone";
        public const string Email = "Email";
        public const string Link = "Link";
        public const string Currency = "Currency";
        public const string Address = "Address";
        public const string Priority = "Priority";
        public const string Reference = "Reference";
        public const string TagList = "TagList";
        public const string ContactList = "ContactList";
        public const string LocationList = "LocationList";
        public const string LinkArray = "LinkArray";
        public const string Folders = "Folders";
        public const string Lists = "Lists";
        public const string ImageUrl = "ImageUrl";
        public const string ItemTypes = "ItemTypes";
    }

    public class EntityTypes
    {
        public const string User = "User";
        public const string Folder = "Folder";
        public const string Item = "Item";
    }

    public class Permissions
    {
        public static int View = 1;
        public static int Modify = 2;
        public static int Full = 3;

        public static bool IsView(int permission)
        { return (permission == View); }

        public static bool CanView(int permission)
        { return (permission == View || permission == Modify || permission == Full); }

        public static bool IsModify(int permission)
        { return (permission == Modify); }

        public static bool CanModify(int permission)
        { return (permission == Modify || permission == Full); }

        public static bool IsFull(int permission)
        { return (permission == Full); }
    }

    public class Sources
    {
        public const string Directory = "Directory";
        public const string Facebook = "Facebook";
        public const string Local = "Local";
    }

    public class SystemEntities
    {   // system folders
        public const string ClientSettings = "$ClientSettings";
        public const string User = "$User";

        // system items - $ClientSettings
        public const string DefaultLists = "DefaultLists";
        public const string ListMetadata = "ListMetadata";
#if IOS
        public const string PhoneSettings = "iPhoneSettings";
#else
        public const string PhoneSettings = "WinPhoneSettings";
#endif

        // system items - $User
        public const string EntityRefs = "EntityRefs";
    }

    public class UserEntities
    {
        // user folders
        public const string Activities = "Activities";
        public const string Lists = "Lists";
        public const string People = "People";
        public const string Places = "Places";

        // user items
        public const string Tasks = "Tasks";
        public const string Groceries = "Groceries";
    }

    public class UserConstants
    {
        public static string SchemaVersion { get { return "1.0.2012.0503"; } }
        public static string ConstantsVersion { get { return "2012-05-18"; } }

        public static List<ActionType> DefaultActionTypes()
        {
            // initialize actions
            var actionTypes = new List<ActionType>();
            actionTypes.Add(new ActionType() { ActionTypeID = 1, FieldName = FieldNames.DueDate, DisplayName = "postpone", ActionName = ActionNames.Postpone, SortOrder = 1 });
            actionTypes.Add(new ActionType() { ActionTypeID = 2, FieldName = FieldNames.DueDate, DisplayName = "add to calendar", ActionName = ActionNames.AddToCalendar, SortOrder = 2 });
            actionTypes.Add(new ActionType() { ActionTypeID = 3, FieldName = FieldNames.Address, DisplayName = "map", ActionName = ActionNames.Map, SortOrder = 3 });
            actionTypes.Add(new ActionType() { ActionTypeID = 4, FieldName = FieldNames.Phone, DisplayName = "call cell", ActionName = ActionNames.Call, SortOrder = 4 });
            actionTypes.Add(new ActionType() { ActionTypeID = 5, FieldName = FieldNames.HomePhone, DisplayName = "call home", ActionName = ActionNames.Call, SortOrder = 5 });
            actionTypes.Add(new ActionType() { ActionTypeID = 6, FieldName = FieldNames.WorkPhone, DisplayName = "call work", ActionName = ActionNames.Call, SortOrder = 6 });
            actionTypes.Add(new ActionType() { ActionTypeID = 7, FieldName = FieldNames.Phone, DisplayName = "text", ActionName = ActionNames.TextMessage, SortOrder = 7 });
            actionTypes.Add(new ActionType() { ActionTypeID = 8, FieldName = FieldNames.WebLink, DisplayName = "browse", ActionName = ActionNames.Browse, SortOrder = 8 });
            actionTypes.Add(new ActionType() { ActionTypeID = 9, FieldName = FieldNames.Email, DisplayName = "email", ActionName = ActionNames.SendEmail, SortOrder = 9 });
            actionTypes.Add(new ActionType() { ActionTypeID = 10, FieldName = FieldNames.Contacts, DisplayName = "show contacts", ActionName = ActionNames.Navigate, SortOrder = 10 });
            actionTypes.Add(new ActionType() { ActionTypeID = 11, FieldName = FieldNames.Locations, DisplayName = "show locations", ActionName = ActionNames.Navigate, SortOrder = 11 });
            return actionTypes;
        }

        public static List<Color> DefaultColors()
        {
            // initialize colors
            var colors = new List<Color>();
            colors.Add(new Color() { ColorID = 0, Name = "White" });
            colors.Add(new Color() { ColorID = 1, Name = "Blue" });
            colors.Add(new Color() { ColorID = 2, Name = "Brown" });
            colors.Add(new Color() { ColorID = 3, Name = "Green" });
            colors.Add(new Color() { ColorID = 4, Name = "Orange" });
            colors.Add(new Color() { ColorID = 5, Name = "Purple" });
            colors.Add(new Color() { ColorID = 6, Name = "Red" });
            colors.Add(new Color() { ColorID = 7, Name = "Yellow" });
            return colors;
        }

        public static List<Permission> DefaultPermissions()
        {
            // initialize permissions
            var permissions = new List<Permission>();
            permissions.Add(new Permission() { PermissionID = 1, Name = "View" });
            permissions.Add(new Permission() { PermissionID = 2, Name = "Modify" });
            permissions.Add(new Permission() { PermissionID = 3, Name = "Full" });
            return permissions;
        }

        public static List<Priority> DefaultPriorities()
        {
            // initialize priorities
            var priorities = new List<Priority>();
            priorities.Add(new Priority() { PriorityID = 0, Name = "Low", Color = "Green" });
            priorities.Add(new Priority() { PriorityID = 1, Name = "Normal", Color = "White" });
            priorities.Add(new Priority() { PriorityID = 2, Name = "High", Color = "Red" });
            return priorities;
        }

        public static List<ItemType> DefaultItemTypes()
        {
            List<ItemType> itemTypes = new List<ItemType>();

            ItemType itemType;

            // create System
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.System, Name = ItemTypeNames.System, UserID = SystemUsers.System, Fields = new List<Field>() });

            // create Task
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Task, Name = ItemTypeNames.Task, UserID = SystemUsers.User, Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000011"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000012"), FieldType = FieldTypes.Integer, Name = FieldNames.Priority, DisplayName = "Priority", DisplayType = DisplayTypes.Priority, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000013"), FieldType = FieldTypes.DateTime, Name = FieldNames.DueDate, DisplayName = "Due", DisplayType = DisplayTypes.DateTimePicker, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000014"), FieldType = FieldTypes.TimeSpan, Name = FieldNames.Duration, DisplayName = "Duration", DisplayType = DisplayTypes.TimeSpanPicker, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000015"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Details", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000016"), FieldType = FieldTypes.Guid, Name = FieldNames.Contacts, DisplayName = "Contacts", DisplayType = DisplayTypes.ContactList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000017"), FieldType = FieldTypes.Guid, Name = FieldNames.Locations, DisplayName = "Locations", DisplayType = DisplayTypes.LocationList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000018"), FieldType = FieldTypes.Json, Name = FieldNames.WebLinks, DisplayName = "Web Links", DisplayType = DisplayTypes.LinkArray, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000019"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", /* DisplayTypes.TagList */ DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000001A"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 10 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000001B"), FieldType = FieldTypes.DateTime, Name = FieldNames.CompletedOn, DisplayName = "Completed On", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 11 });

            // create Location
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Location, Name = ItemTypeNames.Location, UserID = SystemUsers.User, Icon = "location.png", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000021"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000022"), FieldType = FieldTypes.Address, Name = FieldNames.Address, DisplayName = "Address", DisplayType = DisplayTypes.Address, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000023"), FieldType = FieldTypes.Phone, Name = FieldNames.Phone, DisplayName = "Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000024"), FieldType = FieldTypes.Email, Name = FieldNames.Email, DisplayName = "Email", DisplayType = DisplayTypes.Email, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000025"), FieldType = FieldTypes.Json, Name = FieldNames.WebLinks, DisplayName = "Web Links", DisplayType = DisplayTypes.LinkArray, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000026"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Description", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000027"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", /* DisplayTypes.TagList */ DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000028"), FieldType = FieldTypes.String, Name = FieldNames.LatLong, DisplayName = "LatLong", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 8 });

            // create Contact
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Contact, Name = ItemTypeNames.Contact, UserID = SystemUsers.User, Icon = "contact.png", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000031"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000032"), FieldType = FieldTypes.Email, Name = FieldNames.Email, DisplayName = "Email", DisplayType = DisplayTypes.Email, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000033"), FieldType = FieldTypes.Phone, Name = FieldNames.Phone, DisplayName = "Mobile Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000034"), FieldType = FieldTypes.Phone, Name = FieldNames.HomePhone, DisplayName = "Home Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000035"), FieldType = FieldTypes.Phone, Name = FieldNames.WorkPhone, DisplayName = "Work Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000036"), FieldType = FieldTypes.Guid, Name = FieldNames.Locations, DisplayName = "Address", DisplayType = DisplayTypes.LocationList, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000037"), FieldType = FieldTypes.DateTime, Name = FieldNames.Birthday, DisplayName = "Birthday", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000038"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", /* DisplayTypes.TagList */ DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000039"), FieldType = FieldTypes.String, Name = FieldNames.FacebookID, DisplayName = "Facebook ID", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000003A"), FieldType = FieldTypes.String, Name = FieldNames.Sources, DisplayName = "Sources", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 10 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000003B"), FieldType = FieldTypes.Url, Name = FieldNames.Picture, DisplayName = "Picture", DisplayType = DisplayTypes.Hidden /* TODO: DisplayTypes.ImageUrl */, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 11 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000003C"), FieldType = FieldTypes.String, Name = FieldNames.Gender, DisplayName = "Gender", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 12 });

            // create ListItem
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ListItem, Name = ItemTypeNames.ListItem, UserID = SystemUsers.User, Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000041"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000042"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000043"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Notes", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = false, SortOrder = 3 });

            // create ShoppingItem
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ShoppingItem, Name = ItemTypeNames.ShoppingItem, UserID = SystemUsers.User, Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000051"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000052"), FieldType = FieldTypes.String, Name = FieldNames.Category, DisplayName = "Category", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000053"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000054"), FieldType = FieldTypes.String, Name = FieldNames.Amount, DisplayName = "Quantity", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000055"), FieldType = FieldTypes.Currency, Name = FieldNames.Cost, DisplayName = "Price", DisplayType = DisplayTypes.Currency, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000056"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Notes", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000057"), FieldType = FieldTypes.DateTime, Name = FieldNames.CompletedOn, DisplayName = "Completed On", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000058"), FieldType = FieldTypes.Url, Name = FieldNames.Picture, DisplayName = "Picture", DisplayType = DisplayTypes.Hidden /* TODO: DisplayTypes.ImageUrl */, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 8 });

            // create Reference
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Reference, Name = ItemTypeNames.Reference, UserID = SystemUsers.System, Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000061"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Reference, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000062"), FieldType = FieldTypes.Guid, Name = FieldNames.EntityRef, DisplayName = "EntityRef", DisplayType = DisplayTypes.Reference, ItemTypeID = SystemItemTypes.Reference, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000063"), FieldType = FieldTypes.String, Name = FieldNames.EntityType, DisplayName = "EntityType", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Reference, IsPrimary = true, SortOrder = 3 });

            // create NameValue
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.NameValue, Name = ItemTypeNames.NameValue, UserID = SystemUsers.System, Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000071"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.NameValue, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000072"), FieldType = FieldTypes.String, Name = FieldNames.Value, DisplayName = "Value", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.NameValue, IsPrimary = true, SortOrder = 2 });

            return itemTypes;
        }

        public static List<User> DefaultUsers()
        {
            List<User> users = new List<User>();

            User user;

            // create the Task
            users.Add(user = new User() { ID = SystemUsers.System, Name = "System", Email = "system@builtsteady.com" });
#if !CLIENT
            user.CreateDate = new DateTime(2012, 1, 1);
#endif
            users.Add(user = new User() { ID = SystemUsers.User, Name = "User", Email = "user@builtsteady.com" });
#if !CLIENT
            user.CreateDate = new DateTime(2012, 1, 1);
#endif
            return users;
        }

        public static List<Folder> DefaultFolders(User currentUser)
        {
            List<Folder> folders = new List<Folder>();
            Dictionary<Guid, object> defaultLists = new Dictionary<Guid, object>();
            DateTime now = DateTime.Now;
            Item item;
            Folder folder;
            Guid folderID = Guid.NewGuid();

#if !CLIENT
            FolderUser folderUser;
            folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = folderID, UserID = currentUser.ID, PermissionID = Permissions.Full };
#endif
            // create Activities folder
            folder = new Folder()
            {
                ID = folderID,
                SortOrder = 1000,
                Name = UserEntities.Activities,
                ItemTypeID = SystemItemTypes.Task,
#if CLIENT
                Items = new ObservableCollection<Item>(),
#else
                Items = new List<Item>(),
                UserID = currentUser.ID,
                FolderUsers = new List<FolderUser>() { folderUser }
#endif
            };
            folders.Add(folder);

            // create Tasks list
            item = new Item()
            {
                ID = Guid.NewGuid(),
                SortOrder = 1000,
                Name = UserEntities.Tasks,
                FolderID = folder.ID,
                IsList = true,
                ItemTypeID = SystemItemTypes.Task,
                ParentID = null,
                Created = now,
                LastModified = now,
#if !CLIENT
                UserID = currentUser.ID,
#endif
            };
            folder.Items.Add(item);
            // make this defaultList for Tasks
            defaultLists.Add(SystemItemTypes.Task, item);

            // create Learn Zaplify task
            item = new Item()
            {
                ID = Guid.NewGuid(),
                SortOrder = 2000,
                Name = "Learn about Zaplify!",
                FolderID = folder.ID,
                IsList = false,
                ItemTypeID = SystemItemTypes.Task,
                ParentID = item.ID,
                Created = now,
                LastModified = now,
#if CLIENT
                FieldValues = new ObservableCollection<FieldValue>(),
#else
                FieldValues = new List<FieldValue>(),
                UserID = currentUser.ID,
#endif
            };
            folder.Items.Add(item);

#if CLIENT
            item.GetFieldValue(FieldNames.DueDate, true).Value = DateTime.Today.Date.ToString("MM/dd/yyyy");
            item.GetFieldValue(FieldNames.Description, true).Value = "Get connected with Zaplify.";
#else
            item.FieldValues.Add(Item.CreateFieldValue(item.ID, FieldNames.DueDate, DateTime.Today.Date.ToString("MM/dd/yyyy")));
            item.FieldValues.Add(Item.CreateFieldValue(item.ID, FieldNames.Description, "Get connected with Zaplify."));
#endif

            folderID = Guid.NewGuid();
#if !CLIENT
            folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = folderID, UserID = currentUser.ID, PermissionID = Permissions.Full };
#endif
            // create Lists folder
            folder = new Folder()
            {
                ID = folderID,
                SortOrder = 2000,
                Name = UserEntities.Lists,
                ItemTypeID = SystemItemTypes.ListItem,
#if CLIENT
                Items = new ObservableCollection<Item>(),
#else
                Items = new List<Item>(),
                UserID = currentUser.ID,
                FolderUsers = new List<FolderUser>() { folderUser }
#endif
            };
            folders.Add(folder);
            // make this defaultList for ListItems
            defaultLists.Add(SystemItemTypes.ListItem, folder);

            // create Groceries list
            item = new Item()
            {
                ID = Guid.NewGuid(),
                SortOrder = 3000,
                Name = UserEntities.Groceries,
                FolderID = folder.ID,
                IsList = true,
                ItemTypeID = SystemItemTypes.ShoppingItem,
                ParentID = null,
                Created = now,
                LastModified = now,
#if !CLIENT
                UserID = currentUser.ID,
#endif
            };
            folder.Items.Add(item);
            // make this defaultList for ShoppingItems
            defaultLists.Add(SystemItemTypes.ShoppingItem, item);

            folderID = Guid.NewGuid();
#if !CLIENT
            folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = folderID, UserID = currentUser.ID, PermissionID = Permissions.Full };
#endif            
            // create People folder
            folder = new Folder()
            {
                ID = folderID,
                SortOrder = 3000,
                Name = UserEntities.People,
                ItemTypeID = SystemItemTypes.Contact,
#if CLIENT
                Items = new ObservableCollection<Item>(),
#else
                Items = new List<Item>(),
                UserID = currentUser.ID,
                FolderUsers = new List<FolderUser>() { folderUser }
#endif
            };
            folders.Add(folder);
            // make this defaultList for Contacts
            defaultLists.Add(SystemItemTypes.Contact, folder);

            folderID = Guid.NewGuid();
#if !CLIENT
            folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = folderID, UserID = currentUser.ID, PermissionID = Permissions.Full };
#endif
            // create Places folder
            folder = new Folder()
            {
                ID = folderID,
                SortOrder = 4000,
                Name = UserEntities.Places,
                ItemTypeID = SystemItemTypes.Location,
#if CLIENT
                Items = new ObservableCollection<Item>(),
#else
                Items = new List<Item>(),
                UserID = currentUser.ID,
                FolderUsers = new List<FolderUser>() { folderUser }
#endif
            };
            folders.Add(folder);
            // make this defaultList for Locations
            defaultLists.Add(SystemItemTypes.Location, folder);

            folderID = Guid.NewGuid();
#if !CLIENT
            folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = folderID, UserID = currentUser.ID, PermissionID = Permissions.Full };
#endif
            // create $ClientSettings folder
            folder = new Folder() 
            {
                ID = folderID, 
                SortOrder = 0, 
                Name = SystemEntities.ClientSettings, 
                ItemTypeID = SystemItemTypes.NameValue,
#if CLIENT
                Items = new ObservableCollection<Item>(),
#else
                Items = new List<Item>(), 
                UserID = currentUser.ID, 
                FolderUsers = new List<FolderUser>() { folderUser }
#endif
            };
            folders.Add(folder);

            // create DefaultLists list
            var defaultListItemID = Guid.NewGuid();
            item = new Item()
            {
                ID = defaultListItemID,
                SortOrder = 0,
                Name = SystemEntities.DefaultLists,
                FolderID = folder.ID,
                IsList = true,
                ItemTypeID = SystemItemTypes.Reference,
                ParentID = null,
                Created = now,
                LastModified = now,
#if !CLIENT
                UserID = currentUser.ID,
#endif
            };
            folder.Items.Add(item);

            // add defaultList References for each ItemType
            int sortOrder = 1;
            foreach (var keyValue in defaultLists)
            {
                item = new Item()
                {
                    ID = Guid.NewGuid(),
                    SortOrder = sortOrder++,
                    Name = keyValue.Key.ToString(),
                    FolderID = folder.ID,
                    IsList = false,
                    ItemTypeID = SystemItemTypes.Reference,
                    ParentID = defaultListItemID,
                    Created = now,
                    LastModified = now,
#if CLIENT
                    FieldValues = new ObservableCollection<FieldValue>(),
#else
                    FieldValues = new List<FieldValue>(),
                    UserID = currentUser.ID,
#endif
                };

                if (keyValue.Value is Folder) 
                { 
                    Folder folderToRef = (Folder)keyValue.Value; 
#if CLIENT
                    item.GetFieldValue(FieldNames.EntityRef, true).Value = folderToRef.ID.ToString();
                    item.GetFieldValue(FieldNames.EntityType, true).Value = EntityTypes.Folder;
#else
                    item.FieldValues.Add(Item.CreateFieldValue(item.ID, FieldNames.EntityRef, folderToRef.ID.ToString()));
                    item.FieldValues.Add( Item.CreateFieldValue(item.ID, FieldNames.EntityType, EntityTypes.Folder));
#endif                
                }
                if (keyValue.Value is Item) 
                { 
                    Item itemToRef = (Item)keyValue.Value; 
#if CLIENT
                    item.GetFieldValue(FieldNames.EntityRef, true).Value = itemToRef.ID.ToString();
                    item.GetFieldValue(FieldNames.EntityType, true).Value = EntityTypes.Item;
#else
                    item.FieldValues.Add(Item.CreateFieldValue(item.ID, FieldNames.EntityRef, itemToRef.ID.ToString()));
                    item.FieldValues.Add(Item.CreateFieldValue(item.ID, FieldNames.EntityType, EntityTypes.Item));
#endif                  
                }

                folder.Items.Add(item);
            }

            return folders;
        }

    }
}
