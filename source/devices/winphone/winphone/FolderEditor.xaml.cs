﻿using System;
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
    public partial class FolderEditor : PhoneApplicationPage
    {
        private Folder folder;
        private Folder folderCopy;

        public FolderEditor()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("FolderEditor: constructor");

            ConnectedIconImage.DataContext = App.ViewModel;
            LayoutRoot.DataContext = App.ViewModel;

            // enable tabbing
            this.IsTabStop = true;

            this.Loaded += new RoutedEventHandler(FolderEditor_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(FolderEditor_BackKeyPress);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("FolderEditor: OnNavigatedTo");

            if (e.NavigationMode == NavigationMode.Back)
                return;

            string folderIDString = "";

            if (NavigationContext.QueryString.TryGetValue("ID", out folderIDString))
            {
                if (folderIDString == "new")
                {
                    // new folder
                    folderCopy = new Folder();
                    TitlePanel.DataContext = folderCopy;
                }
                else
                {
                    Guid folderID = new Guid(folderIDString);
                    folder = App.ViewModel.Folders.Single<Folder>(tl => tl.ID == folderID);

                    // make a deep copy of the item for local binding
                    folderCopy = new Folder(folder);
                    TitlePanel.DataContext = folderCopy;

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
                ItemType thisItemType = itemTypes.FirstOrDefault(i => i.ID == folderCopy.ItemTypeID);
                ItemTypePicker.SelectedIndex = Math.Max(itemTypes.IndexOf(thisItemType), 0);
            }
        }

        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("FolderEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
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

            // Navigate back to the main page
            NavigationService.GoBack();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // get the name of the folder
            folderCopy.Name = ListName.Text;
            var itemType = ItemTypePicker.SelectedItem as ItemType;
            folderCopy.ItemTypeID = itemType != null ? itemType.ID : SystemItemTypes.Task;

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
            NavigationService.GoBack();
        }

        void FolderEditor_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("FolderEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void FolderEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("FolderEditor: Loaded");
        }

        #endregion
    }
}
