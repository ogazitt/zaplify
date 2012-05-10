using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
		private EntryElement Username;
		private EntryElement Password;
		private CheckboxElement MergeCheckbox;
        private bool accountOperationSuccessful = false;
        private DialogViewController dvc = null;
				
		public SettingsPage()
		{
			// trace event
            TraceHelper.AddMessage("Settings: constructor");

			this.Title = NSBundle.MainBundle.LocalizedString ("Settings", "Settings");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/20-gear2.png");
		}
		
		public override void ViewDidAppear (bool animated)
		{
			// trace event
            TraceHelper.AddMessage("Settings: ViewDidAppear");
			
            if (dvc == null)
                InitializeComponent();
   
            // set the background
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);

            // initialize controls
            user = App.ViewModel.User;  
            Username.Value = user != null ? user.Name : "";
            Password.Value = user != null ? user.Password : "";
			
			base.ViewDidAppear (animated);
		}
		
	 	public override void ViewDidDisappear (bool animated)
		{
			if (accountOperationSuccessful)
			{
			}
			base.ViewDidDisappear(animated);
		}
		
		void CreateUserButton_Click (object sender, EventArgs e)
        {
            // make sure the values are updated
			Username.FetchValue();
			Password.FetchValue();
			if (Username.Value == null || Username.Value == "" ||
                Password.Value == null || Password.Value == "")
            {
                MessageBox.Show("please enter a username, password, and email address");
                return;
            }

            if (MergeCheckbox.Value == false)
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

            WebServiceHelper.CreateUser(
                new User() { Name = Username.Value, Password = Password.Value, Email = Username.Value },
                new CreateUserCallbackDelegate(CreateUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }
		
	 	void SyncUserButton_Click (object sender, EventArgs e)
		{
            // make sure the values are updated
			Username.FetchValue();
			Password.FetchValue();
			//Email.FetchValue();
			if (MergeCheckbox.Value == true)
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

            User user = new User() { Name = Username.Value, Password = Password.Value, Email = Username.Value };

            WebServiceHelper.VerifyUserCredentials(
                user,
                new VerifyUserCallbackDelegate(VerifyUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));

		}

		#region Authentication callback methods

        public delegate void VerifyUserCallbackDelegate(User user, HttpStatusCode? code);
        private void VerifyUserCallback(User user, HttpStatusCode? code)
        {
			this.BeginInvokeOnMainThread(() =>
			{
				switch (code)
	            {
	                case HttpStatusCode.OK:
	                    MessageBox.Show(String.Format("successfully linked with {0} account; data sync will start automatically.", Username.Value));
	                    accountOperationSuccessful = true;
	                    user.Synced = true;
						// the server no longer echos the password in the payload so keep the local value when successful
						if (user.Password == null)
							user.Password = Password.Value;
	                    App.ViewModel.User = user;
	                    App.ViewModel.SyncWithService();
	                    break;
	                case HttpStatusCode.NotFound:
	                    MessageBox.Show(String.Format("user {0} not found", Username.Value));
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
	                    MessageBox.Show(String.Format("account {0} was not successfully paired", Username.Value));
	                    accountOperationSuccessful = false;
	                    break;
	            }
            });
        }

        public delegate void CreateUserCallbackDelegate(User user, HttpStatusCode? code);
        private void CreateUserCallback(User user, HttpStatusCode? code)
        {
            switch (code)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                    MessageBox.Show(String.Format("user account {0} successfully created", Username.Value));
                    accountOperationSuccessful = true;
                    user.Synced = true;
					// the server no longer echos the password in the payload so keep the local value when successful
					if (user.Password == null)
						user.Password = Password.Value;
                    App.ViewModel.User = user;
                    App.ViewModel.SyncWithService();
                    break;
                case HttpStatusCode.NotFound:
                    MessageBox.Show(String.Format("user {0} not found", Username.Value));
                    accountOperationSuccessful = false;
                    break;
                case HttpStatusCode.Conflict:
                    MessageBox.Show(String.Format("user {0} already exists", Username.Value));
                    accountOperationSuccessful = false;
                    break;
                case HttpStatusCode.InternalServerError:
                    MessageBox.Show(String.Format("user {0} was not created successfully (missing a field?)", Username.Value));
                    accountOperationSuccessful = false;
                    break;
                case null:
                    MessageBox.Show(String.Format("couldn't reach the server"));
                    accountOperationSuccessful = false;
                    break;
                default:
                    MessageBox.Show(String.Format("user {0} was not created", Username.Value));
                    accountOperationSuccessful = false;
                    break;
            }
        }
		
		#endregion
            
        #region Helpers
        
        private void InitializeComponent()
        {
            // initialize account controls
            user = App.ViewModel.User;  
            Username = new EntryElement("Email", "Enter email", "");
            Password = new EntryElement("Password", "Enter password", "", true);
            MergeCheckbox = new CheckboxElement("Merge local data?", true);
            
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
                        var phoneSettingsItemCopy = ClientSettingsHelper.GetPhoneSettingsItem(App.ViewModel.ClientSettings);

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
                        RequestQueue.EnqueueRequestRecord(new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Update,
                            Body = new List<Item>() { phoneSettingsItemCopy, phoneSettingsItem },
                            BodyTypeName = "Item",
                            ID = phoneSettingsItem.ID
                        });
            
                        // sync with the server
                        App.ViewModel.SyncWithService();
                    };     
                }
            };

            var root = new RootElement("Settings")
            {
                new Section()
                {
                    new ThemedRootElement("Account")
                    {
                        new Section()
                        {
                            Username,
                            Password,
                            MergeCheckbox,
                        },
                        new Section()
                        {
                            new ButtonListElement() 
                            {
                                new Button() { Caption = "Create Account",  Background = "Images/darkgreybutton.png", Clicked = CreateUserButton_Click },
                                new Button() { Caption = "Pair Account", Background = "Images/darkgreybutton.png", Clicked = SyncUserButton_Click }, 
                            },
                        },
                    },                    
                },
            };
            root[0].AddAll(elements);

            // push the view onto the nav stack
            dvc = new DialogViewController(root);
            dvc.NavigationItem.HidesBackButton = true;  
            dvc.Title = NSBundle.MainBundle.LocalizedString ("Settings", "Settings");
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
            this.PushViewController(dvc, false);
        }
        
        #endregion Helpers
	}
}

