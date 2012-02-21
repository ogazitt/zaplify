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
	public class CalendarPage : UINavigationController
	{
		public CalendarPage()
		{
			// trace event
            TraceHelper.AddMessage("Calendar: constructor");

			this.Title = NSBundle.MainBundle.LocalizedString ("Calendar", "Calendar");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/83-calendar.png");
		}
		
		public override void ViewDidAppear (bool animated)
		{
			// trace event
            TraceHelper.AddMessage("Calendar: ViewDidAppear");
			
			// initialize controls
			var now = DateTime.Today;
			var root = new RootElement("Calendar")
			{
		        from it in App.ViewModel.Items
			        where it.Due != null && it.Due >= now
			        orderby it.Due ascending
			        group it by it.Due into g
			        select new Section (((DateTime) g.Key).ToString("d")) 
					{
			            from hs in g
			               	select (Element) new StringElement (((DateTime) hs.Due).ToString("d"),
						        delegate 
						        {
									ItemPage itemPage = new ItemPage(this, hs);
									itemPage.PushViewController();
								})
							{ 
								Value = hs.Name
							}						                             
					}
		    };
			
			// create and push the dialog view onto the nav stack
			var dvc = new DialogViewController(UITableViewStyle.Plain, root);
			dvc.NavigationItem.HidesBackButton = true;	
			dvc.Title = NSBundle.MainBundle.LocalizedString ("Calendar", "Calendar");
			this.PushViewController(dvc, false);
			
			base.ViewDidAppear (animated);
		}
	}
}

