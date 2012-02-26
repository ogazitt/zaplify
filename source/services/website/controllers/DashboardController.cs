namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Website.Models;

    public class DashboardController : BaseController
    {

        public ActionResult Home()
        {
            UserDataModel model = new UserDataModel(this);
            try
            {   // force access to validate current user
                var userData = model.UserData;
            }
            catch
            {
                return RedirectToAction("SignOut", "Account");
            }
            return View(model);
        }

        public ActionResult Initialize()
        {
            UserDataModel model = new UserDataModel(this);
            try
            {    
                if (model.UserData.Folders.Count == 0)
                {   // only create default folders if no folders exist
                    CreateDefaultFolders(model);
                }
            }
            catch
            {
                return RedirectToAction("SignOut", "Account");
            } 
            return RedirectToAction("Home", "Dashboard");
        }

        void CreateDefaultFolders(UserDataModel model)
        {
            Guid folderID = Guid.NewGuid();
            DateTime now = DateTime.Now;
            Folder folder;
            Item item;

            FolderUser folderUser = new FolderUser() 
                {  ID = Guid.NewGuid(), FolderID = folderID, UserID = this.CurrentUser.ID, PermissionID=Permission.Full };

            // create Activities folder
            folder = new Folder() { ID = folderID, 
                Name = "Activities", UserID = this.CurrentUser.ID, 
                DefaultItemTypeID = ItemType.Task, Items = new List<Item>(), 
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Tasks list
            item = new Item() { ID = Guid.NewGuid(), 
                Name = "Tasks", FolderID = folder.ID, UserID = this.CurrentUser.ID,
                IsList = true, ItemTypeID = ItemType.Task, ParentID = null,
                Created = now, LastModified = now 
            };
            model.StorageContext.Items.Add(item);

            // create Learn Zaplify task
            item = new Item() { ID = Guid.NewGuid(),  
                Name = "Learn about Zaplify!", FolderID = folder.ID, UserID = this.CurrentUser.ID,
                IsList = false, ItemTypeID = ItemType.Task, ParentID = item.ID, 
                FieldValues = new List<FieldValue>(),
                Created = now, LastModified = now
            };
            model.StorageContext.Items.Add(item);
            
            model.StorageContext.FieldValues.Add(
                ConstantsModel.CreateFieldValue(item.ID, ItemType.Task, "Due", DateTime.Today.Date.ToString("yyyy/MM/dd")));
            model.StorageContext.FieldValues.Add(
                ConstantsModel.CreateFieldValue(item.ID, ItemType.Task, "Details", "Tap the browse button below to discover more about Zaplify."));

            // create Lists folder
            folderID = Guid.NewGuid();
            folderUser.FolderID = folderID;
            folder = new Folder()
            {
                ID = folderID,
                Name = "Lists",
                UserID = this.CurrentUser.ID,
                DefaultItemTypeID = ItemType.ListItem,
                Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Groceries list
            item = new Item()
            {
                ID = Guid.NewGuid(),
                Name = "Groceries",
                FolderID = folder.ID,
                UserID = this.CurrentUser.ID,
                IsList = true,
                ItemTypeID = ItemType.ListItem,
                ParentID = null,
                Created = now,
                LastModified = now
            };
            model.StorageContext.Items.Add(item);

            // create People folder
            folderID = Guid.NewGuid();
            folderUser.FolderID = folderID;
            folder = new Folder()
            {
                ID = folderID,
                Name = "People",
                UserID = this.CurrentUser.ID,
                DefaultItemTypeID = ItemType.Contact,
                Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Places folder
            folderID = Guid.NewGuid();
            folderUser.FolderID = folderID;
            folder = new Folder()
            {
                ID = folderID,
                Name = "Places",
                UserID = this.CurrentUser.ID,
                DefaultItemTypeID = ItemType.Location,
                Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            model.StorageContext.SaveChanges();
        }
    }
}
