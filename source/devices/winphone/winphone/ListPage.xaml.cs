﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Collections.ObjectModel;
using BuiltSteady.Zaplify.Devices.Utilities;
using System.Windows.Data;
using System.ComponentModel;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Net.NetworkInformation;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class ListPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private const int rendersize = 10;  // limit of elements to render immediately
        private bool constructorCalled = false;
        private Folder folder;
        private Item list;
        private ListHelper ListHelper;
        private Tag tag;

        private SpeechHelper.SpeechState speechState;
        private string speechDebugString = null;
        private DateTime speechStart;

        // ViewSource for the Folder collection for Import List (used for filtering out non-template folders)
        public CollectionViewSource ImportListViewSource { get; set; }

        private Visibility networkOperationInProgress = Visibility.Collapsed;
        /// <summary>
        /// Whether a network operation is in progress (yes == Visible / no == Collapsed)
        /// </summary>
        /// <returns></returns>
        public Visibility NetworkOperationInProgress
        {
            get
            {
                return networkOperationInProgress;
            }
            set
            {
                if (value != networkOperationInProgress)
                {
                    networkOperationInProgress = value;
                    NotifyPropertyChanged("NetworkOperationInProgress");
                }
            }
        }

        private bool speechButtonEnabled = false;
        /// <summary>
        /// Speech button enabled
        /// </summary>
        /// <returns></returns>
        public bool SpeechButtonEnabled
        {
            get
            {
                return speechButtonEnabled;
            }
            set
            {
                if (value != speechButtonEnabled)
                {
                    speechButtonEnabled = value;
                    NotifyPropertyChanged("SpeechButtonEnabled");
                }
            }
        }

        private string speechButtonText = "done";
        /// <summary>
        /// Speech button text
        /// </summary>
        /// <returns></returns>
        public string SpeechButtonText
        {
            get
            {
                return speechButtonText;
            }
            set
            {
                if (value != speechButtonText)
                {
                    speechButtonText = value;
                    NotifyPropertyChanged("SpeechButtonText");
                }
            }
        }

        private string speechCancelButtonText = "cancel";
        /// <summary>
        /// Speech cancel button text
        /// </summary>
        /// <returns></returns>
        public string SpeechCancelButtonText
        {
            get
            {
                return speechCancelButtonText;
            }
            set
            {
                if (value != speechCancelButtonText)
                {
                    speechCancelButtonText = value;
                    NotifyPropertyChanged("SpeechCancelButtonText");
                }
            }
        }

        private string speechLabelText = "initializing...";
        /// <summary>
        /// Speech button text
        /// </summary>
        /// <returns></returns>
        public string SpeechLabelText
        {
            get
            {
                return speechLabelText;
            }
            set
            {
                if (value != speechLabelText)
                {
                    speechLabelText = value;
                    NotifyPropertyChanged("SpeechLabelText");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                //handler(this, new PropertyChangedEventArgs(propertyName));
                // do the below instead to avoid Invalid cross-thread access exception
                Deployment.Current.Dispatcher.BeginInvoke(() => { handler(this, new PropertyChangedEventArgs(propertyName)); });
            }
        }

        // Constructor
        public ListPage()
        {
            InitializeComponent();

            // trace data
            TraceHelper.AddMessage("ListPage: constructor");

            // set some data context information
            ConnectedIconImage.DataContext = App.ViewModel;
            SpeechProgressBar.DataContext = App.ViewModel;
            QuickAddPopup.DataContext = App.ViewModel;

            // set some data context information for the speech UI
            SpeechPopup_SpeakButton.DataContext = this;
            SpeechPopup_CancelButton.DataContext = this;
            SpeechLabel.DataContext = this;

            ImportListViewSource = new CollectionViewSource();
            ImportListViewSource.Filter += new FilterEventHandler(ImportList_Filter);

            // add some event handlers
            Loaded += new RoutedEventHandler(ListPage_Loaded);
            BackKeyPress += new EventHandler<CancelEventArgs>(ListPage_BackKeyPress);

            // set the constructor called flag
            constructorCalled = true;

            // trace data
            TraceHelper.AddMessage("Exiting ListPage constructor");
        }

        // When page is navigated to set data context to selected item in itemType
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace data
            TraceHelper.AddMessage("ListPage: OnNavigatedTo");

            string IDString = "";
            string typeString = "";
            Guid id;

            // get the type of list to display
            if (NavigationContext.QueryString.TryGetValue("type", out typeString) == false)
            {
                // trace page navigation
                TraceHelper.StartMessage("ListPage: Navigate back");

                // navigate back
                NavigationService.GoBack();
                return;
            }

            // get the ID of the object to display
            if (NavigationContext.QueryString.TryGetValue("ID", out IDString) == false)
            {
                // trace page navigation
                TraceHelper.StartMessage("ListPage: Navigate back");

                // navigate back
                NavigationService.GoBack();
                return;
            }

            // get the ID
            id = new Guid(IDString);

            switch (typeString)
            {
                case "Folder":
                    // get the folder and make it the datacontext
                    try
                    {
                        folder = App.ViewModel.LoadFolder(id);

                        // if the load failed, this folder has been deleted
                        if (folder == null)
                        {
                            // the folder isn't found - this can happen when the folder we were just 
                            // editing was removed in FolderEditor, which then goes back to ListPage.
                            // this will send us back to the MainPage which is appropriate.

                            // trace page navigation
                            TraceHelper.StartMessage("ListPage: Navigate back");

                            // navigate back
                            NavigationService.GoBack();
                            return;
                        }

                        // get the ID of the list to display
                        if (NavigationContext.QueryString.TryGetValue("ParentID", out IDString) == false)
                        {
                            // trace page navigation
                            TraceHelper.StartMessage("ListPage: Navigate back");

                            // navigate back
                            NavigationService.GoBack();
                            return;
                        }

                        // get the ID
                        id = new Guid(IDString);

                        // get the current list name
                        string listName = null;
                        if (id == Guid.Empty)
                            listName = folder.Name;
                        else
                            listName = folder.Items.Single(i => i.ID == id).Name;

                        // construct a synthetic item that represents the list of items for which the 
                        // ParentID is the parent.  this also works for the root list in a folder, which
                        // is represented with a ParentID of Guid.Empty.
                        list = new Item()
                        {
                            ID = id,
                            Name = listName,
                            FolderID = folder.ID,
                            IsList = true,
                            Items = folder.Items.Where(i => i.ParentID == id).ToObservableCollection()
                        };
                    }
                    catch (Exception ex)
                    {
                        // the folder isn't found - this can happen when the folder we were just 
                        // editing was removed in FolderEditor, which then goes back to ListPage.
                        // this will send us back to the MainPage which is appropriate.

                        // trace page navigation
                        TraceHelper.StartMessage(String.Format("ListPage: Navigate back (exception: {0})", ex.Message));

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }
                    break;
                case "Tag":
                    // create a filter 
                    try
                    {
                        tag = App.ViewModel.Tags.Single(t => t.ID == id);
                        
                        // construct a synthetic item that represents the list of items which 
                        // have this tag.  
                        list = new Item()
                        {
                            ID = Guid.Empty, 
                            Name = String.Format("items with {0} tag", tag.Name), 
                            Items = App.ViewModel.Items.Where(t => t.ItemTags.Any(tg => tg.TagID == tag.ID)).ToObservableCollection()
                        };
                    }
                    catch (Exception)
                    {
                        // the tag isn't found - this can happen when the tag we were just 
                        // editing was removed in TagEditor, which then goes back to ListPage.
                        // this will send us back to the MainPage which is appropriate.

                        // trace page navigation
                        TraceHelper.StartMessage("ListPage: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }
                    break;
                default:
                    // trace page navigation
                    TraceHelper.StartMessage("ListPage: Navigate back");

                    // navigate back
                    NavigationService.GoBack();
                    return;
            }

            // set datacontext 
            DataContext = list;

            // create the ListHelper
            ListHelper = new BuiltSteady.Zaplify.Devices.WinPhone.ListHelper(
                list, 
                new RoutedEventHandler(CompleteCheckbox_Click), 
                new RoutedEventHandler(Tag_HyperlinkButton_Click));

            // store the current listbox and ordering
            PivotItem item = (PivotItem) PivotControl.Items[PivotControl.SelectedIndex];
            ListHelper.ListBox = (ListBox)((Grid)item.Content).Children[1];
            ListHelper.OrderBy = (string)item.Header;

            // trace data
            TraceHelper.AddMessage("Exiting ListPage OnNavigatedTo");
        }

        #region Event Handlers

        private void AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListPage: Navigate to Item");

            // Navigate to the new page
            NavigationService.Navigate(
                new Uri(String.Format("/ItemPage.xaml?ID={0}&folderID={1}", "new", folder.ID),
                UriKind.Relative));
        }

        /// <summary>
        /// Handle click event on Complete checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            // trace data
            TraceHelper.StartMessage("CompleteCheckbox Click");

            CheckBox cb = (CheckBox)e.OriginalSource;
            Guid itemID = (Guid)cb.Tag;

            // get the item that was just updated, and ensure the Complete flag is in the correct state
            Item item = folder.Items.Single<Item>(t => t.ID == itemID);

            // create a copy of that item
            Item itemCopy = new Item(item);

            // toggle the complete flag to reflect the checkbox click
            item.Complete = !item.Complete;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { itemCopy, item },
                    BodyTypeName = "Item",
                    ID = item.ID
                });
            
            // reorder the item in the folder and the ListBox
            ListHelper.ReOrderItem(list, item);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace data
            TraceHelper.AddMessage("Finished CompleteCheckbox Click");
        }

        private void DeleteCompletedMenuItem_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("delete all completed items in this folder?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // create a copy of the list to foreach over.  this is because we can't delete
            // from the original collection while it's being enumerated.  the copy we make is shallow 
            // so as not to create brand new Item objects, but then we add all the item references to 
            // an new Items collection that won't interfere with the existing one.
            Item itemlist = new Item(list, false);
            itemlist.Items = new ObservableCollection<Item>();
            foreach (Item t in list.Items)
                itemlist.Items.Add(t);

            // remove any completed items from the original list
            foreach (var item in itemlist.Items)
            {
                if (item.Complete == true)
                {
                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = item
                        });

                    // remove the item from the original collection and from ListBox
                    ListHelper.RemoveItem(list, item);

                    // remove the item from the folder
                    folder.Items.Remove(item);
                }
            }

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (folder.ID != Guid.Empty)
            {
                // trace page navigation
                TraceHelper.StartMessage("ListPage: Navigate to FolderEditor");

                // Navigate to the FolderEditor page
                NavigationService.Navigate(
                    new Uri(String.Format("/FolderEditor.xaml?ID={0}", folder.ID),
                    UriKind.Relative));
            }
            else
            {
                // trace page navigation
                TraceHelper.StartMessage("ListPage: Navigate to TagEditor");

                // Navigate to the TagEditor page
                NavigationService.Navigate(
                    new Uri(String.Format("/TagEditor.xaml?ID={0}", tag.ID),
                    UriKind.Relative));
            }
        }

        // handle events associated with Import List

        private void ImportList_Filter(object sender, FilterEventArgs e)
        {
            Item i = e.Item as Item;
            e.Accepted = i.IsList;
        }

        private void ImportListMenuItem_Click(object sender, EventArgs e)
        {
            // set the collection source for the import template folder picker
            ImportListViewSource.Source = App.ViewModel.Items;
            ImportListPopupListPicker.DataContext = this;

            // open the popup, disable folder selection bug
            ImportListPopup.IsOpen = true;
        }

        private void ImportListPopup_ImportButton_Click(object sender, RoutedEventArgs e)
        {
            Folder tl = ImportListPopupListPicker.SelectedItem as Folder;
            if (tl == null)
                return;

            // add the items in the template to the existing folder
            foreach (Item t in tl.Items)
            {
                DateTime now = DateTime.UtcNow;

                // create the new item
                Item item = new Item(t) { ID = Guid.NewGuid(), FolderID = folder.ID, Created = now, LastModified = now };
                // recreate the itemtags (they must be unique)
                if (item.ItemTags != null && item.ItemTags.Count > 0)
                {
                    foreach (var tt in item.ItemTags)
                    {
                        tt.ID = Guid.NewGuid();
                    }
                }

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = item
                    });

                // add the item to the local collection
                // note that we don't use ListHelper.AddItem() here because it is typically more efficient
                // to add all the items and then re-render the entire folder.  This is because 
                // the typical use case is to import a template into an empty (or nearly empty) folder.
                folder.Items.Add(item);
            }

            // render the new folder 
            ListHelper.RenderList(list);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // close the popup 
            ImportListPopup.IsOpen = false;
        }

        private void ImportListPopup_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // close the popup 
            ImportListPopup.IsOpen = false;
        }

        // Handle selection changed on ListBox
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            // If selected index is -1 (no selection) do nothing
            if (listBox.SelectedIndex == -1)
                return;

            // get the item associated with this click
            Item item = null;

            // retrieve the current selection
            ListBoxItem lbi = listBox.SelectedItem as ListBoxItem;
            if (lbi != null)
                item = lbi.Tag as Item;

            // if there is no item, return without processing the event
            if (item == null)
                return;

            // trace page navigation
            TraceHelper.StartMessage("ListPage: Navigate to Item");

            if (item.IsList == true)
            {
                // Navigate to the list page
                NavigationService.Navigate(new Uri(
                    String.Format(
                        "/ListPage.xaml?type=Folder&ID={0}&ParentID={1}",
                        item.FolderID,
                        item.ID),
                    UriKind.Relative));
            }
            else
            {
                // Navigate to the item page
                NavigationService.Navigate(
                    new Uri(String.Format("/ItemPage.xaml?ID={0}&folderID={1}", item.ID, item.FolderID),
                    UriKind.Relative));
            }

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        // handle ListPage events
        void ListPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListPage: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void ListPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace page navigation
            TraceHelper.AddMessage("ListPage: Loaded");

            // reset the constructor flag
            constructorCalled = false;

            // create the control tree and render the folder
            ListHelper.RenderList(list);

            // trace page navigation
            TraceHelper.AddMessage("Finished ListPage Loaded");
        }

        // handle events associated with the Folders button
        private void FoldersButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListPage: Navigate to Main");

            // Navigate to the main page
            NavigationService.Navigate(
                new Uri("/MainPage.xaml?Tab=Folders", UriKind.Relative));
        }

        private void PivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // store the current listbox
            ListHelper.ListBox = (ListBox)((Grid)((PivotItem)PivotControl.SelectedItem).Content).Children[1];
            ListHelper.OrderBy = (string)((PivotItem)PivotControl.SelectedItem).Header;

            // the pivot control's selection changed event gets called during the initialization of a new
            // page.  since we do rendering in the Loaded event handler, we need to skip rendering here
            // so that we don't do it twice and slow down the loading of the page.
            if (constructorCalled == false)
            {
                ListHelper.RenderList(list);
            }
        }

        // handle events associated with the Quick Add Popup
        private void QuickAddButton_Click(object sender, EventArgs e)
        {
            RenderItemTypes();

            // open the popup, disable folder selection bug, and transfer focus to the popup text box
            QuickAddPopup.IsOpen = true;
            QuickAddPopupTextBox.Focus();
        }

        private void QuickAddPopup_AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = QuickAddPopupTextBox.Text;
            // don't add empty items
            if (name == null || name == "")
                return;

            int itemTypeIndex = QuickAddPopupItemTypePicker.SelectedIndex;
            if (itemTypeIndex < 0)
            {
                MessageBox.Show("item type must be set");
                return;
            }

            // get the value of the IsList checkbox
            bool isChecked = (QuickAddPopupIsListCheckbox.IsChecked == null) ? false : (bool) QuickAddPopupIsListCheckbox.IsChecked;

            // create the new item
            Item item = new Item()
            {
                Name = name,
                FolderID = folder.ID,
                ItemTypeID = App.ViewModel.ItemTypes[itemTypeIndex].ID,
                ParentID = list.ID,
                IsList = isChecked,
                LastModified = DateTime.UtcNow
            };

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = item
                });

            // add the new item to the list
            ListHelper.AddItem(list, item);

            // add the item to the folder
            folder.Items.Add(item);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // clear the textbox and focus back to it
            QuickAddPopupTextBox.Text = "";
            QuickAddPopupTextBox.Focus();
        }

        private void QuickAddPopup_DoneButton_Click(object sender, RoutedEventArgs e)
        {
            // close the popup 
            QuickAddPopup.IsOpen = false;
        }

        // handle events associated with the Speech Popup
        private void SpeechButton_Click(object sender, RoutedEventArgs e)
        {
            // require a connection
            if (DeviceNetworkInformation.IsNetworkAvailable == false ||
                NetworkInterface.GetIsNetworkAvailable() == false)
            {
                MessageBox.Show("apologies - a network connection is required for this feature, and you appear to be disconnected :-(");
                return;
            }
            
            // require an account
            if (App.ViewModel.User == null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "the speech feature requires an account.  create a free account now?",
                    "create account?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;

                // trace page navigation
                TraceHelper.StartMessage("ListPage: Navigate to Settings");

                // Navigate to the settings page
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
                return;
            }

            // set the UI state to initializing state
            speechState = SpeechHelper.SpeechState.Initializing;
            SpeechSetUIState(speechState);

            // store debug / timing info
            speechStart = DateTime.Now;
            speechDebugString = "";

            // store debug / timing info
            TimeSpan ts = DateTime.Now - speechStart;
            string stateString = SpeechHelper.SpeechStateString(speechState);
            string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Connecting Socket");
            TraceHelper.AddMessage(traceString);
            speechDebugString += traceString + "\n";

            // initialize the connection to the speech service
            SpeechHelper.Start(
                App.ViewModel.User,
                new SpeechHelper.SpeechStateCallbackDelegate(SpeechPopup_SpeechStateCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));

            // open the popup
            SpeechPopup.IsOpen = true;
        }

        private void SpeechPopup_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            switch (speechState)
            {
                case SpeechHelper.SpeechState.Initializing:
                case SpeechHelper.SpeechState.Listening:
                case SpeechHelper.SpeechState.Recognizing:
                    // user tapped the cancel button

                    // cancel the current operation / close the socket to the service
                    SpeechHelper.CancelStreamed(
                        new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));

                    // reset the text in the textbox
                    QuickAddPopupTextBox.Text = "";
                    break;
                case SpeechHelper.SpeechState.Finished:
                    // user tapped the OK button

                    // set the text in the popup textbox
                    QuickAddPopupTextBox.Text = SpeechLabelText.Trim('\'');
                    break;
            }
 
            SpeechPopup_Close();
        }

        private void SpeechPopup_Close()
        {
            // close the popup 
            SpeechPopup.IsOpen = false;
        }

        private void SpeechPopup_NetworkOperationInProgressCallBack(bool operationInProgress, bool? operationSuccessful)
        {
            // call the MainViewModel's routine to make sure global network status is reset
            App.ViewModel.NetworkOperationInProgressCallback(operationInProgress, operationSuccessful);

            // signal whether the net operation is in progress or not
            NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);

            // if the operationSuccessful flag is null, no new data; otherwise, it signals the status of the last operation
            if (operationSuccessful != null)
            {
                if ((bool)operationSuccessful == false)
                {
                    // the server wasn't reachable
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show("apologies - cannot reach the speech service at this time.");
                        SpeechPopup_Close();
                    });
                }
            }
        }

        private void SpeechPopup_SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan ts;
            string stateString;
            string traceString;

            switch (speechState)
            {
                case SpeechHelper.SpeechState.Initializing:
                    // can't happen since the button isn't enabled
#if DEBUG
                    MessageBox.Show("Invalid state SpeechState.Initializing reached");
#endif
                    break;
                case SpeechHelper.SpeechState.Listening:
                    // done button tapped

                    // set the UI state to recognizing state
                    speechState = SpeechHelper.SpeechState.Recognizing;
                    SpeechSetUIState(speechState);

                    // store debug / timing info
                    ts = DateTime.Now - speechStart;
                    stateString = SpeechHelper.SpeechStateString(speechState);
                    traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Stopping mic");
                    TraceHelper.AddMessage(traceString);
                    speechDebugString += traceString + "\n";

                    // stop listening and get the recognized text from the speech service
                    SpeechHelper.Stop(new SpeechHelper.SpeechToTextCallbackDelegate(SpeechPopup_SpeechToTextCallback)); 
                    break;
                case SpeechHelper.SpeechState.Recognizing:
                    // can't happen since the button isn't enabled
#if DEBUG
                    MessageBox.Show("Invalid state SpeechState.Initializing reached");
#endif
                    break;
                case SpeechHelper.SpeechState.Finished:
                    // "try again" button tapped

                    // set the UI state to initializing state
                    speechState = SpeechHelper.SpeechState.Initializing;
                    SpeechSetUIState(speechState);

                    // store debug / timing info
                    speechStart = DateTime.Now;
                    speechDebugString = "";

                    // store debug / timing info
                    ts = DateTime.Now - speechStart;
                    stateString = SpeechHelper.SpeechStateString(speechState);
                    traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Initializing Request");
                    TraceHelper.AddMessage(traceString);
                    speechDebugString += traceString + "\n";

                    // initialize the connection to the speech service
                    SpeechHelper.Start(
                        App.ViewModel.User,
                        new SpeechHelper.SpeechStateCallbackDelegate(SpeechPopup_SpeechStateCallback),
                        new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));
                    break;
            }
        }

        private void SpeechPopup_SpeechStateCallback(SpeechHelper.SpeechState state, string message)
        {
            speechState = state;
            SpeechSetUIState(speechState);

            // store debug / timing info
            TimeSpan ts = DateTime.Now - speechStart;
            string stateString = SpeechHelper.SpeechStateString(state);
            string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, message);
            TraceHelper.AddMessage(traceString);
            speechDebugString += traceString + "\n";
        }

        private void SpeechPopup_SpeechToTextCallback(string textString)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // set the UI state to finished state
                speechState = SpeechHelper.SpeechState.Finished;
                SpeechSetUIState(speechState);

                // store debug / timing info
                TimeSpan ts = DateTime.Now - speechStart;
                string stateString = SpeechHelper.SpeechStateString(speechState);
                string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, textString);
                TraceHelper.AddMessage(traceString);
                speechDebugString += traceString + "\n";

                // strip any timing / debug info 
                textString = textString == null ? "" : textString;
                string[] words = textString.Split(' ');
                if (words[words.Length - 1] == "seconds")
                {
                    textString = "";
                    // strip off last two words - "a.b seconds"
                    for (int i = 0; i < words.Length - 2; i++)
                    {
                        textString += words[i];
                        textString += " ";
                    }
                    textString = textString.Trim();
                }

                // set the speech label text as well as the popup text
                SpeechLabelText = textString == null ? "recognition failed" : String.Format("'{0}'", textString);
                QuickAddPopupTextBox.Text = textString;

#if DEBUG && KILL
                MessageBox.Show(speechDebugString);
#endif
            });
        }

        // event handlers related to tags

        private void Tag_HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton button = (HyperlinkButton)e.OriginalSource;
            Guid tagID = (Guid)button.Tag;

            // trace page navigation
            TraceHelper.StartMessage("ListPage: Navigate to Tag");

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
        }

        #endregion

        #region Helpers

        private void RenderItemTypes()
        {
            QuickAddPopupItemTypePicker.ItemsSource = App.ViewModel.ItemTypes;
            QuickAddPopupItemTypePicker.DisplayMemberPath = "Name";

            // set the selected index 
            if (list.ItemTypeID != null && list.ItemTypeID != Guid.Empty)
            {
                try
                {
                    ItemType itemType = App.ViewModel.ItemTypes.Single(lt => lt.ID == list.ItemTypeID);
                    int index = App.ViewModel.ItemTypes.IndexOf(itemType);
                    QuickAddPopupItemTypePicker.SelectedIndex = index;
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Set the UI based on the current state of the speech state machine
        /// </summary>
        /// <param name="state"></param>
        private void SpeechSetUIState(SpeechHelper.SpeechState state)
        {
            switch (state)
            {
                case SpeechHelper.SpeechState.Initializing:
                    SpeechLabelText = "initializing...";
                    SpeechButtonText = "done";
                    SpeechButtonEnabled = false;
                    SpeechCancelButtonText = "cancel";
                    break;
                case SpeechHelper.SpeechState.Listening:
                    SpeechLabelText = "listening...";
                    SpeechButtonText = "done";
                    SpeechButtonEnabled = true;
                    SpeechCancelButtonText = "cancel";
                    break;
                case SpeechHelper.SpeechState.Recognizing:
                    SpeechLabelText = "recognizing...";
                    SpeechButtonText = "try again";
                    SpeechButtonEnabled = false;
                    SpeechCancelButtonText = "cancel";
                    break;
                case SpeechHelper.SpeechState.Finished:
                    SpeechLabelText = "";
                    SpeechButtonText = "try again";
                    SpeechButtonEnabled = true;
                    SpeechCancelButtonText = "ok";
                    break;
            }
        }

        #endregion
    }
}