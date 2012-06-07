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
	public class ListEditor
	{
        private DialogViewController dvc;
        private Item list;
        private Item listCopy;
        private UINavigationController controller;
        private EntryElement ListName;
        private ItemTypePickerElement ItemTypePicker;
        private ParentListPickerElement ParentListPicker;

		public ListEditor(UINavigationController c, Folder f, Item l, Guid? parentID)
		{
			// trace event
            TraceHelper.AddMessage("ListEditor: constructor");
            controller = c;
            list = l;
            if (l == null)
            {
                // new list
                DateTime now = DateTime.UtcNow;
                listCopy = new Item()
                {
                    FolderID = f != null ? f.ID : Guid.Empty,
                    ParentID = parentID ?? Guid.Empty,
                    IsList = true,
                    ItemTypeID = f != null ? f.ItemTypeID : SystemItemTypes.Task,
                    Created = now,
                    LastModified = now
                };
            }
            else
                listCopy = new Item(list);
		}
        
        public void PushViewController()
        {
            // trace event
            TraceHelper.AddMessage("ListEditor: PushViewController");
            
            InitializeComponent();
        }
        
        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // navigate back
            NavigateBack();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new list, delete just does the same thing as cancel
            if (list == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show(String.Format("delete {0}?", list.Name), "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = list
                });

            // reobtain the current folder (it may have been replaced by a GetUser operations since the ListEditor was invoked)
            Folder currentFolder = App.ViewModel.LoadFolder(list.FolderID);
            
            // remove the item from the viewmodel
            list = currentFolder.Items.Single(i => i.ID == list.ID);
            currentFolder.Items.Remove(list);

            // save the changes to local storage
            StorageHelper.WriteFolder(currentFolder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // Pop twice and navigate back to the list page
            controller.PopViewControllerAnimated(false);
            NavigateBack();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // get the name of the list
            listCopy.Name = ListName.Value;

            // get parent list
            listCopy.FolderID = ParentListPicker.SelectedFolderID;
            listCopy.ParentID = ParentListPicker.SelectedParentID;

            // get item type
            listCopy.ItemTypeID = ItemTypePicker.SelectedItemType;

            // check for appropriate values
            if (listCopy.Name == "")
            {
                MessageBox.Show("name cannot be empty");
                return;
            }
   
            // get a reference to the folder of the new or existing list
            Folder currentFolder = App.ViewModel.LoadFolder(listCopy.FolderID);
            
            // if this is a new list, create it
            if (list == null)
            {
                // figure out the sort value 
                float sortOrder = 1000f;
                var listItems = currentFolder.Items.Where(it => it.ParentID == listCopy.ParentID).ToList();
                if (listItems.Count > 0)
                    sortOrder += listItems.Max(it => it.SortOrder);
                listCopy.SortOrder = sortOrder;

                // enqueue the Web Request Record (with a new copy of the list)
                // need to create a copy because otherwise other items may be added to it
                // and we want the record to have exactly one operation in it (create the list)
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = new Item(listCopy)
                    });

                // add the item to the local Folder
                currentFolder.Items.Add(listCopy);
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { list, listCopy },
                        BodyTypeName = "Item",
                        ID = list.ID
                    });

                // save the changes to the existing list (make a deep copy)
                list.Copy(listCopy, true);
                
                // save the new list properties back to the item in the folder
                var item = currentFolder.Items.Single(i => i.ID == list.ID);
                item.Name = list.Name;
                item.ItemTypeID = list.ItemTypeID;
                item.ParentID = list.ParentID;
                item.FolderID = list.FolderID;
            }

            // save the changes to local storage
            StorageHelper.WriteFolder(currentFolder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // Navigate back to the main page
            NavigateBack();
        }

        #endregion Event Handlers
        
        #region Helpers

        private void InitializeComponent()
        {
            // initialize controls 
            ListName = new EntryElement("Name", "", listCopy.Name);

            // set up the parent list picker
            ParentListPicker = new ParentListPickerElement("Parent", list);

            // set up the item type picker
            ItemTypePicker = new ItemTypePickerElement("Type", listCopy.ItemTypeID);

            var root = new RootElement("List Properties")
            {
                new Section()
                {
                    ListName,
                    ParentListPicker,
                    ItemTypePicker,
                    //list == null ? ListCheckbox : null
                }
            };

            // if this isn't a new list, render the delete button
            if (list != null)
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
                dvc.Title = NSBundle.MainBundle.LocalizedString ("List Properties", "List Properties");
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

