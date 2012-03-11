namespace BuiltSteady.Zaplify.Website.Models
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Web.Script.Serialization;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
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
                    StorageContext storageContext = Storage.StaticContext;
                    var actionTypes = storageContext.ActionTypes.OrderBy(a => a.SortOrder).ToList<ActionType>();
                    var colors = storageContext.Colors.OrderBy(c => c.ColorID).ToList<Color>();
                    var itemTypes = storageContext.ItemTypes.Where(l => l.UserID == null).Include("Fields").ToList<ItemType>();  // get the built-in itemtypes
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
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    jsonConstants = serializer.Serialize(Constants);
                }
                return jsonConstants;
            }
        }

        public static FieldValue CreateFieldValue(Guid itemID, Guid itemTypeID, string fieldName, string value)
        {
            ItemType itemType = Constants.ItemTypes.Single<ItemType>(item => item.ID == itemTypeID);
            Field field = itemType.Fields.Single<Field>(fld => fld.DisplayName == fieldName);
            return new FieldValue() { /*ID = Guid.NewGuid(),*/ ItemID = itemID, FieldID = field.ID, Value = value };
        }
    }

    public class UserDataModel
    {
        StorageContext storageContext;
        User currentUser;
        User userData;
        string jsonUserData;

        public UserDataModel(StorageContext storage, User user)
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

        public StorageContext StorageContext
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

                    // Include does not support filtering or sorting, post-process ordering of folders and items in memory
                    // sort folders by SortOrder field
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
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    jsonUserData = serializer.Serialize(UserData);
                }
                return jsonUserData;
            }
        }
    }

}
