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
		UITabBarController tabBarController;
		
        private static MainViewModel viewModel = null;
		private bool initialSync = false;

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
			var more = new UINavigationController(new MoreViewController());
			
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
            if (initialSync == false)
            {
                App.ViewModel.SyncWithService();
                initialSync = true;
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
	}
}

