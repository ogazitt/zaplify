using MonoTouch.UIKit;
using System.Drawing;
using System;
using MonoTouch.Foundation;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using MonoTouch.Dialog;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class MoreViewController : UIViewController
	{
		private UIViewController moreController;
		
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public MoreViewController() : base()
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/second.png");
		}
		
		public override void ViewDidLoad ()
		{
			this.moreController = this;
			
			base.ViewDidLoad ();
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			
			var menu = new RootElement ("More"){
				new Section ()
				{
					new StyledStringElement ("Debug", DebugPage)
					{
						Accessory = UITableViewCellAccessory.DisclosureIndicator,
					}
				},
			};
			
			var dv = new DialogViewController (menu) {
				Style = UITableViewStyle.Plain,
				Autorotate = true
			};
			this.NavigationController.PushViewController (dv, true);				
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
