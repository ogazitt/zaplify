﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Net.NetworkInformation;
using BuiltSteady.Zaplify.Devices.ClientViewModels;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        // number of lists to generate "Add" buttons for
        const int MaxLists = 4;
        private StackPanel[] AddButtons = new StackPanel[3];
        private List<ClientEntity> lists;
        private List<Button> buttonList;

        // speech fields
        private SpeechHelper.SpeechState speechState;
        private string speechDebugString = null;
        private DateTime speechStart;

        private bool addedItemsPropertyChangedHandler = false;
        private bool initialSyncAlreadyHappened = false;
        Item list;
        ScheduleHelper ScheduleHelper;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Main: constructor");

            // Set the data context of the page to the main view model
            DataContext = App.ViewModel;

            // set the data context of a few controls to this page
            SearchHeader.DataContext = this;
            MoreListsListBox.DataContext = this;
            ListsHeader.DataContext = this;

            // set some data context information for the speech UI
            SpeechProgressBar.DataContext = this;
            SpeechPopup_SpeakButton.DataContext = this;
            SpeechPopup_CancelButton.DataContext = this;
            SpeechLabel.DataContext = this;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(MainPage_BackKeyPress);
            NameTextBox.TextChanged += new TextChangedEventHandler(delegate { NotifyPropertyChanged("ListsHeaderText"); });
        }

        public string ListsHeaderText 
        {
            get
            {
                if (String.IsNullOrWhiteSpace(NameTextBox.Text))
                    return "Choose list to navigate to:";
                else
                    return "Choose list to add to:";
            }
        }

        /// <summary>
        /// A list of all the lists under all the folders
        /// </summary>
        public ObservableCollection<Item> MoreLists
        {
            get
            {
                // create the hierarchy of folders/lists for the list picker
                var moreLists = new ObservableCollection<Item>();
                if (App.ViewModel.Folders != null)
                {
                    foreach (Folder f in App.ViewModel.Folders)
                    {
                        moreLists.Add(new Item() { ID = Guid.Empty, Name = f.Name, FolderID = f.ID, ItemTypeID = f.ItemTypeID });
                        var lists = App.ViewModel.Items.
                            Where(i => i.FolderID == f.ID && i.IsList == true && i.ItemTypeID != SystemItemTypes.Reference).
                            OrderBy(i => i.Name).
                            Select(i => new Item() { ID = i.ID, Name = "    " + i.Name, FolderID = i.FolderID, ItemTypeID = i.ItemTypeID }).
                            ToList();
                        foreach (var i in lists)
                            moreLists.Add(i);
                    }
                }
                return moreLists;
            }
        }

        private string searchTerm;
        /// <summary>
        /// Search Term to filter item collection on
        /// </summary>
        /// <returns></returns>
        public string SearchTerm
        {
            get
            {
                return searchTerm == null ? null : String.Format("search results on {0}", searchTerm);
            }
            set
            {
                if (value != searchTerm)
                {
                    searchTerm = value;
                    NotifyPropertyChanged("SearchTerm");
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

        private Visibility speechNetworkOperationInProgress = Visibility.Collapsed;
        /// <summary>
        /// Whether a network operation is in progress (yes == Visible / no == Collapsed)
        /// </summary>
        /// <returns></returns>
        public Visibility SpeechNetworkOperationInProgress
        {
            get
            {
                return speechNetworkOperationInProgress;
            }
            set
            {
                if (value != speechNetworkOperationInProgress)
                {
                    speechNetworkOperationInProgress = value;
                    NotifyPropertyChanged("SpeechNetworkOperationInProgress");
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

        #region Event handlers

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to About");

            // Navigate to the about page
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void DebugMenuItem_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Debug");

            // Navigate to the about page
            NavigationService.Navigate(new Uri("/DebugPage.xaml", UriKind.Relative));
        }

        private void EmailMenuItem_Click(object sender, EventArgs e)
        {
            // create email body
            StringBuilder sb = new StringBuilder("Zaplify Data:\n\n");
            foreach (Folder folder in App.ViewModel.Folders)
            {
                sb.AppendLine(folder.Name);

                foreach (Item item in folder.Items)
                {
                    ItemType itemType;
                    // get itemType for this item
                    try
                    {
                        itemType = App.ViewModel.ItemTypes.Single(lt => lt.ID == item.ItemTypeID);
                    }
                    catch (Exception)
                    {
                        // if can't find the item type, use the first
                        itemType = App.ViewModel.ItemTypes[0];
                    }

                    sb.AppendLine("    " + item.Name);
                    foreach (Field f in itemType.Fields.OrderBy(f => f.SortOrder))
                    {
                        PropertyInfo pi = null;
                        object currentValue = null;

                        // get the current field value.
                        // the value can either be in a strongly-typed property on the item (e.g. Name),
                        // or in one of the FieldValues 
                        try
                        {
                            // get the strongly typed property
                            pi = item.GetType().GetProperty(f.Name);
                            if (pi != null)
                                currentValue = pi.GetValue(item, null);
                        }
                        catch (Exception)
                        {
                        }

                        // if couldn't find a strongly typed property, this property is stored as a 
                        // FieldValue on the item
                        if (pi == null)
                        {
                            // get current item's value for this field
                            FieldValue fieldValue = item.GetFieldValue(f.ID, false);
                            if (fieldValue != null)
                                currentValue = fieldValue.Value;
                            if (String.IsNullOrWhiteSpace((string)currentValue))
                                currentValue = null;
                        }

                        // if this property wasn't found or is null, no need to print anything
                        if (currentValue == null)
                            continue;

                        // already printed out the item name
                        if (f.Name == FieldNames.Name)
                            continue;

                        // format the field value properly
                        if (currentValue != null)
                        {
                            switch (f.DisplayType)
                            {
                                case DisplayTypes.DatePicker:
                                    DateTime dateTime;
                                    try
                                    {
                                        dateTime = Convert.ToDateTime((string)currentValue);
                                    }
                                    catch (Exception)
                                    {
                                        break;
                                    }
                                    sb.AppendFormat("        {0}: {1}\n", f.DisplayName, dateTime.ToString("d"));
                                    break;
                                case DisplayTypes.Priority:
                                    int priority = 0;
                                    try
                                    {
                                        priority = Convert.ToInt32((string)currentValue);
                                    }
                                    catch (Exception)
                                    {
                                        break;
                                    }
                                    sb.AppendFormat("        {0}: {1}\n", f.DisplayName, Item.PriorityNames[priority]);
                                    break;
                                default:
                                    sb.AppendFormat("        {0}: {1}\n", f.DisplayName, currentValue.ToString());
                                    break;
                            }
                        }
                    }
                }
            }

            EmailComposeTask emailComposeItem = new EmailComposeTask();
            emailComposeItem.Subject = "Zaplify Data";
            emailComposeItem.Body = sb.ToString();
            emailComposeItem.Show();
        }

        private void EraseMenuItem_Click(object sender, EventArgs e)
        {
            // confirm the delete and return if the user cancels
            MessageBoxResult result = MessageBox.Show(
                "are you sure you want to erase all data on the phone?  unless you connected the phone to an account, your data will be not be retrievable.",
                "confirm erasing all data",
                MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            initialSyncAlreadyHappened = false;
            App.ViewModel.EraseAllData();

            // refresh the add buttons on the Add tab
            CreateAddButtons();
        }

        // Handle selection changed on ListBox
        private void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (FoldersListBox.SelectedIndex == -1)
                return;

            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to List");

            Folder folder = App.ViewModel.Folders[FoldersListBox.SelectedIndex];
            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/ListPage.xaml?type=Folder&ID=" + folder.ID.ToString(), UriKind.Relative));
            NavigationService.Navigate(new Uri(
                String.Format(
                    "/ListPage.xaml?type=Folder&ID={0}&ParentID={1}",
                    folder.ID.ToString(),
                    Guid.Empty.ToString()),
                UriKind.Relative));

            // Reset selected index to -1 (no selection)
            FoldersListBox.SelectedIndex = -1;
        }

        // Event handlers for Folders tab
        private void Folders_AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to FolderEditor");

            // Navigate to the FolderEditor page
            NavigationService.Navigate(
                new Uri("/FolderEditor.xaml?ID=new",
                UriKind.Relative));
        }

        private void MainPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            if (MoreListsPopup.IsOpen)
            {
                MoreListsPopup.IsOpen = false;
                e.Cancel = true;
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            TraceHelper.AddMessage("Main: Loaded");
          
            // if data isn't loaded from storage yet, load the app data
            if (!App.ViewModel.IsDataLoaded)
            {
                // Load app data from local storage (user creds, about tab data, constants, item types, folders, etc)
                App.ViewModel.LoadData();

                // create the add button panel
                CreateAddButtons();

                // register an event handler to call whenever the UserData is refreshed, so that we can refresh
                // the add buttons
                App.ViewModel.SyncComplete += new MainViewModel.SyncCompleteEventHandler((s, ea) =>
                {
                    // run this on the UI thread 
                    Deployment.Current.Dispatcher.BeginInvoke(() => { CreateAddButtons(); });
                });
            }

            // if haven't synced with web service yet, try now
            if (initialSyncAlreadyHappened == false)
            {
                // attempt to sync with the Service
                App.ViewModel.SyncWithService();

                initialSyncAlreadyHappened = true;

                // if there's a home tab set, switch to it now
                var homeTab = PhoneSettingsHelper.GetHomeTab(App.ViewModel.PhoneClientFolder);
                if (homeTab != null && homeTab != "Add")
                    SelectTab(homeTab);
            }

            // create a list of items to render 
            list = new Item() { Items = FilterItems(App.ViewModel.Items) };

            // create the ListHelper
            ScheduleHelper = new ScheduleHelper(list);

            // store the current listbox and ordering
            ScheduleHelper.ListBox = ItemsListBox;

            // render the items
            ScheduleHelper.RenderList(list);

            // add a property changed handler for the Items property
            if (!addedItemsPropertyChangedHandler)
            {
                App.ViewModel.PropertyChanged += new PropertyChangedEventHandler((s, args) =>
                {
                    // if the Items property was signaled, re-filter and re-render the items folder
                    if (args.PropertyName == "Items")
                    {
                        list.Items = FilterItems(App.ViewModel.Items);
                        ScheduleHelper.RenderList(list);
                    }
                });
                addedItemsPropertyChangedHandler = true;
            }

            // set the datacontext
            SearchHeader.DataContext = this;

            // trace exit
            TraceHelper.AddMessage("Exiting Main Loaded");
        }

        // handle a selection changed event for the "more lists..." ListBox in the MoreListsPopup
        private void MoreListsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (MoreListsListBox.SelectedIndex == -1)
                return;

            Item listToAddTo = MoreLists[MoreListsListBox.SelectedIndex];
            Folder folderToAddTo = App.ViewModel.Folders.Single(f => f.ID == listToAddTo.FolderID);
            if (listToAddTo.ID == Guid.Empty)
            {
                AddItem(folderToAddTo, null);
                ListMetadataHelper.IncrementListSelectedCount(App.ViewModel.PhoneClientFolder, folderToAddTo);
            }
            else
            {
                listToAddTo.Name = listToAddTo.Name.Trim();
                AddItem(folderToAddTo, listToAddTo);
                ListMetadataHelper.IncrementListSelectedCount(App.ViewModel.PhoneClientFolder, listToAddTo);
            }

            // if this is a navigation and not an add operation, we need to sync with the service to push the new selected count
            // (do not sync for operations against $ClientSettings)
            //if (String.IsNullOrWhiteSpace(NameTextBox.Text))
            //    App.ViewModel.SyncWithService();

            // Reset selected index to -1 (no selection)
            MoreListsListBox.SelectedIndex = -1;

            // close popup
            MoreListsPopup.IsOpen = false;
        }


        // When page is navigated to, switch to the specified tab
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // if the user data is already loaded, redraw the add button panel
            if (App.ViewModel.IsDataLoaded == true)
                CreateAddButtons();

            string tabString = "";
            // check for the optional "Tab" parameter
            if (NavigationContext.QueryString.TryGetValue("Tab", out tabString) == false)
                return;

            SelectTab(tabString);
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // remove all existing buttons from the application bar
            ApplicationBar.Buttons.Clear();

            // remove any "add" menu items
            if (((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).Text.StartsWith("add"))
                ApplicationBar.MenuItems.RemoveAt(0);

            // create the sync and settings buttons, which are shared by all pivot tabs
            var syncButton = new ApplicationBarIconButton() 
            { 
                Text = "sync now", 
                IconUri = new Uri("/Images/appbar.refresh.rest.png", UriKind.Relative)                
            };
            syncButton.Click += new EventHandler(RefreshButton_Click);
            var settingsButton = new ApplicationBarIconButton() 
            { 
                Text = "settings", 
                IconUri = new Uri("/Images/appbar.feature.settings.rest.png", UriKind.Relative)                
            };
            settingsButton.Click += new EventHandler(SettingsButton_Click);

            // do tab-specific processing (e.g. adding the right Add button handler)
            switch (MainPivot.SelectedIndex)
            {
                case 0: // add
                    ApplicationBar.Buttons.Add(syncButton);
                    ApplicationBar.Buttons.Add(settingsButton);
                    // if the user data is already loaded, redraw the add button panel
                    if (App.ViewModel.IsDataLoaded == true)
                        CreateAddButtons();
                    break;
                case 1: // schedule
                    ApplicationBar.Buttons.Add(syncButton);
                    ApplicationBar.Buttons.Add(settingsButton);
                    var searchButton = new ApplicationBarIconButton() 
                    { 
                        Text = "filter", 
                        IconUri = new Uri("/Images/appbar.feature.search.rest.png", UriKind.Relative) 
                    };
                    searchButton.Click += new EventHandler(Items_SearchButton_Click);                    
                    ApplicationBar.Buttons.Add(searchButton);
                    break;
                case 2: // folders
                    var addFolderMenuItem = new ApplicationBarMenuItem() 
                    { 
                        Text = "add folder", 
                        //IconUri = new Uri("/Images/appbar.add.rest.png", UriKind.Relative) 
                    };
                    addFolderMenuItem.Click += new EventHandler(Folders_AddButton_Click);
                    ApplicationBar.MenuItems.Insert(0, addFolderMenuItem);
                    ApplicationBar.Buttons.Add(syncButton);
                    ApplicationBar.Buttons.Add(settingsButton);
                    break;
                case 3: // tags
                    var addTagMenuItem = new ApplicationBarMenuItem() 
                    { 
                        Text = "add tag", 
                        // IconUri = new Uri("/Images/appbar.add.rest.png", UriKind.Relative) 
                    };
                    addTagMenuItem.Click += new EventHandler(Tags_AddButton_Click);
                    ApplicationBar.MenuItems.Insert(0, addTagMenuItem);
                    ApplicationBar.Buttons.Add(syncButton);
                    ApplicationBar.Buttons.Add(settingsButton);
                    break;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.ViewModel.SyncWithService();
        }

        // Event handlers for search popup
        private void SearchPopup_SearchButton_Click(object sender, EventArgs e)
        {
            SearchTerm = SearchTextBox.Text;

            // reset the items collection and render the new folder
            list.Items = FilterItems(App.ViewModel.Items);
            ScheduleHelper.RenderList(list);

            // close the popup
            SearchPopup.IsOpen = false;
        }

        private void SearchPopup_ClearButton_Click(object sender, EventArgs e)
        {
            SearchTerm = null;

            // reset the items collection and render the new folder
            list.Items = FilterItems(App.ViewModel.Items);
            ScheduleHelper.RenderList(list);

            // close the popup
            SearchPopup.IsOpen = false;
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Settings");

            // Navigate to the settings page
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        // Event handlers for tags tab
        private void Tag_HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton button = (HyperlinkButton)e.OriginalSource;
            Guid tagID = (Guid)button.Tag;

            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Folder");

            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
        }

        private void Tags_AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to TagEditor");

            // Navigate to the FolderEditor page
            NavigationService.Navigate(
                new Uri("/TagEditor.xaml?ID=new",
                UriKind.Relative));
        }

        /*
        private void TagsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (TagsListBox.SelectedIndex == -1)
                return;

            Tag tag = App.ViewModel.Tags[TagsListBox.SelectedIndex];

            // Trace the navigation and start a new timing
            TraceHelper.StartMessage("Navigating to Folder");

            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tag.ID.ToString(), UriKind.Relative));
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tag.ID.ToString(), UriKind.Relative));

            // Reset selected index to -1 (no selection)
            TagsListBox.SelectedIndex = -1;
        }
        */

        // Event handlers for items tab
        private void Items_AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Item");

            // Navigate to the Item page
            NavigationService.Navigate(
                new Uri("/ItemPage.xaml?ID=new",
                UriKind.Relative));
        }

        private void Items_SearchButton_Click(object sender, EventArgs e)
        {
            SearchPopup.IsOpen = true;
            SearchTextBox.Focus();
        }

        private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            TraceHelper.StartMessage("Main: Navigate to Item");

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

            // initialize the Speech provider
#if DEBUG
            SpeechHelper.SpeechProvider = SpeechHelper.SpeechProviders.NativeSpeech;
#endif

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
                    SpeechHelper.Cancel(
                        new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));

                    // reset the text in the textbox
                    NameTextBox.Text = "";
                    break;
                case SpeechHelper.SpeechState.Finished:
                    // user tapped the OK button

                    // set the text in the popup textbox
                    NameTextBox.Text = SpeechLabelText.Trim('\'');
                    break;
            }

            SpeechPopup_Close();
        }

        private void SpeechPopup_Close()
        {
            // close the popup 
            SpeechPopup.IsOpen = false;
        }

        private void SpeechPopup_NetworkOperationInProgressCallBack(bool operationInProgress, OperationStatus status)
        {
            // call the MainViewModel's routine to make sure global network status is reset
            App.ViewModel.NetworkOperationInProgressCallback(operationInProgress, status);

            // signal whether the net operation is in progress or not
            SpeechNetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);

            // if the operationSuccessful flag is null, no new data; otherwise, it signals the status of the last operation
            if (status != OperationStatus.Started)
            {
                if (status != OperationStatus.Success)
                {   // the server wasn't reachable
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show("Unable to access the speech service at this time.");
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
                NameTextBox.Text = textString;

                // dismiss the speech box
                SpeechPopup.IsOpen = false;

#if DEBUG
                //MessageBox.Show(speechDebugString);
#endif
            });
        }

        #endregion 

        #region Helpers

        private void AddItem(Folder folder, Item list)
        {
            string name = NameTextBox.Text;

            // don't add empty items - instead, navigate to the list
            if (name == null || name == "")
            {
                TraceHelper.StartMessage("AddPage: Navigate to List");
                NavigationService.Navigate(new Uri(
                    String.Format(
                        "/ListPage.xaml?type=Folder&ID={0}&ParentID={1}",
                        folder.ID,
                        list != null ? list.ID : Guid.Empty),
                    UriKind.Relative));
                return;
            }

            Guid itemTypeID;
            Guid parentID;
            if (list == null)
            {
                itemTypeID = folder.ItemTypeID;
                parentID = Guid.Empty;
            }
            else
            {
                itemTypeID = list.ItemTypeID;
                parentID = list.ID;
            }

            // get a reference to the item type
            ItemType itemType = App.ViewModel.ItemTypes.Single(it => it.ID == itemTypeID);

            // special case grocery items - split the name by comma and add a new grocery item for each string
            if (itemTypeID == SystemItemTypes.Grocery)
                foreach (var si in name.Split(','))
                    CreateItem(si.Trim(), folder, itemType, parentID);
            else
                CreateItem(name, folder, itemType, parentID);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // reset the name field to make it easy to add the next one
            NameTextBox.Text = "";
        }

        private void AddItemToFolder(string folderName)
        {
            Folder folder = App.ViewModel.Folders.Single(f => f.Name == folderName);
            AddItem(folder, null);
        }

        private void CreateAddButtons()
        {
            const string moreListsString = "more lists...";
            double width = Math.Max(420f, AddButtonsStackPanel.ActualWidth) / 2;

            // get all the lists
            var entityRefItems = App.ViewModel.GetListsOrderedBySelectedCount();
            lists = new List<ClientEntity>();
            foreach (var entityRefItem in entityRefItems)
            {
                var entityType = entityRefItem.GetFieldValue(FieldNames.EntityType).Value;
                var entityID = new Guid(entityRefItem.GetFieldValue(FieldNames.EntityRef).Value);
                if (entityType == typeof(Folder).Name && App.ViewModel.Folders.Any(f => f.ID == entityID))
                    lists.Add(App.ViewModel.Folders.Single(f => f.ID == entityID));
                if (entityType == typeof(Item).Name && App.ViewModel.Items.Any(i => i.ID == entityID))
                    lists.Add(App.ViewModel.Items.Single(i => i.ID == entityID));
            }
            // create a list of buttons - one for each list
            buttonList = (from it in lists
                          select new Button()
                          {
                              Content = it.Name,
                              Width = width,
                          }).ToList();
            foreach (var b in buttonList)
                b.Click += new RoutedEventHandler(AddButton_Click);

            // clear the button rows
            AddButtonsStackPanel.Children.Clear();
            for (int i = 0; i < AddButtons.Length; i++)
                AddButtons[i] = null;

            // assemble the buttons into rows (maximum of four buttons and two rows)
            // if there are two or less buttons, one row
            // otherwise distribute evenly across two rows
            int count = Math.Min(buttonList.Count, MaxLists);
            int firstrow = count, secondrow = 0, addButtonsRow = 0;
            if (count > MaxLists / 2)
            {
                firstrow = count / 2;
                secondrow = count - firstrow;
            }
            if (firstrow > 0)
            {
                if (firstrow == 1)
                    buttonList[0].Width *= 2;  // if there's only one button, make it double-width
                var sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
                foreach (var b in buttonList.Take(firstrow))
                    sp.Children.Add(b);
                AddButtons[addButtonsRow++] = sp;
                AddButtonsStackPanel.Children.Add(sp);
            }
            if (secondrow > 0)
            {
                var sp = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
                foreach (var b in buttonList.Skip(firstrow).Take(secondrow))
                    sp.Children.Add(b);
                AddButtons[addButtonsRow++] = sp;
                AddButtonsStackPanel.Children.Add(sp);
            }

            var moreButton = new Button()
            {
                Content = moreListsString,
                Width = width * 2,
            };
            moreButton.Click += new RoutedEventHandler(delegate
            {
                // rebind the list of lists
                NotifyPropertyChanged("MoreLists");
                MoreListsPopup.IsOpen = true;
            });
            AddButtonsStackPanel.Children.Add(moreButton);
        }

        private void CreateItem(string name, Folder folder, ItemType itemType, Guid parentID)
        {
            // figure out the sort value 
            float sortOrder = 1000f;
            var listItems = folder.Items.Where(it => it.ParentID == parentID).ToList();
            if (listItems.Count > 0)
                sortOrder += listItems.Max(it => it.SortOrder);

            // create the new item
            Item item = new Item()
            {
                Name = name,
                FolderID = folder.ID,
                ItemTypeID = itemType.ID,
                ParentID = parentID,
                SortOrder = sortOrder
            };

            // hack: special case processing for item types that have a Complete field
            // if it exists, set it to false
            if (itemType.HasField("Complete"))
                item.Complete = false;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = item
                });

            // add the item to the folder
            folder.Items.Add(item);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            // determine which button was clicked
            Button clickedButton = sender as Button ?? AddButtons[0].Children[0] as Button;
            int listIndex = buttonList.IndexOf(clickedButton);
            
            ClientEntity entity = lists[listIndex];
            if (entity is Item)
            {
                var list = entity as Item;
                Folder folder = App.ViewModel.Folders.Single(f => f.ID == list.FolderID);
                AddItem(folder, list);
            }
            if (entity is Folder)
            {
                var folder = entity as Folder;
                AddItem(folder, null);
            }

            // increment the SelectedCount
            ListMetadataHelper.IncrementListSelectedCount(App.ViewModel.PhoneClientFolder, entity);

            // if this is a navigation and not an add operation, we need to sync with the service to push the new selected count
            // (do not sync for operations against $ClientSettings)
            //if (String.IsNullOrWhiteSpace(NameTextBox.Text))
            //    App.ViewModel.SyncWithService();
        }     

        private ObservableCollection<Item> FilterItems(ObservableCollection<Item> items)
        {
            ObservableCollection<Item> filteredItems = new ObservableCollection<Item>();
            foreach (Item item in items)
            {
                // if the item doesn't have a complete field, don't list it
                ItemType itemType = App.ViewModel.ItemTypes.Single(it => it.ID == item.ItemTypeID);
                if (itemType.HasField(FieldNames.Complete) == false)
                    continue;
                
                // if the item is completed, don't list it
                if (item.Complete == true)
                    continue;

                // if the item is a list, don't list it
                if (item.IsList)
                    continue;

                // if the item doesn't have a due date, or it's already passed, don't list it
                if (item.Due == null || item.Due < DateTime.Now.Date)
                    continue;

                // if there is no search term present, add this item and continue
                if (searchTerm == null)
                {
                    filteredItems.Add(item);
                    continue;
                }

                // search for the term in every non-null string field
                foreach (var pi in item.GetType().GetProperties())
                {
                    if (pi.PropertyType.Name == "String" && pi.CanWrite)
                    {
                        string stringVal = (string)pi.GetValue(item, null);
                        // perform case-insensitive comparison
                        if (stringVal != null && stringVal.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            filteredItems.Add(item);
                            break;
                        }
                    }
                }
            }

            // return the filtered item collection
            return filteredItems;
        }

        private void SelectTab(string tabString)
        {
            switch (tabString)
            {
                case "Add":
                    MainPivot.SelectedIndex = 0;  // switch to add tab
                    break;
                case "Schedule":
                    MainPivot.SelectedIndex = 1;  // switch to schedule tab
                    break;
                case "Folders":
                    MainPivot.SelectedIndex = 2;  // switch to folders tab
                    break;
                case "Tags":
                    MainPivot.SelectedIndex = 3;  // switch to tags tab
                    break;
                default:
                    break;
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