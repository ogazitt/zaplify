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
	public class SettingsPage : UINavigationController
	{
		private User user;
		private Element Email;
		private Element Password;
        private ThemedRootElement Root;
        private ThemedRootElement AccountRootElement;
        private bool accountOperationSuccessful = false;
        private DialogViewController dvc = null;

        private bool IsConnected
        {
            get
            {
                return App.ViewModel.User != null && App.ViewModel.User.Synced;
            }
        }

		public SettingsPage()
		{
			// trace event
            TraceHelper.AddMessage("Settings: constructor");

			this.Title = NSBundle.MainBundle.LocalizedString ("Settings", "Settings");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/20-gear2.png");
		}
		
		public override void ViewDidAppear(bool animated)
		{
			// trace event
            TraceHelper.AddMessage("Settings: ViewDidAppear");
			
            if (dvc == null)
                InitializeComponent();
            else
                InitializeRoot();

            // set the background
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
			
			base.ViewDidAppear(animated);
		}
		
	 	public override void ViewDidDisappear(bool animated)
		{
			if (accountOperationSuccessful)
			{
			}
			base.ViewDidDisappear(animated);
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
                InitializeRoot();
                dvc.ReloadData();
            }
        }

		#region Authentication callback methods

        public delegate void VerifyUserCallbackDelegate(User user, HttpStatusCode? code);
        private void VerifyUserCallback(User user, HttpStatusCode? code)
        {
			this.BeginInvokeOnMainThread(() =>
			{
                string email = ((EntryElement)Email).Value;
                string pswd = ((EntryElement)Password).Value;
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
                        RequestQueue.PrepareQueueForAccountConnect(RequestQueue.UserQueue);
	                    App.ViewModel.SyncWithService();
	                    break;
	                case HttpStatusCode.NotFound:
	                    MessageBox.Show(String.Format("user {0} not found", email));
	                    accountOperationSuccessful = false;
	                    break;
	                case HttpStatusCode.Forbidden:
	                    MessageBox.Show(String.Format("incorrect username or password"));
	                    accountOperationSuccessful = false;
	                    break;
	                case null:
	                    MessageBox.Show(String.Format("couldn't reach the server"));
	                    accountOperationSuccessful = false;
	                    break;
	                default:
	                    MessageBox.Show(String.Format("account {0} was not successfully paired", email));
	                    accountOperationSuccessful = false;
	                    break;
	            }

                // update UI if successful
                if (accountOperationSuccessful)
                {
                    dvc.NavigationController.PopViewControllerAnimated(true);
                    InitializeRoot();
                    dvc.ReloadData();
                }
            });
        }

        public delegate void CreateUserCallbackDelegate(User user, HttpStatusCode? code);
        private void CreateUserCallback(User user, HttpStatusCode? code)
        {
            this.BeginInvokeOnMainThread(() =>
            {
                string email = ((EntryElement)Email).Value;
                string pswd = ((EntryElement)Password).Value;
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
                        MessageBox.Show(String.Format("user {0} was not created", email));
                        accountOperationSuccessful = false;
                        break;
                }
    
                // update UI if successful
                if (accountOperationSuccessful)
                {
                    dvc.NavigationController.PopViewControllerAnimated(true);
                    InitializeRoot();
                    dvc.ReloadData();
                }
            });
        }
		
		#endregion
            
        #region Helpers

        private void InitializeAccountSettings()
        {
            user = App.ViewModel.User;  

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
                AccountRootElement = new ThemedRootElement("Account")
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
                AccountRootElement = new ThemedRootElement("Account")
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

            Root[0].Insert(0, UITableViewRowAnimation.None, AccountRootElement);
        }

        private void InitializeComponent()
        {
            InitializeRoot();

            // push the view onto the nav stack
            dvc = new DialogViewController(Root);
            dvc.NavigationItem.HidesBackButton = true;  
            dvc.Title = NSBundle.MainBundle.LocalizedString ("Settings", "Settings");
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
            this.PushViewController(dvc, false);
        }

        private void InitializeRoot()
        {
            // create the root for all settings 
            if (Root == null)
                Root = new ThemedRootElement("Settings");
            else
                Root.Clear();
            Root.Add(new Section());

            // create the account root element
            InitializeAccountSettings();

            // initialize other phone settings by creating a radio element list for each phone setting
            var elements = (from ps in PhoneSettings.Settings.Keys select (Element) new ThemedRootElement(ps, new RadioGroup(null, 0))).ToList();

            // loop through the root elements we just created and create their substructure
            foreach (ThemedRootElement rootElement in elements)
            {
                // set the ViewDisappearing event on child DialogViewControllers to refresh the theme
                rootElement.OnPrepare += (sender, e) => 
                {
                    DialogViewController viewController = e.ViewController as DialogViewController;
                    viewController.ViewDisappearing += delegate 
                    { 
                        var parent = viewController.Root.GetImmediateRootElement() as RootElement;
                        parent.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground); 
                    };
                };
                
                // add a radio element for each of the setting values for this setting type
                var section = new Section();
                section.AddAll(from val in PhoneSettings.Settings[rootElement.Caption].Values select (Element) new RadioEventElement(val.Name));
                rootElement.Add(section);

                // initialize the value of the radio button
                var currentValue = ClientSettingsHelper.GetPhoneSetting(App.ViewModel.ClientSettings, rootElement.Caption);
                var bindingList = PhoneSettings.Settings[rootElement.Caption].Values;
                int selectedIndex = 0;
                if (currentValue != null && bindingList.Any(ps => ps.Name == currentValue))
                {
                    var selectedValue = bindingList.Single(ps => ps.Name == currentValue);
                    selectedIndex = bindingList.IndexOf(selectedValue);
                }
                rootElement.RadioSelected = selectedIndex;
                
                // attach an event handler that saves the selected setting
                foreach (var ree in rootElement[0].Elements)
                {
                    var radioEventElement = (RadioEventElement) ree;
                    radioEventElement.OnSelected += delegate {
                        // make a copy of the existing version of phone settings
                        var phoneSettingsItemCopy = new Item(ClientSettingsHelper.GetPhoneSettingsItem(App.ViewModel.ClientSettings), true);

                        // find the key and the valuy for the current setting
                        var radioRoot = radioEventElement.GetImmediateRootElement();
                        var key = radioRoot.Caption;
                        var phoneSetting = PhoneSettings.Settings[key];
                        string value = phoneSetting.Values[radioRoot.RadioSelected].Name;
      
                        // store the new phone setting
                        ClientSettingsHelper.StorePhoneSetting(App.ViewModel.ClientSettings, key, value);
            
                        // get the new version of phone settings
                        var phoneSettingsItem = ClientSettingsHelper.GetPhoneSettingsItem(App.ViewModel.ClientSettings);

                        // queue up a server request
                        if (App.ViewModel.ClientSettings.ID != Guid.Empty)
                        {
                            RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                            {
                                ReqType = RequestQueue.RequestRecord.RequestType.Update,
                                Body = new List<Item>() { phoneSettingsItemCopy, phoneSettingsItem },
                                BodyTypeName = "Item",
                                ID = phoneSettingsItem.ID
                            });
                        }
            
                        // sync with the server
                        App.ViewModel.SyncWithService();
                    };     
                }
            };

            // attach the other settings pages to the root element using Insert instead of AddAll so as to disable animation
            foreach (var e in elements)
                Root[0].Insert(Root[0].Count, UITableViewRowAnimation.None, e);
        }
        
        #endregion Helpers
	}
}

