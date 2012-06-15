using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Tools.UserDataExport
{
    public class UserDataModel
    {
        UserStorageContext storageContext;
        User currentUser;
        User userData;
        string jsonUserData;

        public UserDataModel(UserStorageContext storage, User user)
        {
            this.storageContext = storage;
            this.currentUser = user;
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
    }
}
