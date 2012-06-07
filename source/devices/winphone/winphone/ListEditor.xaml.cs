using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class ListEditor : PhoneApplicationPage
    {
        private Folder folder;
        private Item list;
        private Item listCopy;
        
        public ListEditor()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("ListEditor: constructor");

            ConnectedIconImage.DataContext = App.ViewModel;
            LayoutRoot.DataContext = App.ViewModel;

            // enable tabbing
            this.IsTabStop = true;

            this.Loaded += new RoutedEventHandler(ListEditor_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(ListEditor_BackKeyPress);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("ListEditor: OnNavigatedTo");

            if (e.NavigationMode == NavigationMode.Back)
                return;

            string folderIDString = "";
            string listIDString = "";

            if (NavigationContext.QueryString.TryGetValue("FolderID", out folderIDString) == false)
            {
                TraceHelper.AddMessage("ListEditor: no folder ID passed in");
                NavigationService.GoBack();
                return;
            }

            Guid folderID = new Guid(folderIDString);
            folder = App.ViewModel.Folders.Single<Folder>(f => f.ID == folderID);

            if (NavigationContext.QueryString.TryGetValue("ID", out listIDString))
            {
                if (listIDString == "new")
                {
                    string parentIDString = "";
                    if (NavigationContext.QueryString.TryGetValue("ParentID", out parentIDString) == false)
                    {
                        TraceHelper.AddMessage("ListEditor: no parent ID passed in");
                        NavigationService.GoBack();
                        return;
                    }

                    // new list
                    DateTime now = DateTime.UtcNow;
                    Guid? parentID = String.IsNullOrEmpty(parentIDString) ? (Guid?)null : new Guid(parentIDString);
                    Item parent = parentID != null ? App.ViewModel.Items.FirstOrDefault(i => i.ParentID == parentID) : null;

                    listCopy = new Item()
                    {
                        FolderID = folderID,
                        ParentID = String.IsNullOrEmpty(parentIDString) ? (Guid?)null : new Guid(parentIDString),
                        IsList = true,
                        ItemTypeID = parent != null ? parent.ItemTypeID : folder.ItemTypeID,
                        Created = now,
                        LastModified = now
                    };
                    TitlePanel.DataContext = listCopy;
                }
                else
                {
                    Guid listID = new Guid(listIDString);
                    list = folder.Items.Single<Item>(l => l.ID == listID);

                    // make a deep copy of the item for local binding
                    listCopy = new Item(list);
                    TitlePanel.DataContext = listCopy;

                    // add the delete button to the ApplicationBar
                    var button = new ApplicationBarIconButton() { Text = "Delete", IconUri = new Uri("/Images/appbar.delete.rest.png", UriKind.Relative) };
                    button.Click += new EventHandler(DeleteButton_Click);

                    // insert after the save button but before the cancel button
                    ApplicationBar.Buttons.Add(button);
                }

                // set up the item type listpicker
                var itemTypes = App.ViewModel.ItemTypes.Where(i => i.UserID != SystemUsers.System).OrderBy(i => i.Name).ToList();
                ItemTypePicker.ItemsSource = itemTypes;
                ItemTypePicker.DisplayMemberPath = "Name";
                ItemType thisItemType = itemTypes.FirstOrDefault(i => i.ID == listCopy.ItemTypeID);
                ItemTypePicker.SelectedIndex = Math.Max(itemTypes.IndexOf(thisItemType), 0);
            }
        }

        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
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
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
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

            // Navigate back to the main page
            NavigationService.GoBack();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // get the name of the list
            listCopy.Name = ListName.Text;
            var itemType = ItemTypePicker.SelectedItem as ItemType;
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
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
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
            NavigationService.GoBack();
        }

        void ListEditor_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void ListEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("ListEditor: Loaded");
        }

        #endregion
    }
}
