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
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using System.Reflection;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Windows.Data;
using System.Runtime.Serialization;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using System.Text.RegularExpressions;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Interactivity;
using WPKeyboardHelper; 
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Device.Location;
using System.ComponentModel;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class ItemPage : PhoneApplicationPage
    {
        private Item thisItem;
        private Item itemCopy;
        private Folder folder;
        private ItemType itemType;
        private Button moreButton;
        private KeyboardHelper keyboardHelper;

        private bool isInitialized = false;
        private int tabIndex = 0;

        // Constructor
        public ItemPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Item: constructor");

            ConnectedIconImage.DataContext = App.ViewModel;

            this.IsTabStop = true;

            this.Loaded += new RoutedEventHandler(ItemPage_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(ItemPage_BackKeyPress);
        }

        // When page is navigated to set data context to selected item in itemType
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("Item: OnNavigatedTo");

            // check to make sure we haven't initialized yet
            if (isInitialized == true)
                return;

            // create the keyboard helper for tabbed navigation
            this.keyboardHelper = new KeyboardHelper(LayoutRoot);

            // reset the tab index
            tabIndex = 0;

            // render the folder field by default
            bool renderFolderField = true;

            // find the folder that this item would belong to
            string folderIDString = "";
            if (NavigationContext.QueryString.TryGetValue("folderID", out folderIDString))
            {
                Guid folderID = new Guid(folderIDString);
                if (folderID != Guid.Empty)
                {
                    try
                    {
                        //folder = App.ViewModel.Folders.Single(folder => folder.ID == folderID);
                        folder = App.ViewModel.LoadFolder(folderID);
                    }
                    catch (Exception)
                    {
                        folder = null;
                    }
                }
            }

            // if we haven't found a folder, use the default one
            if (folder == null)
            {
                folder = App.ViewModel.DefaultFolder;
                renderFolderField = true;
            }

            string itemIDString = "";
            // must have a item ID passed (either a valid GUID or "new")
            if (NavigationContext.QueryString.TryGetValue("ID", out itemIDString) == false)
            {
                // trace page navigation
                TraceHelper.StartMessage("Item: Navigate back");

                NavigationService.GoBack();
                return;
            }

            // the item page is used to construct a new item
            if (itemIDString == "new")
            {
                // remove the "actions" tab
                //ItemPagePivotControl.Items.RemoveAt(0);
                //((PivotItem)(ItemPagePivotControl.Items[0])).IsEnabled = false;
                itemCopy = new Item() { FolderID = folder.ID };
                thisItem = null;
                RenderViewItem(itemCopy); 
                RenderEditItem(itemCopy, renderFolderField);

                // navigate the pivot control to the "edit" view
                ItemPagePivotControl.SelectedIndex = 1;
            }
            else 
            {
                // editing an existing item
                Guid id = new Guid(itemIDString);
                //thisItem = App.ViewModel.Items.Single(t => t.ID == id);
                //folder = App.ViewModel.Folders.Single(folder => folder.ID == thisItem.FolderID);
                thisItem = folder.Items.Single(t => t.ID == id);

                // make a deep copy of the item for local binding
                itemCopy = new Item(thisItem);
                DataContext = itemCopy;
                RenderViewItem(itemCopy);
                RenderEditItem(itemCopy, true /* render the list field */);
            }
                    
            // set the initialized flag
            isInitialized = true;
        }

        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the tastlist page
            NavigationService.GoBack();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new item, delete just does the same thing as cancel
            if (thisItem == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this item?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = itemCopy
                });

            // remove the item from the local itemType
            folder.Items.Remove(thisItem);

            // save the changes to local storage
            //StorageHelper.WriteFolders(App.ViewModel.Folders);
            StorageHelper.WriteFolder(folder);

            // trigger a databinding refresh for items
            //App.ViewModel.NotifyPropertyChanged("Items");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the folder page
            NavigationService.GoBack();
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            // remove the more button
            EditListBox.Items.Remove(moreButton);

            // render the non-primary fields
            RenderEditItemFields(itemCopy, itemType, false, false);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Hack: need to change the focus to the parent page so as to invoke all the LostFocus handlers 
            // and get all the data written back to the fields
            this.Focus();

            // schedule the Save click implementation on the UI thread
            Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SaveButton_Click_Implementation), sender, e);
        }
            
        private void SaveButton_Click_Implementation(object sender, EventArgs e)
        {
            // update the LastModified timestamp
            itemCopy.LastModified = DateTime.UtcNow;

            ParseFields(itemCopy);

            // if this is a new item, create it
            if (thisItem == null)
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = itemCopy
                        });

                // add the item to the local itemType
                folder.Items.Add(itemCopy);
                thisItem = itemCopy;
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { thisItem, itemCopy },
                        BodyTypeName = "Item",
                        ID = thisItem.ID
                    });

                // save the changes to the existing item
                int index = IndexOf(folder, thisItem);
                if (index < 0)
                    return; 
                folder.Items[index] = itemCopy;
                thisItem = itemCopy;
            }
            
            // save the changes to local storage
            //StorageHelper.WriteFolders(App.ViewModel.Folders);
            StorageHelper.WriteFolder(folder);

            // trigger a databinding refresh for items
            //App.ViewModel.NotifyPropertyChanged("Items");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // signal the folder that the FirstDue property needs to be recomputed
            folder.NotifyPropertyChanged("FirstDue");

            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the folder page
            NavigationService.GoBack();
        }

        void ItemPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void ItemPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("Item: Loaded");
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                keyboardHelper.HandleReturnKey();
            }
        }

        #endregion

        #region Helpers

        private string GetTypeName(PropertyInfo pi)
        {
            string typename = pi.PropertyType.Name;
            // if it's a generic type, get the underlying type (this is for Nullables)
            if (pi.PropertyType.IsGenericType)
            {
                typename = pi.PropertyType.FullName;
                string del = "[[System.";  // delimiter
                int index = typename.IndexOf(del);
                index = index < 0 ? index : index + del.Length;  // add length of delimiter
                int index2 = index < 0 ? index : typename.IndexOf(",", index);
                // if anything went wrong, default to String
                if (index < 0 || index2 < 0)
                    typename = "String";
                else
                    typename = typename.Substring(index, index2 - index);
            }
            return typename;
        }

        /// <summary>
        /// Find a item by ID and then return its index 
        /// </summary>
        /// <param name="observableCollection"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        private int IndexOf(Folder folder, Item item)
        {
            try
            {
                Item itemRef = folder.Items.Single(t => t.ID == item.ID);
                return folder.Items.IndexOf(itemRef);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void ParseFields(Item item)
        {
            string text = item.Description;
            if (text == null || text == "")
                return;

            Match m;

            // parse the text for a phone number
            m = Regex.Match(text, @"(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Phone = m.Value;

            // parse the text for an email address
            m = Regex.Match(text, @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[A-Z]{2}|com|org|net|edu|gov|mil|biz|info|mobi|name|aero|asia|jobs|museum)\b", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Email = m.Value;

            // parse the text for a website
            m = Regex.Match(text, @"((http|https)(:\/\/))?([a-zA-Z0-9]+[.]{1}){2}[a-zA-z0-9]+(\/{1}[a-zA-Z0-9]+)*\/?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Website = m.Value;           
        }

        private void RenderEditItem(Item item, bool renderFolderField)
        {
            // get itemType for this item
            try
            {
                itemType = App.ViewModel.ItemTypes.Single(it => it.ID == item.ItemTypeID);
            }
            catch (Exception)
            {
                // if can't find the folder type, use the first
                itemType = App.ViewModel.ItemTypes[0];
            }

            // render the primary fields
            RenderEditItemFields(item, itemType, true, renderFolderField);

            // render more button
            moreButton = new Button() { Content = "more details" };
            moreButton.Click += new RoutedEventHandler(MoreButton_Click);
            EditListBox.Items.Add(moreButton);
        }

        private void RenderEditItemField(Item item, Field field)
        {
            PropertyInfo pi = null;
            object currentValue = null;
            object container = null;

            // get the current field value.
            // the value can either be in a strongly-typed property on the item (e.g. Name),
            // or in one of the FieldValues 
            try
            {
                // get the strongly typed property
                pi = item.GetType().GetProperty(field.Name);
                if (pi != null)
                {
                    // store current item's value for this field
                    currentValue = pi.GetValue(item, null);

                    // set the container - this will be the object that will be passed 
                    // to pi.SetValue() below to poke new values into
                    container = itemCopy;
                }
            }
            catch (Exception)
            {
                // an exception indicates this isn't a strongly typed property on the Item
                // this is NOT an error condition
            }

            // if couldn't find a strongly typed property, this property is stored as a 
            // FieldValue on the item
            if (pi == null)
            {
                // get current item's value for this field, or create a new FieldValue
                // if one doesn't already exist
                FieldValue fieldValue = item.GetFieldValue(field.ID, true);
                currentValue = fieldValue.Value;

                // get the value property of the current fieldvalue (this should never fail)
                pi = fieldValue.GetType().GetProperty("Value");
                if (pi == null)
                    return;

                // set the container - this will be the object that will be passed 
                // to pi.SetValue() below to poke new values into
                container = fieldValue;
            }

            ListBoxItem listBoxItem = new ListBoxItem();
            StackPanel EditStackPanel = new StackPanel();
            listBoxItem.Content = EditStackPanel;
            EditStackPanel.Children.Add(
                new TextBlock()
                {
                    Text = field.DisplayName,
                    Style = (Style)App.Current.Resources["PhoneTextNormalStyle"]
                });

            // create a textbox (will be used by the majority of field types)
            double minWidth = App.Current.RootVisual.RenderSize.Width;
            if ((int)minWidth == 0)
                minWidth = ((this.Orientation & PageOrientation.Portrait) == PageOrientation.Portrait) ? 480.0 : 800.0;

            TextBox tb = new TextBox() { DataContext = container, MinWidth = minWidth, IsTabStop = true };
            tb.SetBinding(TextBox.TextProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });

            bool notMatched = false;
            // render the right control based on the DisplayType 
            switch (field.DisplayType)
            {
                case DisplayTypes.Text:
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    EditStackPanel.Children.Add(tb);
                    break;
                case DisplayTypes.TextArea:
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                    tb.AcceptsReturn = true;
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Height = 300;
                    tb.TabIndex = tabIndex++;
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                    EditStackPanel.Children.Add(tb);
                    break;
                case DisplayTypes.Phone:
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.TelephoneNumber } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    StackPanel innerPanel = RenderEditItemImageButtonPanel(tb);
                    ImageButton imageButton = (ImageButton)innerPanel.Children[1];
                    imageButton.Click += new RoutedEventHandler(delegate
                    {
                        PhoneNumberChooserTask chooser = new PhoneNumberChooserTask();
                        chooser.Completed += new EventHandler<PhoneNumberResult>((sender, e) =>
                        {
                            if (e.TaskResult == TaskResult.OK && e.PhoneNumber != null && e.PhoneNumber != "")
                                pi.SetValue(container, e.PhoneNumber, null);
                        });
                        chooser.Show();
                    });
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                case DisplayTypes.Link:
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Url } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    EditStackPanel.Children.Add(tb);
                    break;
                case DisplayTypes.Email:
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.EmailSmtpAddress } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    innerPanel = RenderEditItemImageButtonPanel(tb);
                    imageButton = (ImageButton)innerPanel.Children[1];
                    imageButton.Click += new RoutedEventHandler(delegate
                    {
                        EmailAddressChooserTask chooser = new EmailAddressChooserTask();
                        chooser.Completed += new EventHandler<EmailResult>((sender, e) =>
                        {
                            if (e.TaskResult == TaskResult.OK && e.Email != null && e.Email != "")
                                pi.SetValue(container, e.Email, null);
                        });
                        chooser.Show();
                    });
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                case DisplayTypes.Address:
                    tb.InputScope = new InputScope()
                    {
                        Names = 
                            { 
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressStreet },
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressCity },
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressStateOrProvince },
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressCountryName },
                            }
                    };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    innerPanel = RenderEditItemImageButtonPanel(tb);
                    imageButton = (ImageButton)innerPanel.Children[1];
                    imageButton.Click += new RoutedEventHandler(delegate
                    {
                        // start the location service
                        GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                        watcher.MovementThreshold = 20; // Use MovementThreshold to ignore noise in the signal.
                        watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>((sender, e) =>
                        {
                            if (e.Status == GeoPositionStatus.Ready)
                            {
                                // Use the Position property of the GeoCoordinateWatcher object to get the current location.
                                GeoCoordinate co = watcher.Position.Location;
                                tb.Text = co.Latitude.ToString("0.000") + "," + co.Longitude.ToString("0.000");
                                //Stop the Location Service to conserve battery power.
                                watcher.Stop();
                            }
                        });
                        watcher.Start();
                    });
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                case DisplayTypes.Priority:
                    ListPicker lp = new ListPicker()
                    {
                        MinWidth = minWidth,
                        FullModeItemTemplate = (DataTemplate)App.Current.Resources["FullListPickerTemplate"],
                        IsTabStop = true
                    };
                    lp.ItemsSource = App.ViewModel.Constants.Priorities;
                    lp.DisplayMemberPath = "Name";
                    int? lpval = (int?)pi.GetValue(container, null);
                    if (lpval != null)
                        lp.SelectedIndex = (int)lpval;
                    else
                        lp.SelectedIndex = 1;  // HACK: hardcode to "Normal" priority.  this should come from a table.
                    lp.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(container, lp.SelectedIndex == 1 ? (int?)null : lp.SelectedIndex, null); });
                    lp.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(lp);
                    break;
                case "Folder":
                    ListPicker folderPicker = new ListPicker() { MinWidth = minWidth, IsTabStop = true };
                    folderPicker.ItemsSource = App.ViewModel.Folders;
                    folderPicker.DisplayMemberPath = "Name";
                    Folder tl = App.ViewModel.Folders.FirstOrDefault(list => list.ID == folder.ID);
                    folderPicker.SelectedIndex = App.ViewModel.Folders.IndexOf(tl);
                    folderPicker.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(container, App.ViewModel.Folders[folderPicker.SelectedIndex].ID, null); });
                    folderPicker.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(folderPicker);
                    break;
                case "List":
                    ListPicker listPicker = new ListPicker()
                    {
                        MinWidth = minWidth,
                        FullModeItemTemplate = (DataTemplate)App.Current.Resources["FullListPickerTemplate"],
                        IsTabStop = true
                    };
                    var lists = App.ViewModel.Items.Where(i => i.FolderID == item.FolderID && i.IsList == true).OrderBy(i => i.Name).ToObservableCollection();
                    lists.Insert(0, new Item()
                    {
                        ID = Guid.Empty,
                        Name = folder.Name + "(folder)"
                    });
                    listPicker.ItemsSource = lists;
                    listPicker.DisplayMemberPath = "Name";
                    Item thisItem = lists.FirstOrDefault(i => i.ID == item.ParentID);
                    // if the list isn't found (e.g. ParentID == null), SelectedIndex will default to the Folder scope (which is correct for that case)
                    listPicker.SelectedIndex = Math.Max(lists.IndexOf(thisItem), 0);  
                    listPicker.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(container, lists[listPicker.SelectedIndex].ID, null); });
                    listPicker.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(listPicker);
                    break;
                case "Integer":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Digits } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, Convert.ToInt32(tb.Text), null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    EditStackPanel.Children.Add(tb);
                    break;
                case DisplayTypes.DatePicker:
                    DatePicker dp = new DatePicker() { DataContext = container, MinWidth = minWidth, IsTabStop = true };
                    dp.SetBinding(DatePicker.ValueProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                    dp.ValueChanged += new EventHandler<DateTimeValueChangedEventArgs>(delegate
                    {
                        //pi.SetValue(container, dp.Value, null);
                        pi.SetValue(container, dp.Value == null ? null : ((DateTime)dp.Value).ToString("d"), null);
                        folder.NotifyPropertyChanged("FirstDue");
                        folder.NotifyPropertyChanged("FirstDueColor");
                    });
                    dp.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(dp);
                    break;
                case DisplayTypes.Checkbox:
                    CheckBox cb = new CheckBox() { DataContext = container, IsTabStop = true };
                    cb.SetBinding(CheckBox.IsCheckedProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                    cb.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(cb);
                    break;
                case DisplayTypes.TagList:
                    TextBox taglist = new TextBox() { MinWidth = minWidth, IsTabStop = true };
                    taglist.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    taglist.TabIndex = tabIndex++;
                    RenderEditItemTagList(taglist, (Item) container, pi);
                    EditStackPanel.Children.Add(taglist);
                    break;
                    /*
                case "ListPointer":
                    innerPanel = RenderEditFolderPointer(pi, minWidth);
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                     */
                default:
                    notMatched = true;
                    break;
            }

            // if wasn't able to match field type by display type, try matching by CLR type
            if (notMatched == true)
            {
                string typename = GetTypeName(pi);
                switch (typename)
                {
                    case "String":
                        tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                        tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                        tb.TabIndex = tabIndex++;
                        tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                        EditStackPanel.Children.Add(tb);
                        break;
                    case "Int32":
                        tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Digits } } };
                        tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, Convert.ToInt32(tb.Text), null); });
                        tb.TabIndex = tabIndex++;
                        tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                        EditStackPanel.Children.Add(tb);
                        break;
                    case "DateTime":
                        DatePicker dp = new DatePicker() { DataContext = container, MinWidth = minWidth, IsTabStop = true };
                        dp.SetBinding(DatePicker.ValueProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                        dp.ValueChanged += new EventHandler<DateTimeValueChangedEventArgs>(delegate
                        {
                            pi.SetValue(container, dp.Value, null);
                            folder.NotifyPropertyChanged("FirstDue");
                            folder.NotifyPropertyChanged("FirstDueColor");
                        });
                        dp.TabIndex = tabIndex++;
                        EditStackPanel.Children.Add(dp);
                        break;
                    case "Boolean":
                        CheckBox cb = new CheckBox() { DataContext = container, IsTabStop = true };
                        cb.SetBinding(CheckBox.IsEnabledProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                        cb.TabIndex = tabIndex++;
                        EditStackPanel.Children.Add(cb);
                        break;
                    default:
                        break;
                }
            }

            // add the listboxitem to the listbox
            EditListBox.Items.Add(listBoxItem);
        }
        
        private void RenderEditItemFields(Item item, ItemType itemtype, bool primary, bool renderListField)
        {
            if (renderListField == true)
            {
                //FieldType fieldType = new FieldType() { Name = "FolderID", DisplayName = "folder", DisplayType = "Folder" };
                Field field = new Field() { Name = "ParentID", DisplayName = "list", DisplayType = "List" };
                RenderEditItemField(item, field);
            }

            // render fields
            foreach (Field f in itemtype.Fields.Where(f => f.IsPrimary == primary).OrderBy(f => f.SortOrder))
                RenderEditItemField(item, f);

            // refresh the keyboard tabstops
            keyboardHelper.RefreshTabbedControls(null);
        }

        private static StackPanel RenderEditItemImageButtonPanel(TextBox tb)
        {
            tb.MinWidth -= 64;
            StackPanel innerPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
            innerPanel.Children.Add(tb);
            ImageButton imageButton = new ImageButton()
            {
                Image = new BitmapImage(new Uri("/Images/button.search.png", UriKind.Relative)),
                PressedImage = new BitmapImage(new Uri("/Images/button.search.pressed.png", UriKind.Relative)),
                Width = 48,
                Height = 48,
                Template = (ControlTemplate)App.Current.Resources["ImageButtonControlTemplate"]
            };
            innerPanel.Children.Add(imageButton);
            return innerPanel;
        }

        /*
        private StackPanel RenderEditFolderPointer(PropertyInfo pi, double minWidth)
        {
            StackPanel innerPanel;
            innerPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
            CheckBox listcb = new CheckBox() { DataContext = itemCopy, IsTabStop = true };
            listcb.SetBinding(CheckBox.IsCheckedProperty, new Binding("LinkedFolderIDBool"));
            listcb.TabIndex = tabIndex++;
            ListPicker listPicker = new ListPicker()
            {
                MinWidth = minWidth,
                FullModeItemTemplate = (DataTemplate)App.Current.Resources["FullListPickerTemplate"],
                DataContext = listcb
            };
            listPicker.SetBinding(ListPicker.IsEnabledProperty, new Binding("IsChecked"));
            listPicker.ItemsSource = App.ViewModel.Folders;
            listPicker.DisplayMemberPath = "Name";
            Guid? folderID = (Guid?)pi.GetValue(itemCopy, null);
            if (folderID != null)
            {
                try
                {
                    Folder folderVal = App.ViewModel.Folders.Single(t => t.ID == (Guid)folderID);
                    if (folderVal != null)
                        listPicker.SelectedIndex = App.ViewModel.Folders.IndexOf(folderVal);
                }
                catch (Exception)
                {
                    listPicker.SelectedIndex = 0;
                }
            }
            else
                listPicker.SelectedIndex = 0;

            // set the event handlers for the checkbox and listpicker
            listcb.Unchecked += new RoutedEventHandler(delegate { pi.SetValue(itemCopy, null, null); });
            listcb.Checked += new RoutedEventHandler(delegate { pi.SetValue(itemCopy, App.ViewModel.Folders[listPicker.SelectedIndex].ID, null); });
            listPicker.SelectionChanged += new SelectionChangedEventHandler(delegate
            {
                if (listcb.IsChecked == false)
                    pi.SetValue(itemCopy, null, null);
                else
                    pi.SetValue(itemCopy, App.ViewModel.Folders[listPicker.SelectedIndex].ID, null);
            });
            innerPanel.Children.Add(listcb);
            innerPanel.Children.Add(listPicker);
            return innerPanel;
        }
         */

        private void RenderEditItemTagList(TextBox taglist, Item item, PropertyInfo pi)
        {
            taglist.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };

            // build the comma delimited tag folder for this item
            bool addDelimiter = false;
            StringBuilder sb = new StringBuilder();
            var itemtags = (IEnumerable<ItemTag>)pi.GetValue(item, null);
            if (itemtags != null)
            {
                foreach (ItemTag tt in itemtags)
                {
                    if (addDelimiter)
                        sb.Append(",");
                    Tag tag = App.ViewModel.Tags.Single(t => t.ID == tt.TagID);
                    sb.Append(tag.Name);
                    addDelimiter = true;
                }
                taglist.Text = sb.ToString();
            }

            // retrieve the itemtags for the item, creating new tags along the way
            taglist.LostFocus += new RoutedEventHandler(delegate
            {
                //ObservableCollection<ItemTag> existingTags = (ObservableCollection<ItemTag>)pi.GetValue(item, null);
                ObservableCollection<ItemTag> newTags = new ObservableCollection<ItemTag>();
                string[] tags = taglist.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tt in tags)
                {
                    string str = tt.Trim();
                    Tag tag;
                    try
                    {
                        tag = App.ViewModel.Tags.Single(t => t.Name == str);
                        newTags.Add(new ItemTag() { Name = str, TagID = tag.ID, ItemID = item.ID });
                    }
                    catch (Exception)
                    {
                        // this is a new tag that we need to create 
                        tag = new Tag() { Name = str };
                        newTags.Add(new ItemTag() { Name = str, TagID = tag.ID, ItemID = item.ID });

                        // enqueue the Web Request Record 
                        RequestQueue.EnqueueRequestRecord(
                            new RequestQueue.RequestRecord()
                            {
                                ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                                Body = new Tag(tag)
                            });

                        // add the tag to the tag folder
                        App.ViewModel.Tags.Add(tag);

                        // save the changes to local storage
                        StorageHelper.WriteTags(App.ViewModel.Tags);
                    }
                }

                // store the new ItemTag collection in the item
                pi.SetValue(item, newTags, null);

                // create the mirror Tags collection in the item
                item.CreateTags(App.ViewModel.Tags);
            });
        }

        private void RenderViewItem(Item item)
        {
            // get the item type
            ItemType itemType = null;
            if (ItemType.ItemTypes.TryGetValue(item.ItemTypeID, out itemType) == false)
                return;

            int row = 0;

            // render fields
            foreach (ActionType action in App.ViewModel.Constants.ActionTypes.OrderBy(a => a.SortOrder))
            {
                FieldValue fieldValue = null;
                
                // find out if the property exists on the current item
                try
                {
                    Field field = itemType.Fields.Single(f => f.Name == action.FieldName);
                    fieldValue = item.FieldValues.Single(fv => fv.FieldID == field.ID);
                }
                catch (Exception)
                {
                    // we can't do anything with this field since we don't have it on the local type
                    // but that's ok - we can keep going
                    continue;
                }

                // get the value of the property
                string currentValue = fieldValue.Value;

                // for our purposes, an empty value is the same as null
                if (currentValue == "")
                    currentValue = null;

                // render this property if it's not null/empty
                if (currentValue != null)
                {
                    // first make sure that we do want to render (type-specific logic goes here)
                    switch (action.ActionName)
                    {
                        case "Postpone":
                            // if the date is already further in the future than today, omit adding this action
                            if (Convert.ToDateTime(currentValue).Date > DateTime.Today.Date)
                                continue;
                            break;
                    }

                    // add a new row
                    ViewGrid.RowDefinitions.Add(new RowDefinition() { MaxHeight = 72 });
                    Thickness margin = new Thickness(12, 20, 0, 0);  // bounding rectangle of padding

                    // create a new buton for the action (verb)
                    var button = new Button()
                    {
                        Content = action.DisplayName,
                        MinWidth = 200
                    };
                    button.SetValue(Grid.ColumnProperty, 0);
                    button.SetValue(Grid.RowProperty, row);
                    ViewGrid.Children.Add(button);
                    
                    // create a label which holds the noun the verb will act upon 
                    // usually extracted from the item field's contents
                    var valueTextBlock = new TextBlock()
                    {
                        DataContext = fieldValue,
                        Style = (Style)App.Current.Resources["PhoneTextNormalStyle"],
                        Margin = margin,
                    };

                    //value.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                    valueTextBlock.SetValue(Grid.ColumnProperty, 1);
                    valueTextBlock.SetValue(Grid.RowProperty, row++);
                    ViewGrid.Children.Add(valueTextBlock);

                    // render the action based on the action type
                    switch (action.ActionName)
                    {
                        case "Navigate":
                            try
                            {
                                Folder f = App.ViewModel.Folders.Single(t => t.ID == Guid.Parse(currentValue));
                                valueTextBlock.Text = String.Format("to {0}", f.Name);
                                button.Click += new RoutedEventHandler(delegate
                                {
                                    // trace page navigation
                                    TraceHelper.StartMessage("Item: Navigate to Folder");

                                    // Navigate to the new page
                                    NavigationService.Navigate(new Uri("/ListPage.xaml?type=Folder&ID=" + f.ID.ToString(), UriKind.Relative));
                                });
                            }
                            catch (Exception)
                            {
                                valueTextBlock.Text = "(folder not found)";
                            }
                            break;
                        case "Postpone":
                            valueTextBlock.Text = "to tomorrow";
                            button.Click += new RoutedEventHandler(delegate
                            {
                                item.DueDate = DateTime.Today.Date.AddDays(1.0).ToString("yyyy-MM-dd");
                                folder.NotifyPropertyChanged("FirstDue");
                                folder.NotifyPropertyChanged("FirstDueColor");
                            });
                            break;
                        case "AddToCalendar":
                            valueTextBlock.DataContext = item;
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("DueDisplay"));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                folder.NotifyPropertyChanged("FirstDue");
                                folder.NotifyPropertyChanged("FirstDueColor");
                            });
                            break;
                        case "Map":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Value"));
                            button.Click += new RoutedEventHandler(delegate
                            {
#if WINPHONE7 // Pre-MANGO
                                string mapUrl = "maps:";
                                bool space = false;
                                foreach (string part in valueString.Split(' '))
                                {
                                    if (space == true)
                                        mapUrl += "%20";
                                    mapUrl += part;
                                    space = true;
                                }
                                WebBrowserItem mapItem = new WebBrowserItem() { Uri = new Uri(mapUrl) };
#else // MANGO
                                BingMapsTask mapItem = new BingMapsTask() { SearchTerm = currentValue };
#endif
                                mapItem.Show();
                            });
                            break;
                        case "Phone":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Value"));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                PhoneCallTask phoneCallTask = new PhoneCallTask() { PhoneNumber = (string)currentValue };
                                phoneCallTask.Show();
                            });
                            break;
                        case "TextMessage":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Value"));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                SmsComposeTask smsTask = new SmsComposeTask() { To = (string)currentValue };
                                smsTask.Show();
                            });
                            break;
                        case "Browse":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Value"));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                string url = (string)currentValue;
                                if (url.Substring(1, 4) != "http")
                                    url = String.Format("http://{0}", url);
                                WebBrowserTask browserTask = new WebBrowserTask() { Uri = new Uri(url) };
                                browserTask.Show();
                            });
                            break;
                        case "Email":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Value"));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                EmailComposeTask emailItem = new EmailComposeTask() { To = (string)currentValue };
                                emailItem.Show();
                            });
                            break;
                    }
                }
            }
        }

        #endregion
    }
}