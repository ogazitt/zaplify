using System;
using System.Collections.Generic;

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

        // non user-visible FieldNames
        public const string FacebookID = "FacebookID";      // String
        public const string Intent = "Intent";              // String
        public const string Sources = "Sources";            // String (comma-delimited) 
        
        // TODO: this should be a WorkflowParameter, not a FieldName
        public const string SubjectHint = "SubjectHint";    // String
    }

    public class SuggestionTypes
    {
        public const string ChooseOne = "ChooseOne";
        public const string ChooseOneSubject = "ChooseOneSubject";
        public const string ChooseMany = "ChooseMany";
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
}
