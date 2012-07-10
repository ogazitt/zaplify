using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class FolderEditor
	{
        private DialogViewController dvc;
        private Folder folder;
        private Folder folderCopy;
        private UINavigationController controller;
        private EntryElement ListName;
        private ItemTypePickerElement ItemTypePicker;

		public FolderEditor(UINavigationController c, Folder f)
		{
			// trace event
            TraceHelper.AddMessage("FolderEditor: constructor");
            controller = c;
            folder = f;
            if (f == null)
                folderCopy = new Folder();
            else
                folderCopy = new Folder(folder);
		}
        
        public void PushViewController()
        {
            // trace event
            TraceHelper.AddMessage("FolderEditor: PushViewController");
            
            InitializeComponent();
        }
        
        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("FolderEditor: Navigate back");

            // navigate back
            NavigateBack();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new folder, delete just does the same thing as cancel
            if (folder == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this folder?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = folder
                });

            // remove the item from the viewmodel
            App.ViewModel.Folders.Remove(folder);
            App.ViewModel.FolderDictionary.Remove(folder.ID);

            // save the changes to local storage
            StorageHelper.WriteFolders(App.ViewModel.Folders);
            StorageHelper.DeleteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("FolderEditor: Navigate back");

            // Pop twice and navigate back to the folder page
            controller.PopViewControllerAnimated(false);
            NavigateBack();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // get the property values
            folderCopy.Name = ListName.Value;
            folderCopy.ItemTypeID = ItemTypePicker.SelectedItemType;

            // check for appropriate values
            if (folderCopy.Name == "")
            {
                MessageBox.Show("folder name cannot be empty");
                return;
            }

            // if this is a new folder, create it
            if (folder == null)
            {
                folder = folderCopy;

                // figure out the sort value 
                float sortOrder = 1000f;
                if (App.ViewModel.Folders.Count > 0)
                    sortOrder += App.ViewModel.Folders.Max(f => f.SortOrder);
                folder.SortOrder = sortOrder;

                // enqueue the Web Request Record (with a new copy of the folder)
                // need to create a copy because otherwise other items may be added to it
                // and we want the record to have exactly one operation in it (create the folder)
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = new Folder(folder)
                    });

                // add the item to the local itemType
                App.ViewModel.Folders.Add(folder);
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Folder>() { folder, folderCopy },
                        BodyTypeName = "Folder",
                        ID = folder.ID
                    });

                // save the changes to the existing folder (make a deep copy)
                folder.Copy(folderCopy, true);
            }

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);
            StorageHelper.WriteFolders(App.ViewModel.Folders);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("FolderEditor: Navigate back");

            // Navigate back to the main page
            NavigateBack();
        }

        #endregion Event Handlers
        
        #region Helpers

        private void InitializeComponent()
        {
            // initialize controls 
            ListName = new EntryElement("Name", "", folderCopy.Name);
            
            // set up the item type listpicker
            ItemTypePicker = new ItemTypePickerElement("Folder Type", folderCopy.ItemTypeID);

            var root = new RootElement("Folder Properties")
            {
                new Section()
                {
                    ListName,
                    ItemTypePicker
                }
            };

            // if this isn't a new folder, render the delete button
            if (folder != null)
            {
                var actionButtons = new ButtonListElement()
                {
                    new Button() { Caption = "Delete", Background = "Images/redbutton.png", Clicked = DeleteButton_Click }, 
                };
                actionButtons.Margin = 0f;
                root.Add(new Section() { actionButtons });
            }           

            if (dvc == null)
            {
                // create and push the dialog view onto the nav stack
                dvc = new DialogViewController(UITableViewStyle.Grouped, root);
                dvc.Title = NSBundle.MainBundle.LocalizedString ("Folder Properties", "Folder Properties");
                dvc.NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Done, delegate {
                    // save the item and trigger a sync with the service  
                    SaveButton_Click(null, null);
                });
                dvc.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel, delegate { NavigateBack(); });
                dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);                
                controller.PushViewController(dvc, true);
            }
            else
            {
                // refresh the dialog view controller with the new root
                var oldroot = dvc.Root;
                dvc.Root = root;
                oldroot.Dispose();
                dvc.ReloadData();
            }
        }

        private void NavigateBack()
        {
            controller.PopViewControllerAnimated(true);
        }

        #endregion Helpers
	}    
}

