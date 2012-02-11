using System;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public class StyledHtmlElement : StyledStringElement
	{
		NSUrl nsUrl;
		static NSString hkey = new NSString ("StyledHtmlElement");
		UIWebView web;

		public StyledHtmlElement(string caption, string val, UITableViewCellStyle style, string url) : base (caption, val, style)
		{
			Url = url;
		}

		public StyledHtmlElement(string caption, string url) : base (caption)
		{
			Url = url;
		}

		public StyledHtmlElement(string caption, NSUrl url) : base (caption)
		{
			nsUrl = url;
		}

		public string Url {
			get {
				return nsUrl.ToString ();
			}
			set {
				nsUrl = new NSUrl (value);
			}
		}
		
		protected override NSString CellKey {
			get {
				return hkey;
			}
		}

		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = tv.DequeueReusableCell (CellKey);
			if (cell == null){
				cell = base.GetCell(tv);
				cell.SelectionStyle = UITableViewCellSelectionStyle.Blue;
			}
			//cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;

			return cell;
		}

		static bool NetworkActivity {
			set {
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = value;
			}
		}

		// We use this class to dispose the web control when it is not
		// in use, as it could be a bit of a pig, and we do not want to
		// wait for the GC to kick-in.
		class WebViewController : UIViewController {
			StyledHtmlElement container;

			public WebViewController (StyledHtmlElement container) : base ()
			{
				this.container = container;
			}

			public override void ViewWillDisappear (bool animated)
			{
				base.ViewWillDisappear (animated);
				NetworkActivity = false;
				if (container.web == null)
					return;

				container.web.StopLoading ();
				container.web.Dispose ();
				container.web = null;
			}

			public bool Autorotate { get; set; }

			public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
			{
				return Autorotate;
			}
		}

		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			var vc = new WebViewController (this) {
				Autorotate = dvc.Autorotate
			};
			web = new UIWebView (UIScreen.MainScreen.ApplicationFrame){
				BackgroundColor = UIColor.White,
				ScalesPageToFit = true,
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
			};
			web.LoadStarted += delegate {
				NetworkActivity = true;
				var indicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.White);
				vc.NavigationItem.RightBarButtonItem = new UIBarButtonItem(indicator);
				indicator.StartAnimating();
			};
			web.LoadFinished += delegate {
				NetworkActivity = false;
				vc.NavigationItem.RightBarButtonItem = null;
			};
			web.LoadError += (webview, args) => {
				NetworkActivity = false;
				vc.NavigationItem.RightBarButtonItem = null;
				if (web != null)
					web.LoadHtmlString (
						String.Format ("<html><center><font size=+5 color='red'>{0}:<br>{1}</font></center></html>",
						"An error occurred:", args.Error.LocalizedDescription), null);
			};
			vc.NavigationItem.Title = Caption;
			vc.View.AddSubview (web);

			dvc.ActivateController (vc);
			web.LoadRequest (NSUrlRequest.FromUrl (nsUrl));
		}
	}
}

