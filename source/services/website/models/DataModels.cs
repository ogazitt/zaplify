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
                        Where(l => l.UserID == null || l.UserID == SystemUsers.System || l.UserID == SystemUsers.User).
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
                        Themes = new List<string>() { "Default", "Redmond", "Overcast" }
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

        UserStorageContext storageContext;
        User currentUser;
        User userData;
        List<Folder> folders;
        List<Folder> clientFolders; 
        string jsonUserData;

        public UserDataModel(UserStorageContext storage, User user)
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

        public string UserTheme
        {
            get
            {
                List<Folder> folders = UserData.Folders;
                foreach (var folder in folders)
                {
                    if (folder.Name.Equals(SystemFolders.ClientSettings))
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
                    userData = storageContext.Users.Include("Folders").Single<User>(u => u.Name == currentUser.Name);
                    if (userData.Folders != null && userData.Folders.Count > 0)
                    {   // get user and all top-level data
                        userData = storageContext.Users.
                            Include("ItemTypes.Fields").
                            Include("Tags").
                            Include("Folders.FolderUsers").
                            Include("Folders.Items.ItemTags").
                            Include("Folders.Items.FieldValues").
                            Single<User>(u => u.ID == userData.ID && u.Folders.Any(f => f.FolderUsers.Any(fu => fu.UserID == userData.ID)));

                        // Items already serialized under Folders, don't serialize another copy
                        userData.Items = null;
                    }

                    // Include does not support filtering or sorting
                    // post-process ordering of folders and items in memory by SortOrder field
                    this.folders = userData.Folders.OrderBy(f => f.SortOrder).ToList();
                    for (var i=0; i < this.folders.Count; i++)
                    {   // sort items by SortOrder field
                        this.folders[i].Items = this.folders[i].Items.OrderBy(item => item.SortOrder).ToList(); 
                    }
                }
                // include ALL folders
                userData.Folders = this.folders;
                return userData;
            }
        }

        // exclude SystemFolders except for $ClientSettings
        public User ClientUserData
        {
            get
            {
                User userData = UserData;
                if (clientFolders == null)
                {
                    clientFolders = new List<Folder>();
                    for (var i = 0; i < this.folders.Count; i++)
                    {
                        Folder folder = this.folders[i];
                        if (!folder.Name.StartsWith("$") || folder.Name.Equals(SystemFolders.ClientSettings))
                        {
                            clientFolders.Add(folder);
                        }
                    }
                }
                userData.Folders = clientFolders;
                return userData;
            }
        }

        public string JsonUserData
        {
            get
            {
                if (jsonUserData == null)
                {   // serialize ClientUserData
                    jsonUserData = JsonSerializer.Serialize(ClientUserData);
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
