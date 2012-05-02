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
        private Folder folder;
        private Item list;
        private Item listCopy;
        private UINavigationController controller;
        private EntryElement ListName;
        private RootElement ItemTypePicker;
        private List<ItemType> ItemTypes;
        
		public ListEditor(UINavigationController c, Folder f, Item l, Guid? parentID)
		{
			// trace event
            TraceHelper.AddMessage("ListEditor: constructor");
            controller = c;
            folder = f;
            list = l;
            if (l == null)
            {
                // new list
                DateTime now = DateTime.UtcNow;
                listCopy = new Item()
                {
                    FolderID = f.ID,
                    ParentID = parentID ?? Guid.Empty,
                    IsList = true,
                    ItemTypeID = folder.ItemTypeID,
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

            MessageBoxResult result = MessageBox.Show("delete this list?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = list
                });

            // remove the item from the viewmodel
            folder.Items.Remove(list);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

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
            ListName.FetchValue();
            listCopy.Name = ListName.Value;
            var index = ItemTypePicker.RadioSelected;
            var itemType = index >= 0 && index < ItemTypes.Count ? ItemTypes[index] : null;
            listCopy.ItemTypeID = itemType != null ? itemType.ID : SystemItemTypes.Task;

            // check for appropriate values
            if (listCopy.Name == "")
            {
                MessageBox.Show("list name cannot be empty");
                return;
            }

            // if this is a new list, create it
            if (list == null)
            {
                // figure out the sort value 
                float sortOrder = 1000f;
                var listItems = folder.Items.Where(it => it.ParentID == listCopy.ParentID).ToList();
                if (listItems.Count > 0)
                    sortOrder += listItems.Max(it => it.SortOrder);
                listCopy.SortOrder = sortOrder;

                // enqueue the Web Request Record (with a new copy of the list)
                // need to create a copy because otherwise other items may be added to it
                // and we want the record to have exactly one operation in it (create the list)
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = new Item(listCopy)
                    });

                // add the item to the local itemType
                folder.Items.Add(listCopy);
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
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
                var item = folder.Items.Single(i => i.ID == list.ID);
                item.Name = list.Name;
                item.ItemTypeID = list.ItemTypeID;
            }

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

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
            
            // set up the item type listpicker
            ItemTypes = App.ViewModel.ItemTypes.Where(i => i.UserID != SystemUsers.System).OrderBy(i => i.Name).ToList();
            ItemType thisItemType = ItemTypes.FirstOrDefault(i => i.ID == listCopy.ItemTypeID);
            int selectedIndex = Math.Max(ItemTypes.IndexOf(thisItemType), 0);
            var itemTypeSection = new Section();
            itemTypeSection.AddAll(from it in ItemTypes select (Element) new RadioElement(it.Name));
            ItemTypePicker = new RootElement("List Type", new RadioGroup(selectedIndex)) { itemTypeSection };

            var root = new RootElement("List Editor")
            {
                new Section()
                {
                    ListName,
                    ItemTypePicker,
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
                dvc.Title = NSBundle.MainBundle.LocalizedString ("List Editor", "List Editor");
                dvc.NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Done, delegate {
                    // save the item and trigger a sync with the service  
                    SaveButton_Click(null, null);
                });
                dvc.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel, delegate { NavigateBack(); });
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

