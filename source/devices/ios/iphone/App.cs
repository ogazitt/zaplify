using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.ClientHelpers;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("App")]
	public partial class App : UIApplicationDelegate
	{
		// class-level declarations
		UIWindow window;
		static UITabBarController tabBarController;
		
        private static MainViewModel viewModel = null;
		private bool initialSyncAlreadyHappened = false;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }

        public static UITabBarController TabBarController { get { return tabBarController; } }
		
		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// trace event
			TraceHelper.StartMessage("App: Loaded");
          
            // if data isn't loaded from storage yet, load the app data
            if (!App.ViewModel.IsDataLoaded)
            {
                // Load app data from local storage (user creds, about tab data, constants, item types, folders, etc)
                App.ViewModel.LoadData();
            }
         
            // create pages
			var add = new AddPage();
			var calendar = new SchedulePage();
			var folders = new UINavigationController(new FoldersViewController(UITableViewStyle.Plain));
			var settings = new SettingsPage();
			var more = new MoreViewController();
			
			tabBarController = new UITabBarController ();
			tabBarController.ViewControllers = new UIViewController [] {
				add,
				calendar,
				folders,
				settings,
				more,
			};
			
			tabBarController.ViewControllerSelected += (sender, e) => 
			{
				UITabBarController v = (UITabBarController) sender;
				v.LoadView();
			};
			
            // if haven't synced with web service yet, try now
            if (initialSyncAlreadyHappened == false)
            {
                App.ViewModel.SyncWithService();
                initialSyncAlreadyHappened = true;

                // if there's a home tab set, switch to it now
                var homeTab = ClientSettingsHelper.GetHomeTab(App.ViewModel.ClientSettings);
                if (homeTab != null && homeTab != "Add")
                    SelectTab(homeTab);
            }

            // create a new window instance based on the screen size
            window = new UIWindow (UIScreen.MainScreen.Bounds);
            if (UIDevice.CurrentDevice.CheckSystemVersion(4, 0)) 
                window.RootViewController = tabBarController;
            else
                window.AddSubview(tabBarController.View);
			window.MakeKeyAndVisible();
			
            // trace exit
            TraceHelper.AddMessage("Exiting App Loaded");

			return true;
		}

        private void SelectTab(string tabString)
        {
            switch (tabString)
            {
                case "Add":
                    tabBarController.SelectedIndex = 0;  // switch to add tab
                    break;
                case "Schedule":
                    tabBarController.SelectedIndex = 1;  // switch to schedule tab
                    break;
                case "Folders":
                    tabBarController.SelectedIndex = 2;  // switch to folders tab
                    break;
                case "Tags":
                    tabBarController.SelectedIndex = 3;  // switch to tags tab
                    break;
                default:
                    break;
            }
        }

	}
}

