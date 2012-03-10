namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;

    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
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
            catch (Exception)
            {
                return RedirectToAction("Initialize", "Dashboard");
                //return RedirectToAction("SignOut", "Account");
            } 
            return RedirectToAction("Home", "Dashboard");
        }

        void CreateDefaultFolders(UserDataModel model)
        {
            DateTime now = DateTime.Now;
            FolderUser folderUser;
            Folder folder;
            Item item;

            // create Activities folder
            folderUser = new FolderUser() 
                {  ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID=Permissions.Full };
            folder = new Folder() { ID = folderUser.FolderID, SortOrder = 1000,
                Name = "Activities", UserID = this.CurrentUser.ID, 
                ItemTypeID = SystemItemTypes.Task, Items = new List<Item>(), 
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Tasks list
            item = new Item() { ID = Guid.NewGuid(),
                SortOrder = 1000, Name = "Tasks",
                FolderID = folder.ID, UserID = this.CurrentUser.ID,
                IsList = true, ItemTypeID = SystemItemTypes.Task, ParentID = null,
                Created = now, LastModified = now
            };
            model.StorageContext.Items.Add(item);

            // create Learn Zaplify task
            item = new Item() { ID = Guid.NewGuid(),
                SortOrder = 2000, Name = "Learn about Zaplify!",
                FolderID = folder.ID, UserID = this.CurrentUser.ID,
                IsList = false, ItemTypeID = SystemItemTypes.Task, ParentID = item.ID,
                FieldValues = new List<FieldValue>(),
                Created = now, LastModified = now
            };
            model.StorageContext.Items.Add(item);

            model.StorageContext.FieldValues.Add(
                ConstantsModel.CreateFieldValue(item.ID, SystemItemTypes.Task, "Due", DateTime.Today.Date.ToString("yyyy/MM/dd")));
            model.StorageContext.FieldValues.Add(
                ConstantsModel.CreateFieldValue(item.ID, SystemItemTypes.Task, "Details", "Tap the browse button below to discover more about Zaplify."));

            // create Lists folder
            folderUser = new FolderUser() 
                { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
            folder = new Folder() { ID = folderUser.FolderID, SortOrder = 2000,
                Name = "Lists", UserID = this.CurrentUser.ID,
                ItemTypeID = SystemItemTypes.ListItem, Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Groceries list
            item = new Item() { ID = Guid.NewGuid(),
                SortOrder = 3000, Name = "Groceries",
                FolderID = folder.ID, UserID = this.CurrentUser.ID,
                IsList = true, ItemTypeID = SystemItemTypes.ShoppingItem, ParentID = null,
                Created = now, LastModified = now
            };
            model.StorageContext.Items.Add(item);
            
            // create People folder
            folderUser = new FolderUser() 
                { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
            folder = new Folder() { ID = folderUser.FolderID, SortOrder = 3000,
                Name = "People", UserID = this.CurrentUser.ID,
                ItemTypeID = SystemItemTypes.Contact, Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Places folder
            folderUser = new FolderUser() 
                { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = this.CurrentUser.ID, PermissionID = Permissions.Full };
            folder = new Folder() { ID = folderUser.FolderID, SortOrder = 4000,
                Name = "Places", UserID = this.CurrentUser.ID,
                ItemTypeID = SystemItemTypes.Location, Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            model.StorageContext.SaveChanges();
        }
    }
}
