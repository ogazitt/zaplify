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
using Microsoft.Phone.Tasks;
using System.Reflection;
using System.Windows.Data;
using System.ComponentModel;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.ClientHelpers;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class SettingsPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        public SettingsPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Settings: constructor");

            // Set the data context of the page to the main view model
            DataContext = App.ViewModel;

            // set up tabbing
            this.IsTabStop = true;

            Loaded += new RoutedEventHandler(SettingsPage_Loaded);
            BackKeyPress += new EventHandler<CancelEventArgs>(SettingsPage_BackKeyPress);
        }

        private bool enableCreateButton;
        /// <summary>
        /// Databound flag to indicate whether to enable the create button
        /// </summary>
        /// <returns></returns>
        public bool EnableCreateButton
        {
            get
            {
                //return enableCreateButton;
                return true;
            }
            set
            {
                if (value != enableCreateButton)
                {
                    enableCreateButton = value;
                    NotifyPropertyChanged("EnableCreateButton");
                }
            }
        }

        private bool enableSyncButton;
        /// <summary>
        /// Databound flag to indicate whether to enable the sync button
        /// </summary>
        /// <returns></returns>
        public bool EnableSyncButton
        {
            get
            {
                //return enableSyncButton;
                return true;
            }
            set
            {
                if (value != enableSyncButton)
                {
                    enableSyncButton = value;
                    NotifyPropertyChanged("EnableSyncButton");
                }
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

        #region Event handlers

        private void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (Email.Text == null || Email.Text == "" ||
                Password.Password == null || Password.Password == "")
            {
                MessageBox.Show("please enter a valid email address and password");
                return;
            }

            if (MergeCheckbox.IsChecked == false)
            {
                MessageBoxResult result = MessageBox.Show(
                    "leaving the 'merge' checkbox unchecked will cause any new items you've added to be lost.  " +
                    "click ok to create the account without the local data, or cancel the operation.",
                    "erase local data?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;

                // clear the record queue
                RequestQueue.RequestRecord record = RequestQueue.DequeueRequestRecord();
                while (record != null)
                {
                    record = RequestQueue.DequeueRequestRecord();
                }
            }

            User user = new User() { Email = Email.Text, Password = Password.Password };
            App.ViewModel.User = user;

            WebServiceHelper.CreateUser(
                user,
                new CreateUserCallbackDelegate(CreateUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }

        void SettingsPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Settings: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace page navigation
            TraceHelper.AddMessage("Settings: Loaded");

            // initialize some fields
            DefaultListPicker.ItemsSource = App.ViewModel.Folders;
            DefaultListPicker.DisplayMemberPath = "Name";

            int index = App.ViewModel.Folders.IndexOf(App.ViewModel.DefaultFolder);

            if (index >= 0)
                DefaultListPicker.SelectedIndex = index;

            CreateUserButton.DataContext = this;
            SyncUserButton.DataContext = this;
        }

        // Event handlers for settings tab
        private void SaveButton_Click(object sender, EventArgs e)
        {
            // if we made changes in the account info but didn't successfully carry them out, put up a warning dialog
            if (accountTextChanged && !accountOperationSuccessful)
            {
                MessageBoxResult result = MessageBox.Show(
                    "account was not successfully created or paired (possibly because you haven't clicked the 'create' or 'pair' button).  " +
                    "click ok to dismiss the settings page and forget the changes to the account page, or cancel the operation.",
                    "exit settings before creating or pairing an account?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            else
            {
                // save the new account information
                User user = new User() { Email = Email.Text, Password = Password.Password };
                App.ViewModel.User = user;
            }

            // save the default folder in any case
            App.ViewModel.DefaultFolder = DefaultListPicker.SelectedItem as Folder;

            // trace page navigation
            TraceHelper.StartMessage("Settings: Navigate back");

            // go back to main page
            NavigationService.GoBack();
        }

        private void SyncUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (MergeCheckbox.IsChecked == true)
            {
                MessageBoxResult result = MessageBox.Show(
                    "leaving the 'merge' checkbox checked will merge the new data on the phone with existing data in the account, potentially creating duplicate data.  " +
                    "click ok to sync the account and merge the phone data, or cancel the operation.",
                    "merge local data?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;
            }
            else
            {
                // clear the record queue
                RequestQueue.RequestRecord record = RequestQueue.DequeueRequestRecord();
                while (record != null)
                {
                    record = RequestQueue.DequeueRequestRecord();
                }
            }

            User user = new User() { Email = Email.Text, Password = Password.Password };
            App.ViewModel.User = user;

            WebServiceHelper.VerifyUserCredentials(
                user,
                new VerifyUserCallbackDelegate(VerifyUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }

        private void Textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            // must have values for the create user button to be enabled
            if (Email.Text == null || Email.Text == "" ||
                Password.Password == null || Password.Password == "")
                CreateUserButton.IsEnabled = false;
            else
                CreateUserButton.IsEnabled = true;

            // email and password textboxes must have valid values for the sync button to be enabled
            if (Email.Text == null || Email.Text == "" ||
                Password.Password == null || Password.Password == "")
                SyncUserButton.IsEnabled = false;
            else
                SyncUserButton.IsEnabled = true;

            // email must be different than the current email (if any) for create user button to be enabled
            if (App.ViewModel.User != null && App.ViewModel.User.Name == Email.Text)
                CreateUserButton.IsEnabled = false;

            // indicate that the account text is modified
            accountTextChanged = true;
        }

        #endregion

        #region Authentication callback methods

        public delegate void VerifyUserCallbackDelegate(User user, HttpStatusCode? code);
        private void VerifyUserCallback(User user, HttpStatusCode? code)
        {
            // run this on the UI thread
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (code)
                {
                    case HttpStatusCode.OK:
                        MessageBox.Show(String.Format("successfully linked with {0} account; data sync will start automatically.", Email.Text));
                        accountOperationSuccessful = true;
                        user.Synced = true;
                        App.ViewModel.User = user;
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
                        MessageBox.Show(String.Format("account {0} was not successfully paired", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                }
                //if (!accountOperationSuccessful)
                //    App.ViewModel.User = null;

            });
        }

        public delegate void CreateUserCallbackDelegate(User user, HttpStatusCode? code);
        private void CreateUserCallback(User user, HttpStatusCode? code)
        {
            // run this on the UI thread
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (code)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        MessageBox.Show(String.Format("user account {0} successfully created", Email.Text));
                        accountOperationSuccessful = true;
                        user.Synced = true;
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
                        MessageBox.Show(String.Format("email address {0} in invalid OR password is not strong enough", Email.Text));
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
                        MessageBox.Show(String.Format("user {0} was not created", Email.Text));
                        accountOperationSuccessful = false;
                        break;
                }

                //if (!accountOperationSuccessful)
                //    App.ViewModel.User = null;
            });
        }

        #endregion Authentication callback methods
    }
}