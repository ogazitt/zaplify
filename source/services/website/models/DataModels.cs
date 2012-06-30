namespace BuiltSteady.Zaplify.Website.Models
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Web.Script.Serialization;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Shared.Entities;
    using BuiltSteady.Zaplify.Website.Controllers;
    using BuiltSteady.Zaplify.Website.Resources;

    public static class ConstantsModel
    {
        static Constants constants;
        static string jsonConstants;

        public static Constants Constants
        {
            get
            {
                if (constants == null)
                {
                    UserStorageContext storageContext = Storage.StaticUserContext;
                    var actionTypes = storageContext.ActionTypes.OrderBy(a => a.SortOrder).ToList<ActionType>();
                    var colors = storageContext.Colors.OrderBy(c => c.ColorID).ToList<Color>();
                    var itemTypes = storageContext.ItemTypes.
                        Where(l => l.UserID == SystemUsers.System || l.UserID == SystemUsers.User).
                        Include("Fields").ToList<ItemType>();  // get the built-in itemtypes
                    var permissions = storageContext.Permissions.OrderBy(p => p.PermissionID).ToList<Permission>();
                    var priorities = storageContext.Priorities.OrderBy(p => p.PriorityID).ToList<Priority>();
                    constants = new Constants()
                    {
                        ActionTypes = actionTypes,
                        Colors = colors,
                        ItemTypes = itemTypes,
                        Permissions = permissions,
                        Priorities = priorities,
                        // TODO: inspect themes folder to fetch installed themes
                        Themes = new List<string>() { "Default", "Redmond", "Pink", "Overcast" }
                    };
                }
                return constants;
            }
        }

        public static string JsonConstants
        {
            get
            {
                if (jsonConstants == null)
                {
                    jsonConstants = JsonSerializer.Serialize(Constants);
                }
                return jsonConstants;
            }
        }
    }

    public class UserDataModel
    {
        public const string DefaultTheme = "default";
        public const string FBConsentSuccess = "FBConsentSuccess";
        public const string FBConsentFail = "FBConsentFail";
        public const string GoogleConsentSuccess = "GoogleConsentSuccess";
        public const string GoogleConsentFail = "GoogleConsentFail";
        public const string CloudADConsentSuccess = "CloudADConsentSuccess";
        public const string CloudADConsentFail = "CloudADConsentFail";

        UserStorageContext storageContext;
        User currentUser;
        User userData;
        string jsonUserData;

        public UserDataModel(User user, UserStorageContext storage)
        {
            this.storageContext = storage;
            this.currentUser = user;
        }

        public UserDataModel(BaseController controller)
        {
            this.storageContext = controller.StorageContext;
            this.currentUser = controller.CurrentUser;
        }

        public UserDataModel(BaseResource resource)
        {
            this.storageContext = resource.StorageContext;
            this.currentUser = resource.CurrentUser;
        }

        public bool RenewFBToken { get; set; }
        public string ConsentStatus { get; set; }

        public string UserTheme
        {
            get
            {
                foreach (var folder in UserData.Folders)
                {
                    if (folder.Name.Equals(SystemEntities.ClientSettings))
                    {
                        foreach (var item in folder.Items)
                        {
                            if (item.Name.Equals(UserPreferences.UserPreferencesKey))
                            {
                                foreach (var fv in item.FieldValues)
                                {
                                    if (fv.FieldName.Equals(FieldNames.Value))
                                    {
                                        UserPreferences preferences = JsonSerializer.Deserialize<UserPreferences>(fv.Value);
                                        return preferences.Theme;
                                    }
                                }                                
                            }
                        }
                        break;
                    }
                }
                return UserDataModel.DefaultTheme;
            }
        }

        public UserStorageContext StorageContext
        {
            get { return this.storageContext; }
        }

        public User UserData
        {
            get
            {
                if (userData == null)
                {
                    // synchronize with Calendar (BEFORE getting UserData)
                    GoogleClient client = new GoogleClient(currentUser, storageContext);
                    client.SynchronizeCalendar();

                    userData = storageContext.Users.
                        Include("ItemTypes.Fields").
                        Include("Tags").
                        Single<User>(u => u.Name == currentUser.Name);

                    // retrieve non-system folders for this user 
                    // (does not include other user folders this user has been given access to via FolderUsers)
                    List<Folder> folders = this.StorageContext.Folders.
                        Include("FolderUsers").
                        Include("Items.ItemTags").
                        Include("Items.FieldValues").
                        Where(f => f.UserID == userData.ID && f.ItemTypeID != SystemItemTypes.System).
                        OrderBy(f => f.SortOrder).
                        ToList();

                    if (folders != null && folders.Count > 0)
                    {
                        userData.Folders = folders;

                        // Include does not support filtering or sorting
                        // post-process ordering of Items in memory by SortOrder field
                        for (var i = 0; i < userData.Folders.Count; i++)
                        {   // sort items by SortOrder field
                            userData.Folders[i].Items = userData.Folders[i].Items.OrderBy(item => item.SortOrder).ToList();
                        }
                    }
                    else
                    {
                        userData.Folders = new List<Folder>();
                    }
                }
                return userData;
            }
        }

        public string JsonUserData
        {
            get
            {
                if (jsonUserData == null)
                {   // serialize UserData
                    jsonUserData = JsonSerializer.Serialize(UserData);
                }
                return jsonUserData;
            }
        }

        // static accessor uses HttpContext to give access to Master page
        const string currentThemeKey = "CurrentTheme";
        public static string CurrentTheme
        {
            get
            {
                if (System.Web.HttpContext.Current != null &&
                    System.Web.HttpContext.Current.Items.Contains(currentThemeKey))
                {
                    return (string)System.Web.HttpContext.Current.Items[currentThemeKey];
                }
                return UserDataModel.DefaultTheme;
            }
            set
            {
                if (System.Web.HttpContext.Current != null)
                {
                    System.Web.HttpContext.Current.Items[currentThemeKey] = value;
                }
            }
        }

    }

    public class UserPreferences
    {
        public const string UserPreferencesKey = "WebPreferences";

        public string Theme { get; set; }
    }
}
