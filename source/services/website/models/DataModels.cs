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
                        Priorities = priorities
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

        public static FieldValue CreateFieldValue(Guid itemID, Guid itemTypeID, string fieldName, string value)
        {
            //ItemType itemType = Constants.ItemTypes.Single<ItemType>(item => item.ID == itemTypeID);
            //Field field = itemType.Fields.Single<Field>(fld => fld.Name == fieldName);
            return new FieldValue() { /*ID = Guid.NewGuid(),*/ ItemID = itemID, FieldName = fieldName, Value = value };
        }
    }

    public class UserDataModel
    {
        UserStorageContext storageContext;
        User currentUser;
        User userData;
        UserCredential userCredentials;
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
                    userData.Folders = userData.Folders.OrderBy(f => f.SortOrder).ToList();
                    for (var i=0; i < userData.Folders.Count; i++)
                    {   // sort items by SortOrder field
                        userData.Folders[i].Items = userData.Folders[i].Items.OrderBy(item => item.SortOrder).ToList(); 
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
                {   // do not serialize system folders except for $ClientSettings
                    User userData = UserData;
                    List<Folder> folders = new List<Folder>();
                    for (var i = 0; i < userData.Folders.Count; i++)
                    {
                        Folder folder = userData.Folders[i];
                        if (!folder.Name.StartsWith("$") || folder.Name.StartsWith("$Client"))
                        {
                            folders.Add(folder);
                        }
                    }
                    userData.Folders = folders;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    jsonUserData = JsonSerializer.Serialize(UserData);
                }
                return jsonUserData;
            }
        }

    }
}
