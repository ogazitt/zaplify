using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class MoreViewController : UINavigationController
	{
        private DialogViewController dvc;
        
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public MoreViewController() : base()
		{
            TraceHelper.AddMessage("More: constructor");
			this.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/appbar.overflowdots.png");
		}
		
		public override void ViewDidLoad()
		{
            TraceHelper.AddMessage("More: ViewDidLoad");
            InitializeComponent();
            base.ViewDidLoad();
		}
		
        public override void ViewDidUnload()
        {
            TraceHelper.AddMessage("More: ViewDidUnload");
            Cleanup();
            base.ViewDidUnload();
        }
        
		public override void ViewDidAppear(bool animated)
		{
            TraceHelper.AddMessage("More: ViewDidAppear");
			
            if (dvc == null)
                InitializeComponent();

            // set the background
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            dvc.TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            
            base.ViewDidAppear(animated);  
		}

        public override void ViewDidDisappear(bool animated)
        {
            TraceHelper.AddMessage("More: ViewDidDisppear");
            base.ViewDidDisappear(animated);
        }
		
		public override void DidReceiveMemoryWarning()
		{
            TraceHelper.AddMessage("More: DidReceiveMemoryWarning");
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone) {
				return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
			} else {
				return true;
			}
		}	

        private void Cleanup()
        {
            if (dvc != null)
                this.ViewControllers = new UIViewController[0];
            dvc = null;
        }

        private void InitializeComponent()
        {
            var root = new RootElement("More")
            {
                new Section()
                {
                    new StyledStringElement("Add Folder", delegate 
                    { 
                        //var form = new FolderEditor(this.NavigationController, null);
                        var form = new FolderEditor(this, null);
                        form.PushViewController();
                    })
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    },
                    new StyledStringElement("Add List", delegate 
                    { 
                        //r form = new ListEditor(this.NavigationController, null, null, null);
                        var form = new ListEditor(this, null, null, null);
                        form.PushViewController();
                    })
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    },
                    new StyledStringElement("Erase All Data", delegate
                    {
                        MessageBoxResult result = MessageBox.Show(
                            "are you sure you want to erase all data on the phone?  unless you connected the phone to an account, your data will be not be retrievable.",
                            "confirm erasing all data",
                            MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.Cancel)
                            return;
            
                        App.ViewModel.EraseAllData();
                    })
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    },
                    new StyledStringElement("Debug", DebugPage)
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    }
                },
            };

            // create and push the dialog view onto the nav stack
            dvc = new DialogViewController(UITableViewStyle.Plain, root);
            dvc.NavigationItem.HidesBackButton = true;  
            dvc.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
            this.PushViewController(dvc, true);
        }
	}
}
