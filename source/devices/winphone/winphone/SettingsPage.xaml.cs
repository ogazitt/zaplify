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
using TaskStoreWinPhoneUtilities;
using TaskStoreClientEntities;
using System.Reflection;
using System.Windows.Data;
using System.ComponentModel;

namespace TaskStoreWinPhone
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
                return enableCreateButton;
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
                return enableSyncButton;
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
            if (Username.Text == null || Username.Text == "" ||
                Password.Password == null || Password.Password == "" ||
                Email.Text == null || Email.Text == "")
            {
                MessageBox.Show("please enter a username, password, and email address");
                return;
            }

            if (MergeCheckbox.IsChecked == false)
            {
                MessageBoxResult result = MessageBox.Show(
                    "leaving the 'merge' checkbox unchecked will cause any new tasks you've added to be lost.  " +
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

            WebServiceHelper.CreateUser(
                new User() { Name = Username.Text, Password = Password.Password, Email = Email.Text },
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
            DefaultListPicker.ItemsSource = App.ViewModel.TaskLists;
            DefaultListPicker.DisplayMemberPath = "Name";

            int index = App.ViewModel.TaskLists.IndexOf(App.ViewModel.DefaultTaskList);

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
                User user = new User() { Name = Username.Text, Password = Password.Password, Email = Email.Text };
                App.ViewModel.User = user;
            }

            // save the default tasklist in any case
            App.ViewModel.DefaultTaskList = DefaultListPicker.SelectedItem as TaskList;

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
                    "leaving the 'merge' checkbox checked will merge the new lists on the phone with existing data in the account, potentially creating duplicate lists.  " +
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

            User user = new User() { Name = Username.Text, Password = Password.Password, Email = Email.Text };

            WebServiceHelper.VerifyUserCredentials(
                user,
                new VerifyUserCallbackDelegate(VerifyUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }

        private void Textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            // all textboxes must have values for the create user button to be enabled
            if (Username.Text == null || Username.Text == "" ||
                Password.Password == null || Password.Password == "" ||
                Email.Text == null || Email.Text == "")
                CreateUserButton.IsEnabled = false;
            else
                CreateUserButton.IsEnabled = true;

            // username and password textboxes must have valid values for the sync button to be enabled
            if (Username.Text == null || Username.Text == "" ||
                Password.Password == null || Password.Password == "")
                SyncUserButton.IsEnabled = false;
            else
                SyncUserButton.IsEnabled = true;

            // username must be different than the current username (if any) for create user button to be enabled
            if (App.ViewModel.User != null && App.ViewModel.User.Name == Username.Text)
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
                        MessageBox.Show(String.Format("successfully linked with {0} account; data sync will start automatically.", Username.Text));
                        accountOperationSuccessful = true;
                        user.Synced = true;
                        App.ViewModel.User = user;
                        App.ViewModel.SyncWithService();
                        break;
                    case HttpStatusCode.NotFound:
                        MessageBox.Show(String.Format("user {0} not found", Username.Text));
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
                        MessageBox.Show(String.Format("account {0} was not successfully paired", Username.Text));
                        accountOperationSuccessful = false;
                        break;
                }
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
                        MessageBox.Show(String.Format("user account {0} successfully created", Username.Text));
                        accountOperationSuccessful = true;
                        user.Synced = true;
                        App.ViewModel.User = user;
                        App.ViewModel.SyncWithService();
                        break;
                    case HttpStatusCode.NotFound:
                        MessageBox.Show(String.Format("user {0} not found", Username.Text));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.Conflict:
                        MessageBox.Show(String.Format("user {0} already exists", Username.Text));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.InternalServerError:
                        MessageBox.Show(String.Format("user {0} was not created successfully (missing a field?)", Username.Text));
                        accountOperationSuccessful = false;
                        break;
                    case null:
                        MessageBox.Show(String.Format("couldn't reach the server"));
                        accountOperationSuccessful = false;
                        break;
                    default:
                        MessageBox.Show(String.Format("user {0} was not created", Username.Text));
                        accountOperationSuccessful = false;
                        break;
                }
            });
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

        private void RenderSettingsTab(Settings settings)
        {
            foreach (PropertyInfo pi in settings.GetType().GetProperties())
            {
                // get the value of the property
                var val = pi.GetValue(settings, null);

                ListBoxItem listBoxItem = new ListBoxItem();
                StackPanel EditStackPanel = new StackPanel();
                listBoxItem.Content = EditStackPanel;
                EditStackPanel.Children.Add(new TextBlock() { Text = pi.Name, FontSize = 20 });

                string typename = GetTypeName(pi);

                // render the right control based on the type
                switch (typename)
                {
                    //case "String":
                    //    PropertyInfo pistr = pi;
                    //    tb = new TextBox() { DataContext = taskCopy };
                    //    tb.SetBinding(TextBox.TextProperty, new Binding(pistr.Name) { Mode = BindingMode.TwoWay });
                    //    //tb.LostFocus += new RoutedEventHandler(delegate { pistr.SetValue(taskCopy, tb.Text, null); });
                    //    EditStackPanel.Children.Add(tb);
                    //    break;
                    case "String":
                    case "Int32":
                    case "DateTime":
                        TextBox tb = new TextBox() { DataContext = settings, MinWidth = App.Current.RootVisual.RenderSize.Width };
                        tb.SetBinding(TextBox.TextProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                        EditStackPanel.Children.Add(tb);
                        break;
                    default:
                        break;
                }
                // add the listboxitem to the listbox
                //SecondListBox.Items.Add(listBoxItem);
            }
        }

        #endregion
    }
}