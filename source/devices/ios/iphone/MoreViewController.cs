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
        private DialogViewController dvc = null;
        
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public MoreViewController() : base()
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/appbar.overflowdots.png");
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
		}
		
		public override void ViewDidAppear (bool animated)
		{
			var root = new RootElement("More")
            {
				new Section ()
				{
                    new StyledStringElement("Add Folder", delegate 
                    { 
                        var form = new FolderEditor(this, null);
                        form.PushViewController();
                    })
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    },
                    new StyledStringElement("Add List", delegate 
                    { 
                        var form = new ListEditor(this, null, null, null);
                        form.PushViewController();
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
			
            if (dvc == null)
            {
                // create and push the dialog view onto the nav stack
                dvc = new DialogViewController(UITableViewStyle.Plain, root);
                dvc.NavigationItem.HidesBackButton = true;  
                dvc.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
                this.PushViewController(dvc, false);
            }
            else
            {
                // refresh the dialog view controller with the new root
                var oldroot = dvc.Root;
                dvc.Root = root;
                oldroot.Dispose();
                dvc.ReloadData();
            }
   
            // set the background
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            dvc.TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            
            base.ViewDidAppear(animated);         
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
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
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone) {
				return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
			} else {
				return true;
			}
		}	
	}
}
