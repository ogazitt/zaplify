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
	public class SchedulePage : UINavigationController
	{
        private DialogViewController dvc;
        
		public SchedulePage()
		{
			// trace event
            TraceHelper.AddMessage("Schedule: constructor");

			this.Title = NSBundle.MainBundle.LocalizedString ("Schedule", "Schedule");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/83-calendar.png");
		}
		
        public override void ViewDidLoad()
        {
            TraceHelper.AddMessage("Schedule: ViewDidLoad");
            InitializeComponent();
            base.ViewDidLoad();
        }

        public override void ViewDidUnload()
        {
            TraceHelper.AddMessage("Schedule: ViewDidUnload");
            if (dvc != null)
                this.ViewControllers = new UIViewController[0];
            dvc = null;
            base.ViewDidUnload();
        }

		public override void ViewDidAppear (bool animated)
		{
            TraceHelper.AddMessage("Schedule: ViewDidAppear");

            // initialize an empty DVC if hasn't happened yet
            if (dvc == null)
                InitializeComponent();

            // refresh the dialog view controller with the new root (dvc disposes the old root)
            dvc.Root = CreateScheduleRoot();
            //dvc.ReloadData();
   
            // set the background
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            dvc.TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            
            base.ViewDidAppear (animated);
		}

        public override void ViewDidDisappear(bool animated)
        {
            TraceHelper.AddMessage("Schedule: ViewDidDisappear");
            base.ViewDidDisappear(animated);
        }

        private RootElement CreateScheduleRoot()
        {
            // initialize controls
            var now = DateTime.Today;
            var root = new RootElement("Schedule")
            {
                from it in App.ViewModel.Items
                    where it.Due != null && it.Due >= now
                    orderby it.Due ascending
                    group it by ((DateTime)it.Due).Date into g
                    select new Section (((DateTime) g.Key).ToShortDateString())
                    {
                        from hs in g
                            select (Element) new StringElement (((DateTime) hs.Due).ToShortTimeString(),
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
            return root;
        }

        private void InitializeComponent()
        {
            if (dvc == null)
            {
                // create and push the dialog view onto the nav stack
                dvc = new DialogViewController(UITableViewStyle.Plain, new RootElement("Schedule"));
                dvc.NavigationItem.HidesBackButton = true;  
                dvc.Title = NSBundle.MainBundle.LocalizedString ("Schedule", "Schedule");
                this.PushViewController(dvc, false);
            }
        }
	}
}

