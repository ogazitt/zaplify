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
	public class SettingsPage : UIViewController
	{
		private User user;
		private EntryElement Username;
		private EntryElement Password;
		private EntryElement Email;
		private CheckboxElement MergeCheckbox;
		
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
			
			// initialize controls
			user = App.ViewModel.User;	
			Username = new EntryElement("Username", "Enter username", user.Name);
			Password = new EntryElement("Password", "Enter password", user.Password, true);
			Email = new EntryElement("Email", "Enter email", user.Email);
			MergeCheckbox = new CheckboxElement("Merge local data?", true);
			
			var root = new RootElement("Settings")
			{
				new Section("Account")
				{
					Username,
					Password,
					Email,
					MergeCheckbox,
				},
				new Section()
				{
					new ButtonListElement() 
					{
						new Button() { Caption = "Create Account", Clicked = CreateUserButton_Click },
						new Button() { Caption = "Pair Account", Clicked = SyncUserButton_Click }, 
					},
				},
			};
			var dvc = new DialogViewController(root);
			dvc.NavigationItem.HidesBackButton = true;
			
			// push the settings view onto the nav stack
			this.NavigationController.PushViewController(dvc, false);
			//this.NavigationController.NavigationBarHidden = true; 
			//this.NavigationController.PushViewController (dvc, true);

			base.ViewDidAppear (animated);
		}
		
		private bool accountTextChanged = false;
        private bool accountOperationSuccessful = false;
		
		void CreateUserButton_Click (object sender, EventArgs e)
        {
            if (Username.Value == null || Username.Value == "" ||
                Password.Value == null || Password.Value == "" ||
                Email.Value == null || Email.Value == "")
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
                new User() { Name = Username.Value, Password = Password.Value, Email = Email.Value },
                new CreateUserCallbackDelegate(CreateUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }
		
	 	void SyncUserButton_Click (object sender, EventArgs e)
		{
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

            User user = new User() { Name = Username.Value, Password = Password.Value, Email = Email.Value };

            WebServiceHelper.VerifyUserCredentials(
                user,
                new VerifyUserCallbackDelegate(VerifyUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));

		}

		#region Authentication callback methods

        public delegate void VerifyUserCallbackDelegate(User user, HttpStatusCode? code);
        private void VerifyUserCallback(User user, HttpStatusCode? code)
        {
#if WINDOWS_PHONE
            // run this on the UI thread
			Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
#endif
				switch (code)
	            {
	                case HttpStatusCode.OK:
	                    MessageBox.Show(String.Format("successfully linked with {0} account; data sync will start automatically.", Username.Value));
	                    accountOperationSuccessful = true;
	                    user.Synced = true;
	                    App.ViewModel.User = user;
	                    App.ViewModel.SyncWithService();
	                    break;
	                case HttpStatusCode.NotFound:
	                    MessageBox.Show(String.Format("user {0} not found", Username.Value));
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
	                    MessageBox.Show(String.Format("account {0} was not successfully paired", Username.Value));
	                    accountOperationSuccessful = false;
	                    break;
	            }
#if WINDOWS_PHONE
            });
#endif
        }

        public delegate void CreateUserCallbackDelegate(User user, HttpStatusCode? code);
        private void CreateUserCallback(User user, HttpStatusCode? code)
        {
#if WINDOWS_PHONE
            // run this on the UI thread
			Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
#endif
                switch (code)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        MessageBox.Show(String.Format("user account {0} successfully created", Username.Value));
                        accountOperationSuccessful = true;
                        user.Synced = true;
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
#if WINDOWS_PHONE
            });
#endif
        }
		
		#endregion
	}
}

