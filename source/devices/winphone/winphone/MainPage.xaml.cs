using System;
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
using System.Reflection;
using System.Windows.Data;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using System.Xml.Linq;
using System.Windows.Resources;
using System.IO;
using Microsoft.Phone.Tasks;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private bool addedItemsPropertyChangedHandler = false;
        private bool initialSync = false;
        Item list;
        ListHelper ListHelper;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Main: constructor");

            // Set the data context of the page to the main view model
            DataContext = App.ViewModel;

            // set the data context of the search header to this page
            SearchHeader.DataContext = this;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
                // do the below instead to avoid Invalid cross-thread access exception
                //Deployment.Current.Dispatcher.BeginInvoke(() => { handler(this, new PropertyChangedEventArgs(propertyName)); });
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
                                    sb.AppendFormat("        {0}: {1}\n", f.DisplayName, ((DateTime)currentValue).ToString("d"));
                                    break;
                                case DisplayTypes.Priority:
                                    sb.AppendFormat("        {0}: {1}\n", f.DisplayName, Item.PriorityNames[(int)currentValue]);
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
                "are you sure you want to erase all data on the phone?  unless you paired the phone to an account, your data will be not be retrievable.",
                "confirm erasing all data",
                MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            foreach (var tl in App.ViewModel.Folders)
                StorageHelper.DeleteFolder(tl);
            StorageHelper.WriteConstants(null);
            StorageHelper.WriteDefaultFolderID(null);
            StorageHelper.WriteItemTypes(null);
            StorageHelper.WriteTags(null);
            StorageHelper.WriteFolders(null);
            StorageHelper.WriteUserCredentials(null);
            RequestQueue.DeleteQueue();
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

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            TraceHelper.AddMessage("Main: Loaded");
          
            // if data isn't loaded from storage yet, load the app data
            if (!App.ViewModel.IsDataLoaded)
            {
                // Load app data from local storage (user creds, about tab data, constants, item types, folders, etc)
                App.ViewModel.LoadData();
            }

            // if haven't synced with web service yet, try now
            if (initialSync == false)
            {
                // attempt to sync with the Service
                App.ViewModel.SyncWithService();

                initialSync = true;
            }

            // create a list of items to render 
            list = new Item() { Items = FilterItems(App.ViewModel.Items) };

            // create the ListHelper
            ListHelper = new BuiltSteady.Zaplify.Devices.WinPhone.ListHelper(
                list,
                new RoutedEventHandler(Items_CompleteCheckbox_Click), 
                new RoutedEventHandler(Tag_HyperlinkButton_Click));

            // store the current listbox and ordering
            ListHelper.ListBox = ItemsListBox;
            ListHelper.OrderBy = "due";

            // render the items
            ListHelper.RenderList(list);

            // add a property changed handler for the Items property
            if (!addedItemsPropertyChangedHandler)
            {
                App.ViewModel.PropertyChanged += new PropertyChangedEventHandler((s, args) =>
                {
                    // if the Items property was signaled, re-filter and re-render the items folder
                    if (args.PropertyName == "Items")
                    {
                        list.Items = FilterItems(App.ViewModel.Items);
                        ListHelper.RenderList(list);
                    }
                });
                addedItemsPropertyChangedHandler = true;
            }

            // set the datacontext
            SearchHeader.DataContext = this;

            // trace exit
            TraceHelper.AddMessage("Exiting Main Loaded");
        }

        // When page is navigated to, switch to the specified tab
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string tabString = "";
            // check for the optional "Tab" parameter
            if (NavigationContext.QueryString.TryGetValue("Tab", out tabString) == false)
            {
                return;
            }

            switch (tabString)
            {
                case "Items":
                    MainPivot.SelectedIndex = 0;  // switch to items tab
                    break;
                case "Folders":
                    MainPivot.SelectedIndex = 1;  // switch to folders tab
                    break;
                case "Tags":
                    MainPivot.SelectedIndex = 2;  // switch to tags tab
                    break;
                default:
                    break;
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // get a reference to the add button (always first) and remove any eventhandlers
            var AddButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            try
            {
                AddButton.Click -= new EventHandler(Items_AddButton_Click);
                AddButton.Click -= new EventHandler(Folders_AddButton_Click);
                AddButton.Click -= new EventHandler(Tags_AddButton_Click);

                // remove the last button (in case it was added)
                ApplicationBar.Buttons.RemoveAt(3);
            }
            catch (Exception)
            {
            }

            // do tab-specific processing (e.g. adding the right Add button handler)
            switch (MainPivot.SelectedIndex)
            {
                case 0: // items
                    AddButton.Click += new EventHandler(Items_AddButton_Click);
                    var searchButton = new ApplicationBarIconButton() 
                    { 
                        Text = "filter", 
                        IconUri = new Uri("/Images/appbar.feature.search.rest.png", UriKind.Relative) 
                    };
                    searchButton.Click += new EventHandler(Items_SearchButton_Click);                    
                    ApplicationBar.Buttons.Add(searchButton);
                    break;
                case 1: // folders
                    AddButton.Click += new EventHandler(Folders_AddButton_Click);
                    break;
                case 2: // tags
                    AddButton.Click += new EventHandler(Tags_AddButton_Click);
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
            ListHelper.RenderList(list);

            // close the popup
            SearchPopup.IsOpen = false;
        }

        private void SearchPopup_ClearButton_Click(object sender, EventArgs e)
        {
            SearchTerm = null;

            // reset the items collection and render the new folder
            list.Items = FilterItems(App.ViewModel.Items);
            ListHelper.RenderList(list);

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

        /// <summary>
        /// Handle click event on Complete checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Items_CompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)e.OriginalSource;
            Guid itemID = (Guid)cb.Tag;

            // get the item that was just updated, and ensure the Complete flag is in the correct state
            Item item = App.ViewModel.Items.Single<Item>(t => t.ID == itemID);

            // get a reference to the base folder that this item belongs to
            Folder f = App.ViewModel.LoadFolder(item.FolderID);

            // create a copy of that item
            Item itemCopy = new Item(item);

            // toggle the complete flag to reflect the checkbox click
            item.Complete = !item.Complete;

            // bump the last modified timestamp
            item.LastModified = DateTime.UtcNow;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { itemCopy, item },
                    BodyTypeName = "Item",
                    ID = item.ID
                });

            // remove the item from the list and ListBox (because it will now be complete)
            ListHelper.RemoveItem(list, item);

            // save the changes to local storage
            StorageHelper.WriteFolder(f);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();
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

        #endregion 

        #region Helpers

        private ObservableCollection<Item> FilterItems(ObservableCollection<Item> items)
        {
            ObservableCollection<Item> filteredItems = new ObservableCollection<Item>();
            foreach (Item item in items)
            {
                // if the item is completed, don't list it
                if (item.Complete == null || item.Complete == true)
                    continue;

                // if the item is a list, don't list it
                if (item.IsList)
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

        #endregion
    }
}