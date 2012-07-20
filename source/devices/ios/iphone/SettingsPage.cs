using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class AccountPage
	{
		private User user;
		private Element Email;
		private Element Password;
        private bool accountOperationSuccessful = false;
        private DialogViewController dvc = null;
        private UINavigationController controller;

        private bool IsConnected
        {
            get
            {
                return App.ViewModel.User != null && App.ViewModel.User.Synced;
            }
        }

        public AccountPage(UINavigationController c)
        {
            TraceHelper.AddMessage("Account: constructor");
            controller = c;
			//this.Title = NSBundle.MainBundle.LocalizedString ("Account", "Account");
		}

        public void PushViewController()
        {
            // trace event
            TraceHelper.AddMessage("Account: PushViewController");            
            InitializeComponent();
        }
		
        void ConnectUserButton_Click (object sender, EventArgs e)
        {
            // validate account info
            var email = ((EntryElement) Email).Value;
            var pswd = ((EntryElement) Password).Value;
            if (String.IsNullOrWhiteSpace(email) ||
                String.IsNullOrWhiteSpace(pswd))
            {
                MessageBox.Show("please enter a valid email address and password");
                return;
            }

            // process an account connect request
            User user = new User() { Email = email, Password = pswd };
            WebServiceHelper.VerifyUserCredentials(
                user,
                new VerifyUserCallbackDelegate(VerifyUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }

		void CreateUserButton_Click(object sender, EventArgs e)
        {
            // validate account info
            ((EntryElement)Email).FetchValue();
            ((EntryElement)Password).FetchValue();
            var email = ((EntryElement) Email).Value;
            var pswd = ((EntryElement) Password).Value;
            if (String.IsNullOrWhiteSpace(email) ||
                String.IsNullOrWhiteSpace(pswd))
            {
                MessageBox.Show("please enter a valid email address and password");
                return;
            }

            // process an account creation request
            User user = new User() { Email = email, Password = pswd };
            WebServiceHelper.CreateUser(
                user,
                new CreateUserCallbackDelegate(CreateUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }
		
        void DisconnectUserButton_Click(object sender, EventArgs e)
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
                accountOperationSuccessful = false;
                dvc.NavigationController.PopViewControllerAnimated(true);
                controller.ResignFirstResponder();
                CreateRoot();
                dvc.ReloadData();
            }
        }

		#region Authentication callback methods

        public delegate void VerifyUserCallbackDelegate(User user, HttpStatusCode? code);
        private void VerifyUserCallback(User user, HttpStatusCode? code)
        {
			controller.BeginInvokeOnMainThread(() =>
			{
                string email = ((EntryElement)Email).Value;
                string pswd = ((EntryElement)Password).Value;

                // if the user did not get returned, the operation could not have been successful
                if (code == HttpStatusCode.OK && user == null)
                    code = HttpStatusCode.ServiceUnavailable;
                switch (code)
	            {
	                case HttpStatusCode.OK:
	                    MessageBox.Show(String.Format("successfully connected to account {0}; data sync will start automatically.", email));
	                    accountOperationSuccessful = true;
	                    user.Synced = true;
						// the server no longer echos the password in the payload so keep the local value when successful
						if (user.Password == null)
                            user.Password = pswd;
                        App.ViewModel.User = user;
                        // prepare the user queue and remove the system queue (the $ClientSettings will be lost because the server is authoritative)
                        RequestQueue.PrepareUserQueueForAccountConnect();
                        RequestQueue.DeleteQueue(RequestQueue.SystemQueue);
	                    App.ViewModel.SyncWithService();
	                    break;
	                case HttpStatusCode.NotFound:
	                    MessageBox.Show(String.Format("user {0} not found", email));
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
                        MessageBox.Show(String.Format("did not successfully connect to account {0}", email));
	                    accountOperationSuccessful = false;
	                    break;
	            }

                // update UI if successful
                if (accountOperationSuccessful)
                {
                    dvc.NavigationController.PopViewControllerAnimated(true);
                    CreateRoot();
                    dvc.ReloadData();
                }
                controller.ResignFirstResponder();
            });
        }

        public delegate void CreateUserCallbackDelegate(User user, HttpStatusCode? code);
        private void CreateUserCallback(User user, HttpStatusCode? code)
        {
            controller.BeginInvokeOnMainThread(() =>
            {
                string email = ((EntryElement)Email).Value;
                string pswd = ((EntryElement)Password).Value;

                // if the user came back null, the operation could not have completed successfully
                if (code == HttpStatusCode.OK || code == HttpStatusCode.Created)
                    if (user == null)
                        code = HttpStatusCode.ServiceUnavailable;
                switch (code)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        MessageBox.Show(String.Format("user account {0} successfully created", email));
                        accountOperationSuccessful = true;
                        user.Synced = true;
    					// the server no longer echos the password in the payload so keep the local value when successful
    					if (user.Password == null)
    						user.Password = pswd;
                        App.ViewModel.User = user;
                        App.ViewModel.SyncWithService();
                        break;
                    case HttpStatusCode.NotFound:
                        MessageBox.Show(String.Format("user {0} not found", email));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.Conflict:
                        MessageBox.Show(String.Format("user {0} already exists", email));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.NotAcceptable:
                        MessageBox.Show(String.Format("email address {0} is invalid or password is not strong enough", email));
                        accountOperationSuccessful = false;
                        break;
                    case HttpStatusCode.InternalServerError:
                        MessageBox.Show(String.Format("user {0} was not created successfully (missing a field?)", email));
                        accountOperationSuccessful = false;
                        break;
                    case null:
                        MessageBox.Show(String.Format("couldn't reach the server"));
                        accountOperationSuccessful = false;
                        break;
                    default:
                        MessageBox.Show(String.Format("user {0} was not created - contact support@builtsteady.com", email));
                        accountOperationSuccessful = false;
                        break;
                }
    
                // update UI if successful
                if (accountOperationSuccessful)
                {
                    dvc.NavigationController.PopViewControllerAnimated(true);
                    CreateRoot();
                    dvc.ReloadData();
                }
                controller.ResignFirstResponder();
            });
        }
		
		#endregion
            
        #region Helpers

        private RootElement InitializeAccountSettings()
        {
            user = App.ViewModel.User;  
            ThemedRootElement accountRootElement = null;

            // initialize the Account element based on whether connected or disconnected
            if (IsConnected)
            {
                // initialize account controls
                Email = new StringElement("Email", user.Email);
                // create unicode bullet characters for every character in the password
                var sb = new StringBuilder();
                if (user != null && user.Password != null)
                    foreach (var c in user.Password)
                        sb.Append("\u25CF"); // \u2022
                Password = new StringElement("Password", sb.ToString());

                // create buttons
                var button = new ButtonListElement() 
                {
                    new Button() { Caption = "Disconnect",  Background = "Images/darkgreybutton.png", Clicked = DisconnectUserButton_Click },
                };
                button.Margin = 0f;

                // create the account root element
                accountRootElement = new ThemedRootElement("Account")
                {
                    new Section()
                    {
                        Email,
                        Password,
                    },     
                    new Section()
                    { 
                        button 
                    }
                };
            }
            else
            {
                // initialize account controls
                Email = new EntryElement("Email", "Enter email", user != null ? user.Email : null);
                Password = new EntryElement("Password", "Enter password", user != null ? user.Password : null, true);

                var createButton = new ButtonListElement() 
                {
                    new Button() { Caption = "Create a new account",  Background = "Images/darkgreybutton.png", Clicked = CreateUserButton_Click },
                };
                createButton.Margin = 0f;
                var connectButton = new ButtonListElement() 
                {
                    new Button() { Caption = "Connect to an existing account", Background = "Images/darkgreybutton.png", Clicked = ConnectUserButton_Click }, 
                };
                connectButton.Margin = 0f;

                // create the account root element
                accountRootElement = new ThemedRootElement("Account")
                {
                    new Section()
                    {
                        Email,
                        Password,
                    },     
                    new Section()
                    { 
                        createButton,
                    },
                    new Section()
                    { 
                        connectButton
                    }
                };
            }   

            return accountRootElement;
        }

        private void InitializeComponent()
        {
            // create and push the view onto the nav stack
            var root = InitializeAccountSettings();
            dvc = new DialogViewController(root, true);
            //CreateRoot();
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
            controller.PushViewController(dvc, true);
        }

        private void CreateRoot()
        {
            // create the root for all settings.  note that the RootElement is cleared and recreated, as opposed to 
            // new'ed up from scratch.  this is by design - otherwise we don't get correct behavior on page transitions
            // when the theme is changed.
            if (dvc.Root == null)
                dvc.Root = new ThemedRootElement("Settings");
            else
            {
                // clean up any existing state for the existing root element
                if (dvc.Root.Count > 0)
                {
                    // force the dismissal of the keyboard in the account settings page if it was created
                    if (dvc.Root[0].Count > 0)
                    {
                        var tableView = dvc.Root[0][0].GetContainerTableView();
                        if (tableView != null)
                            tableView.EndEditing(true);
                    }
                }
                // clearing the root will cascade the Dispose call on its children
                dvc.Root.Clear();
            }

            // create the account root element
            dvc.Root.Add(new Section() { InitializeAccountSettings() });
        }
        
        #endregion Helpers
	}
}

