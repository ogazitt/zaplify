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
using Microsoft.Phone.Tasks;
using System.Reflection;
using System.Windows.Data;
using System.ComponentModel;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;
using Microsoft.Phone.Shell;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class SettingsPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        bool isInitialized = false;

        public SettingsPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Settings: constructor");

            // Set the data context of the page to the main view model
            DataContext = App.ViewModel;

            // set up tabbing
            this.IsTabStop = true;

            // if already connected, open to Settings tab
            if (IsConnected)
                MainPivot.SelectedIndex = 1;

            MainPivot.SelectionChanged += new SelectionChangedEventHandler(MainPivot_SelectionChanged);
            BackKeyPress += new EventHandler<CancelEventArgs>(SettingsPage_BackKeyPress);
        }

        /// <summary>
        /// Databound flag to that determines some controls' visibility based on whether we are connected or disconnected
        /// </summary>
        /// <returns></returns>
        public Visibility ConnectedMode
        {
            get
            {
                return IsConnected ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Databound property for create button text (which doubles as the disconnect button)
        /// </summary>
        public string CreateButtonText
        {
            get
            {
                return IsConnected ? "disconnect" : "create";
            }
        }

        /// <summary>
        /// Databound flag to indicate whether to enable the create/disconnect and connect buttons
        /// </summary>
        /// <returns></returns>
        public bool EnableButtons
        {
            get
            {
                return IsConnected
                    ? true
                    : !String.IsNullOrWhiteSpace(Email.Text) && !String.IsNullOrWhiteSpace(Password.Password);
            }
        }

        public bool IsConnected
        {
            get
            {
                return App.ViewModel.User != null && App.ViewModel.User.Synced;
            }
        }

        private bool accountTextChanged = false;
        private bool accountOperationSuccessful = false;

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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (isInitialized == true)
                return;

            // trace page navigation
            TraceHelper.AddMessage("Settings: Loaded");

            Email.IsEnabled = !IsConnected;
            Password.IsEnabled = !IsConnected;
            accountOperationSuccessful = false;
            accountTextChanged = false;
            SettingsPanel.Children.Clear();

            foreach (var setting in PhoneSettings.Settings.Keys)
            {
                // get the source list for the setting, along with any current value
                var phoneSetting = PhoneSettings.Settings[setting];
                //var bindingList = (from l in list select new { Name = l }).ToList();
                var bindingList = phoneSetting.Values;
                var value = PhoneSettingsHelper.GetPhoneSetting(App.ViewModel.PhoneClientFolder, setting);
                int selectedIndex = 0;
                if (value != null && bindingList.Any(ps => ps.Name == value))
                {
                    var selectedValue = bindingList.Single(ps => ps.Name == value);
                    selectedIndex = bindingList.IndexOf(selectedValue);
                }

                var template = !String.IsNullOrEmpty(phoneSetting.DisplayTemplate) ?
                    (DataTemplate)App.Current.Resources[phoneSetting.DisplayTemplate] :
                    null;

                // create a new list picker for the setting
                var listPicker = new ListPicker()
                {
                    Header = setting,
                    Tag = setting,
                    SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0
                };
                listPicker.ItemsSource = bindingList;
                listPicker.DisplayMemberPath = "Name";
                if (template != null)
                    listPicker.FullModeItemTemplate = template;
                SettingsPanel.Children.Add(listPicker);
            }

            CreateUserButton.DataContext = this;
            CreateButtonLabel.DataContext = this;
            ConnectUserButton.DataContext = this;
            ConnectButtonLabel.DataContext = this;
            Email.LostFocus += new RoutedEventHandler(delegate { accountTextChanged = true; NotifyPropertyChanged("EnableButtons"); });
            Password.LostFocus += new RoutedEventHandler(delegate { accountTextChanged = true; NotifyPropertyChanged("EnableButtons"); });

            isInitialized = true;
        }

        #region Event handlers

        private void ConnectUserButton_Click(object sender, RoutedEventArgs e)
        {
            // validate account info
            if (String.IsNullOrWhiteSpace(Email.Text) ||
                String.IsNullOrWhiteSpace(Password.Password))
            {
                MessageBox.Show("please enter a valid email address and password");
                return;
            }

            // process an account connect request
            User user = new User() { Email = Email.Text, Password = Password.Password };
            WebServiceHelper.VerifyUserCredentials(
                user,
                new VerifyUserCallbackDelegate(VerifyUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }

        private void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            // if we're connected, this is a disconnect request
            if (IsConnected)
            {
                // if the request queue isn't empty, warn the user
                if (RequestQueue.GetRequestRecord(RequestQueue.UserQueue) != null)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "some of the changes you made on the phone haven't made it to your Zaplify account yet.  " +
                        "click ok to disconnect now and potentially lose these changes, or cancel the operation",
                        "disconnect now?",
                        MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.Cancel)
                        return;
                }

                // process the disconnect
                App.ViewModel.User = null;
                App.ViewModel.EraseAllData();
                WebServiceHelper.Disconnect();

                // reset the settings page
                NotifyPropertyChanged("ConnectedMode");
                NotifyPropertyChanged("CreateButtonText");
                NotifyPropertyChanged("EnableButtons");
                Email.IsEnabled = true;
                Email.Text = "";
                Password.IsEnabled = true;
                Password.Password = "";
                accountTextChanged = false;
                accountOperationSuccessful = false;
                foreach (var element in SettingsPanel.Children)
                {
                    // get the listpicker key and value
                    ListPicker listPicker = element as ListPicker;
                    if (listPicker == null)
                        continue;
                    listPicker.SelectedIndex = 0;
                }
                return;
            }
            
            // validate account info
            if (String.IsNullOrWhiteSpace(Email.Text) ||
                String.IsNullOrWhiteSpace(Password.Password))
            {
                MessageBox.Show("please enter a valid email address and password");
                return;
            }

            // process an account creation request
            User user = new User() { Email = Email.Text, Password = Password.Password };
            WebServiceHelper.CreateUser(
                user,
                new CreateUserCallbackDelegate(CreateUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }

        void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // remove all existing buttons from the application bar
            ApplicationBar.Buttons.Clear();

            // do tab-specific processing (e.g. adding the Save button)
            switch (MainPivot.SelectedIndex)
            {
                case 0: // account
                    break;
                case 1: // settings
                    var saveButton = new ApplicationBarIconButton()
                    {
                        Text = "save",
                        IconUri = new Uri("/Images/appbar.save.rest.png", UriKind.Relative)
                    };
                    saveButton.Click += new EventHandler(SaveButton_Click);
                    ApplicationBar.Buttons.Add(saveButton);
                    break;
            }
        }

        // Event handlers for settings tab
        private void SaveButton_Click(object sender, EventArgs e)
        {
            // if we made changes in the account info but didn't successfully carry them out, put up a warning dialog
            if (accountTextChanged && !accountOperationSuccessful)
            {
                MessageBoxResult result = MessageBox.Show(
                    "account was not successfully created or connected (possibly because you haven't clicked the 'create' or 'connect' button).  " +
                    "click ok to dismiss the settings page and forget the changes to the account page, or cancel to try again.",
                    "exit settings before creating or connecting to an account?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;
            }

            // get current version of phone settings
            var phoneSettingsItemCopy = new Item(PhoneSettingsHelper.GetPhoneSettingsItem(App.ViewModel.PhoneClientFolder), true);

            // loop through the settings and store the new value
            foreach (var element in SettingsPanel.Children)
            {
                // get the listpicker key and value
                ListPicker listPicker = element as ListPicker;
                if (listPicker == null)
                    continue;
                string key = (string)listPicker.Tag;
                var phoneSetting = PhoneSettings.Settings[key];
                string value = phoneSetting.Values[listPicker.SelectedIndex].Name;

                // store the key/value pair in phone settings (without syncing)
                PhoneSettingsHelper.StorePhoneSetting(App.ViewModel.PhoneClientFolder, key, value);
            }

            // get the new version of phone settings
            var phoneSettingsItem = PhoneSettingsHelper.GetPhoneSettingsItem(App.ViewModel.PhoneClientFolder);

            // queue up a server request
            if (App.ViewModel.PhoneClientFolder.ID != Guid.Empty)
            {
                RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { phoneSettingsItemCopy, phoneSettingsItem },
                    BodyTypeName = "Item",
                    ID = phoneSettingsItem.ID,
                    IsDefaultObject = true
                });

                // sync with the server
                App.ViewModel.SyncWithService();
            }

            // trace page navigation
            TraceHelper.StartMessage("Settings: Navigate back");

            // notify the view model that some data-bound properties bound to client settings may have changed
            App.ViewModel.NotifyPropertyChanged("BackgroundColor");

            // go back to main page
            NavigationService.GoBack();
        }

        private void SettingsPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Settings: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        #endregion

        #region Authentication callback methods

        public delegate void VerifyUserCallbackDelegate(User user, HttpStatusCode? code);
        private void VerifyUserCallback(User user, HttpStatusCode? code)
        {
            // run this on the UI thread
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // if the user did not get returned, the operation could not have been successful
                if (code == HttpStatusCode.OK && user == null)
                    code = HttpStatusCode.ServiceUnavailable;
                switch (code)
                {
                    case HttpStatusCode.OK:
                        MessageBox.Show(String.Format("successfully connected to account {0}; data sync will start automatically.", Email.Text));
                        accountOperationSuccessful = true;
                        user.Synced = true;
                        // the server no longer echos the password in the payload so keep the local value when successful
                        if (user.Password == null)
                            user.Password = Password.Password;
                        App.ViewModel.User = user;
                        // prepare for sync by cleaning out the user queue and deleting the system queue ($ClientSettings comes from the server account)
                        RequestQueue.PrepareUserQueueForAccountConnect();
                        RequestQueue.DeleteQueue(RequestQueue.SystemQueue);
                        App.ViewModel.SyncWithService();
                        break;
                    case HttpStatusCode.NotFound:
                        MessageBox.Show(String.Format("user {0} not found", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.Forbidden:
                        MessageBox.Show(String.Format("incorrect password"));
                        accountOperationSuccessful = false;
                        break;
                    case null:
                        MessageBox.Show(String.Format("couldn't reach the server"));
                        accountOperationSuccessful = false;
                        break;
                    default:
                        MessageBox.Show(String.Format("did not successfully connect to account {0}", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                }

                // update UI if successful
                if (accountOperationSuccessful)
                {
                    NotifyPropertyChanged("ConnectedMode");
                    NotifyPropertyChanged("CreateButtonText");
                    NotifyPropertyChanged("EnableButtons");
                    Email.IsEnabled = false;
                    Password.IsEnabled = false;
                    // return to main page
                    TraceHelper.StartMessage("Settings: Navigate back");
                    NavigationService.GoBack();
                }
            });
        }

        public delegate void CreateUserCallbackDelegate(User user, HttpStatusCode? code);
        private void CreateUserCallback(User user, HttpStatusCode? code)
        {
            // run this on the UI thread
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // if the user came back null, the operation could not have completed successfully
                if (code == HttpStatusCode.OK || code == HttpStatusCode.Created)
                    if (user == null)
                        code = HttpStatusCode.ServiceUnavailable;
                switch (code)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        MessageBox.Show(String.Format("user account {0} successfully created", Email.Text));
                        accountOperationSuccessful = true;
                        user.Synced = true;
                        // the server no longer echos the password in the payload so keep the local value when successful
                        if (user.Password == null)
                            user.Password = Password.Password;
                        App.ViewModel.User = user;
                        App.ViewModel.SyncWithService();
                        break;
                    case HttpStatusCode.NotFound:
                        MessageBox.Show(String.Format("user {0} not found", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.Conflict:
                        MessageBox.Show(String.Format("user {0} already exists", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.NotAcceptable:
                        MessageBox.Show(String.Format("email address {0} is invalid or password is not strong enough", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.InternalServerError:
                        MessageBox.Show(String.Format("user {0} was not created successfully (missing a field?)", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                    case null:
                        MessageBox.Show(String.Format("couldn't reach the server"));
                        accountOperationSuccessful = false;
                        break;
                    default:
                        MessageBox.Show(String.Format("user {0} was not created - contact support@builtsteady.com", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                }

                // update UI if successful
                if (accountOperationSuccessful)
                {
                    NotifyPropertyChanged("ConnectedMode");
                    NotifyPropertyChanged("CreateButtonText");
                    NotifyPropertyChanged("EnableButtons");
                    Email.IsEnabled = false;
                    Password.IsEnabled = false;
                    // return to main page
                    TraceHelper.StartMessage("Settings: Navigate back");
                    NavigationService.GoBack();
                }
            });
        }

        #endregion Authentication callback methods
    }
}