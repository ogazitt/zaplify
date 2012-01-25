using MonoTouch.UIKit;
using System.Drawing;
using System;
using MonoTouch.Foundation;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization.Json;

namespace builtsteady.zaplify.devices.iphone
{
	public partial class FirstViewController : UIViewController
	{
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public FirstViewController ()
			: base (UserInterfaceIdiomIsPhone ? "FirstViewController_iPhone" : "FirstViewController_iPad", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("First", "First");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/first");
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			User user = new User() 
			{
				Name = "ogazitt",
				Password = "zrc022..",
				Email = "ogazitt@gmail.com"
			};
			TraceHelper.AddMessage("Omri was here");
			string str;
			
			MemoryStream ms = new MemoryStream();
			DataContractJsonSerializer dc = new DataContractJsonSerializer(typeof(User));
			dc.WriteObject(ms, user);

			ms.Position = 0;
			StreamReader reader = new StreamReader(ms);
			str = reader.ReadToEnd();

			ms.Position = 0;
			User user2 = (User) dc.ReadObject(ms);

			UIAlertView msgbox = new UIAlertView("serialized string", str, null, "OK", null);
			msgbox.Show ();
			//any additional setup after loading the view, typically from a nib.
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
