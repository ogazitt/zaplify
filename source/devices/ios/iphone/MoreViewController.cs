using System;
using System.Drawing;
using System.IO;
using System.Linq;
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
	public partial class MoreViewController
	{
        private DialogViewController dvc;
        private UINavigationController controller;
        
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public MoreViewController(UINavigationController c)
		{
            TraceHelper.AddMessage("More: constructor");
			//this.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
			//this.TabBarItem.Image = UIImage.FromBundle ("Images/appbar.overflowdots.png");
            controller = c;
		}
		
        public void PushViewController()
        {
            // trace event
            TraceHelper.AddMessage("Account: PushViewController");            
            InitializeComponent();
        }
        
        /*
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
        */

        private void Cleanup()
        {
            //if (dvc != null)
            //    this.ViewControllers = new UIViewController[0];
            dvc = null;
        }

        private void InitializeComponent()
        {
            var root = new RootElement("Settings")
            {
                new Section()
                {
                    new StyledStringElement("Account", delegate 
                    { 
                        var form = new AccountPage(controller);
                        form.PushViewController();
                    })
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    },
                    new StyledStringElement("Add Folder", delegate 
                    { 
                        //var form = new FolderEditor(this.NavigationController, null);
                        //var form = new FolderEditor(controller, null);
                        var form = new FolderEditor(controller, null);
                        form.PushViewController();
                    })
                    {
                        Accessory = UITableViewCellAccessory.DisclosureIndicator,
                        BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground)
                    },
                    new StyledStringElement("Add List", delegate 
                    { 
                        //r form = new ListEditor(this.NavigationController, null, null, null);
                        //var form = new ListEditor(this, null, null, null);
                        var form = new ListEditor(controller, null, null, null);
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

            // initialize other phone settings by creating a radio element list for each phone setting
            var elements = (from ps in PhoneSettings.Settings.Keys select (Element) new ThemedRootElement(ps, new RadioGroup(null, 0))).ToList();

            // loop through the root elements we just created and create their substructure
            foreach (ThemedRootElement rootElement in elements)
            {
                // set the ViewDisappearing event on child DialogViewControllers to refresh the theme
                rootElement.OnPrepare += (sender, e) => 
                {
                    DialogViewController viewController = e.ViewController as DialogViewController;
                    viewController.ViewDisappearing += delegate 
                    { 
                        var parent = viewController.Root.GetImmediateRootElement() as RootElement;
                        if (parent != null && parent.TableView != null)
                        {
                            parent.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground); 
                            parent.TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
                        }
                    };
                };
                
                // add a radio element for each of the setting values for this setting type
                var section = new Section();
                section.AddAll(from val in PhoneSettings.Settings[rootElement.Caption].Values select (Element) new RadioEventElement(val.Name));
                rootElement.Add(section);

                // initialize the value of the radio button
                var currentValue = PhoneSettingsHelper.GetPhoneSetting(App.ViewModel.PhoneClientFolder, rootElement.Caption);
                var bindingList = PhoneSettings.Settings[rootElement.Caption].Values;
                int selectedIndex = 0;
                if (currentValue != null && bindingList.Any(ps => ps.Name == currentValue))
                {
                    var selectedValue = bindingList.Single(ps => ps.Name == currentValue);
                    selectedIndex = bindingList.IndexOf(selectedValue);
                }
                rootElement.RadioSelected = selectedIndex;
                
                // attach an event handler that saves the selected setting
                foreach (var ree in rootElement[0].Elements)
                {
                    var radioEventElement = (RadioEventElement) ree;
                    radioEventElement.OnSelected += delegate {
                        // make a copy of the existing version of phone settings
                        var phoneSettingsItemCopy = new Item(PhoneSettingsHelper.GetPhoneSettingsItem(App.ViewModel.PhoneClientFolder), true);

                        // find the key and the valuy for the current setting
                        var radioRoot = radioEventElement.GetImmediateRootElement();
                        var key = radioRoot.Caption;
                        var phoneSetting = PhoneSettings.Settings[key];
                        string value = phoneSetting.Values[radioRoot.RadioSelected].Name;
      
                        // store the new phone setting
                        PhoneSettingsHelper.StorePhoneSetting(App.ViewModel.PhoneClientFolder, key, value);
            
                        // get the new version of phone settings
                        var phoneSettingsItem = PhoneSettingsHelper.GetPhoneSettingsItem(App.ViewModel.PhoneClientFolder);

                        // queue up a server request
                        if (App.ViewModel.PhoneClientFolder.ID != Guid.Empty)
                        {
                            RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                            {
                                ReqType = RequestQueue.RequestRecord.RequestType.Update,
                                Body = new List<Item>() { phoneSettingsItemCopy, phoneSettingsItem },
                                BodyTypeName = "Item",
                                ID = phoneSettingsItem.ID
                            });
                        }
            
                        // sync with the server
                        App.ViewModel.SyncWithService();

                        // if user changed theme, refresh the navigation controller color
                        if (key == PhoneSettings.Theme)
                        {
                            controller.NavigationBar.TintColor = UIColorHelper.FromString(App.ViewModel.Theme.ToolbarBackground);
                            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
                            dvc.TableView.SetNeedsDisplay();
                            //dvc.TableView.ReloadData();
                        }
                    }; 
                }
            };

            // attach the other settings pages to the root element using Insert instead of AddAll so as to disable animation
            foreach (var e in elements)
                root[0].Insert(root[0].Count, UITableViewRowAnimation.None, e);

            // create and push the dialog view onto the nav stack
            dvc = new DialogViewController(UITableViewStyle.Plain, root, true);
            //dvc.NavigationItem.HidesBackButton = false;  
            //dvc.Title = NSBundle.MainBundle.LocalizedString ("More", "More");
            //this.PushViewController(dvc, true);
            controller.PushViewController(dvc, true);
        }
	}
}
