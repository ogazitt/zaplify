using System.Drawing;
using System;
using System.Net;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientViewModels;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class SettingsViewController : UIViewController
	{
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public SettingsViewController ()
			: base (UserInterfaceIdiomIsPhone ? "SettingsViewController_iPhone" : "SettingsViewController_iPad", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Settings", "Settings");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/20-gear2.png");
		}

		private bool accountTextChanged = false;
        private bool accountOperationSuccessful = false;
		
		partial void CreateUserButton_Click (MonoTouch.Foundation.NSObject sender)
        {
            if (Username.Text == null || Username.Text == "" ||
                Password.Text == null || Password.Text == "" ||
                Email.Text == null || Email.Text == "")
            {
                MessageBox.Show("please enter a username, password, and email address");
                return;
            }

            if (MergeCheckbox.On == false)
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
                new User() { Name = Username.Text, Password = Password.Text, Email = Email.Text },
                new CreateUserCallbackDelegate(CreateUserCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(App.ViewModel.NetworkOperationInProgressCallback));
        }
		
		partial void SyncUserButton_Click (MonoTouch.Foundation.NSObject sender)
		{
			if (MergeCheckbox.On == true)
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

            User user = new User() { Name = Username.Text, Password = Password.Text, Email = Email.Text };

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
#if WINDOWS_PHONE
            });
#endif
        }
		
		#endregion
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// make the password textbox show circles instead of text
			Password.SecureTextEntry = true;
			
			User user = App.ViewModel.User;
			if (user != null)
			{
				Username.Text = user.Name;
				Password.Text = user.Password;
				Email.Text = user.Email;				
			}
		}
		
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return true;
		}
	}
}

