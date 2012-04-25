using System;
using System.Collections.Generic;
#if CLIENT
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
        public static List<Guid> List = new List<Guid>() { System, Task, Location, Contact, ListItem, ShoppingItem, Reference, NameValue};
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
    }

    public class ActionNames
    {                                                       // FieldNames:
        public const string Navigate = "Navigate";          // Contacts, Locations
        public const string Postpone = "Postpone";          // DueDate
        public const string AddToCalendar = "AddToCalendar";// ReminderDate
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
        public const string DueDate = "DueDate";            // DateTime
        public const string ReminderDate = "ReminderDate";  // DateTime
        public const string Birthday = "Birthday";          // DateTime
        public const string Address = "Address";            // Address
        public const string WebLink = "WebLink";            // Url
        public const string WebLinks = "WebLinks";          // ItemID
        public const string Email = "Email";                // Email
        public const string Phone = "Phone";                // Phone
        public const string HomePhone = "HomePhone";        // Phone
        public const string WorkPhone = "WorkPhone";        // Phone
        public const string Amount = "Amount";              // String
        public const string Cost = "Cost";                  // Currency
        public const string ItemTags = "ItemTags";          // TagIDs
        public const string ItemRef = "ItemRef";            // ItemID
        public const string Contacts = "Contacts";          // ItemID
        public const string Locations = "Locations";        // ItemID
        public const string Value = "Value";                // String (value of NameValue - e.g. SuggestionID)
        public const string Category = "Category";          // String (grocery category)

        // non user-visible FieldNames
        public const string FacebookID = "FacebookID";      // String
        public const string Sources = "Sources";            // String (comma-delimited) 
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
        public const string Phone = "Phone";
        public const string Email = "Email";
        public const string Url = "Url";
        public const string Address = "Address";
        public const string Currency = "Currency";
        public const string TagIDs = "TagIDs";
        public const string ItemID = "ItemID";

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
        public const string UrlList = "UrlList";
        public const string Folders = "Folders";
        public const string Lists = "Lists";
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

        // system items
        public const string PossibleSubjects = "PossibleSubjects";
    }

    public class UserDatabase
    {
        public static List<ActionType> DefaultActionTypes()
        {
            // initialize actions
            var actionTypes = new List<ActionType>();
            actionTypes.Add(new ActionType() { ActionTypeID = 1, FieldName = FieldNames.DueDate, DisplayName = "postpone", ActionName = ActionNames.Postpone, SortOrder = 1 });
            actionTypes.Add(new ActionType() { ActionTypeID = 2, FieldName = FieldNames.ReminderDate, DisplayName = "add reminder", ActionName = ActionNames.AddToCalendar, SortOrder = 2 });
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
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.System, Name = "System", UserID = SystemUsers.System, Fields = new List<Field>() });

            // create Task
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Task, Name = "Task", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000011"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000012"), FieldType = FieldTypes.Integer, Name = FieldNames.Priority, DisplayName = "Priority", DisplayType = DisplayTypes.Priority, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000013"), FieldType = FieldTypes.DateTime, Name = FieldNames.DueDate, DisplayName = "Due", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000014"), FieldType = FieldTypes.DateTime, Name = FieldNames.ReminderDate, DisplayName = "Reminder", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000015"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Details", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000016"), FieldType = FieldTypes.ItemID, Name = FieldNames.Contacts, DisplayName = "Contacts", DisplayType = DisplayTypes.ContactList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000017"), FieldType = FieldTypes.ItemID, Name = FieldNames.Locations, DisplayName = "Locations", DisplayType = DisplayTypes.LocationList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000018"), FieldType = FieldTypes.ItemID, Name = FieldNames.WebLinks, DisplayName = "Web Links", DisplayType = DisplayTypes.UrlList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000019"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", DisplayType = DisplayTypes.TagList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000001A"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 10 });

            // create Location
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Location, Name = "Location", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000021"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000022"), FieldType = FieldTypes.Address, Name = FieldNames.Address, DisplayName = "Address", DisplayType = DisplayTypes.Address, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000023"), FieldType = FieldTypes.Phone, Name = FieldNames.Phone, DisplayName = "Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000024"), FieldType = FieldTypes.Email, Name = FieldNames.Email, DisplayName = "Email", DisplayType = DisplayTypes.Email, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000025"), FieldType = FieldTypes.ItemID, Name = FieldNames.WebLinks, DisplayName = "Web Links", DisplayType = DisplayTypes.UrlList, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000026"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Description", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000027"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", DisplayType = DisplayTypes.TagList, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 7 });

            // create Contact
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Contact, Name = "Contact", Icon = "contact.png", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000031"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000032"), FieldType = FieldTypes.Email, Name = FieldNames.Email, DisplayName = "Email", DisplayType = DisplayTypes.Email, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000033"), FieldType = FieldTypes.Phone, Name = FieldNames.Phone, DisplayName = "Mobile Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000034"), FieldType = FieldTypes.Phone, Name = FieldNames.HomePhone, DisplayName = "Home Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000035"), FieldType = FieldTypes.Phone, Name = FieldNames.WorkPhone, DisplayName = "Work Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000036"), FieldType = FieldTypes.ItemID, Name = FieldNames.Locations, DisplayName = "Address", DisplayType = DisplayTypes.LocationList, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000037"), FieldType = FieldTypes.DateTime, Name = FieldNames.Birthday, DisplayName = "Birthday", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000038"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000039"), FieldType = FieldTypes.String, Name = FieldNames.FacebookID, DisplayName = "Facebook ID", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000003A"), FieldType = FieldTypes.String, Name = FieldNames.Sources, DisplayName = "Sources", DisplayType = DisplayTypes.Hidden, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 10 });

            // create ListItem
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ListItem, Name = "ListItem", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000041"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000042"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000043"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Notes", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = false, SortOrder = 3 });

            // create ShoppingItem
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ShoppingItem, Name = "ShoppingItem", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000051"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000052"), FieldType = FieldTypes.String, Name = FieldNames.Category, DisplayName = "Category", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000053"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000054"), FieldType = FieldTypes.String, Name = FieldNames.Amount, DisplayName = "Quantity", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000055"), FieldType = FieldTypes.Currency, Name = FieldNames.Cost, DisplayName = "Price", DisplayType = DisplayTypes.Currency, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000056"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Notes", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 6 });

            // create Reference
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Reference, Name = "Reference", UserID = SystemUsers.System, Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000061"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Reference, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000062"), FieldType = FieldTypes.ItemID, Name = FieldNames.ItemRef, DisplayName = "Reference", DisplayType = DisplayTypes.Reference, ItemTypeID = SystemItemTypes.Reference, IsPrimary = true, SortOrder = 2 });

            // create NameValue
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.NameValue, Name = "NameValue", UserID = SystemUsers.System, Fields = new List<Field>() });
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
    }
}
