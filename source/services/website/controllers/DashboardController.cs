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
            catch
            {
                return RedirectToAction("SignOut", "Account");
            } 
            return RedirectToAction("Home", "Dashboard");
        }

        void CreateDefaultFolders(UserDataModel model)
        {
            Guid activitiesID = Guid.NewGuid();
            Guid peopleID = Guid.NewGuid();
            Guid placesID = Guid.NewGuid();
            Guid listsID = Guid.NewGuid();
            DateTime now = DateTime.Now;
            Folder folder;
            Item item;

            FolderUser folderUser = new FolderUser() 
                {  ID = Guid.NewGuid(), FolderID = activitiesID, UserID = this.CurrentUser.ID, PermissionID=Permissions.Full };

            // create Activities folder
            folder = new Folder() { ID = activitiesID, SortOrder = 1000,
                Name = "Activities", UserID = this.CurrentUser.ID, 
                ItemTypeID = SystemItemTypes.Task, Items = new List<Item>(), 
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Lists folder
            folderUser.FolderID = listsID;
            folder = new Folder() { ID = listsID, SortOrder = 2000,
                Name = "Lists", UserID = this.CurrentUser.ID,
                ItemTypeID = SystemItemTypes.ListItem, Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create People folder
            folderUser.FolderID = peopleID;
            folder = new Folder() { ID = peopleID, SortOrder = 3000,
                Name = "People", UserID = this.CurrentUser.ID,
                ItemTypeID = SystemItemTypes.Contact, Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);

            // create Places folder
            folderUser.FolderID = placesID;
            folder = new Folder() { ID = placesID, SortOrder = 4000,
                Name = "Places", UserID = this.CurrentUser.ID,
                ItemTypeID = SystemItemTypes.Location, Items = new List<Item>(),
                FolderUsers = new List<FolderUser>() { folderUser }
            };
            model.StorageContext.Folders.Add(folder);
            // save folders
            model.StorageContext.SaveChanges();

            model = new UserDataModel(Storage.NewContext, this.CurrentUser);

            // create Tasks list
            item = new Item() { ID = Guid.NewGuid(), SortOrder = 1000,
                Name = "Tasks", FolderID = activitiesID, UserID = this.CurrentUser.ID,
                IsList = true, ItemTypeID = SystemItemTypes.Task, ParentID = null,
                Created = now, LastModified = now 
            };
            model.StorageContext.Items.Add(item);

            // create Learn Zaplify task
            item = new Item() { ID = Guid.NewGuid(), SortOrder = 2000,
                Name = "Learn about Zaplify!", FolderID = activitiesID, UserID = this.CurrentUser.ID,
                IsList = false, ItemTypeID = SystemItemTypes.Task, ParentID = item.ID, 
                FieldValues = new List<FieldValue>(),
                Created = now, LastModified = now
            };
            model.StorageContext.Items.Add(item);
            
            model.StorageContext.FieldValues.Add(
                ConstantsModel.CreateFieldValue(item.ID, SystemItemTypes.Task, "Due", DateTime.Today.Date.ToString("yyyy/MM/dd")));
            model.StorageContext.FieldValues.Add(
                ConstantsModel.CreateFieldValue(item.ID, SystemItemTypes.Task, "Details", "Tap the browse button below to discover more about Zaplify."));

            // create Groceries list
            item = new Item() { ID = Guid.NewGuid(), SortOrder = 3000,
                Name = "Groceries", FolderID = listsID, UserID = this.CurrentUser.ID,
                IsList = true, ItemTypeID = SystemItemTypes.ShoppingItem, ParentID = null,
                Created = now, LastModified = now
            };
            model.StorageContext.Items.Add(item);

            model.StorageContext.SaveChanges();
        }
    }
}
