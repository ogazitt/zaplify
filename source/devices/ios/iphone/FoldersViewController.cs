using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using System.Drawing;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class FoldersViewController : UIViewController
	{
		private UIViewController thisController;
        public ObservableCollection<Folder> Folders { get; set; }
        private UITableView TableView;
        private UIToolbar Toolbar;
		
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}
     
		public FoldersViewController(UITableViewStyle style) : base()
		{
            TraceHelper.AddMessage("Folders: constructor");
			this.Title = NSBundle.MainBundle.LocalizedString ("Folders", "Folders");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/33-cabinet.png");
		}
        
        public override void ViewDidLoad()
        {
            TraceHelper.AddMessage("Folders: ViewDidLoad");
            base.ViewDidLoad();
            InitializeComponent();

            SortFolders();
            TableView.DataSource = new TableDataSource(this);
            TableView.Delegate = new TableDelegate(this);
            this.thisController = this;
        }
		
        public override void ViewDidUnload()
        {
            TraceHelper.AddMessage("Folders: ViewDidUnload");

            // Release any retained subviews of the main view.
            thisController = null;
            Folders = null;
            if (TableView != null)
                TableView.Dispose();
            TableView = null;
            if (Toolbar != null)
                Toolbar.Dispose();
            Toolbar = null;
            this.NavigationController.ViewControllers = new UIViewController[0];
            base.ViewDidUnload();
        }

        public override void ViewDidAppear(bool animated)
		{
            TraceHelper.AddMessage("Folders: ViewDidAppear");

            // set the background
            TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            Toolbar.TintColor = UIColorHelper.FromString(App.ViewModel.Theme.ToolbarBackground);
            NavigationController.NavigationBar.TintColor = UIColorHelper.FromString(App.ViewModel.Theme.ToolbarBackground);
            
            SortFolders();
            TableView.ReloadData();
			base.ViewDidAppear(animated);

            // HACK: touch the ViewControllers array to refresh it (in case the user popped the nav stack)
            // this is to work around a bug in monotouch (https://bugzilla.xamarin.com/show_bug.cgi?id=1889)
            // where the UINavigationController leaks UIViewControllers when the user pops the nav stack
            if (this.NavigationController.ViewControllers.Length > 0) {}
		}
		
        public override void ViewDidDisappear(bool animated)
        {
            TraceHelper.AddMessage("Folders: ViewDidDisappear");
            base.ViewDidDisappear(animated);
        }

		public override void DidReceiveMemoryWarning()
		{
            TraceHelper.AddMessage("Folders: DidReceiveMemoryWarning");

            // Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();
			
			// Release any cached data, images, etc that aren't in use.
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
        
        public void SortFolders()
        {
            Folders = App.ViewModel.Folders.OrderBy(f => f.SortOrder).ToObservableCollection();
        }
		
        void InitializeComponent()
        {
            // calculate the frame sizes
            float navBarHeight = new UINavigationController().NavigationBar.Bounds.Height;
            float tabBarHeight = 0;
            float availableHeight = View.Bounds.Height - navBarHeight - tabBarHeight;
            float toolbarHeight = navBarHeight;
            float tableHeight = availableHeight - toolbarHeight;
            
            // create the tableview
            TableView = new UITableView() { Frame = new RectangleF(0, 0, View.Bounds.Width, tableHeight) };
            TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            this.View.AddSubview(TableView);

            // create the toolbar
            Toolbar = new UIToolbar() { Frame = new RectangleF(0, tableHeight, View.Bounds.Width, toolbarHeight) };
            var flexSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);            
            
            //var addButton = new UIBarButtonItem("\u2795" /* big plus */ + "List", UIBarButtonItemStyle.Plain, delegate { 
            var addButton = new UIBarButtonItem(UIBarButtonSystemItem.Add, delegate { 
                FolderEditor folderEditor = new FolderEditor(this.NavigationController, null);
                folderEditor.PushViewController();
            });
            
            // create the settings button
            var settingsButtonImage = UIImageCache.GetUIImage("Images/20-gear2.png");
            var settingsButton = new UIBarButtonItem(settingsButtonImage, UIBarButtonItemStyle.Plain, delegate {
                //var settingsPage = new SettingsPage();
                //settingsPage.PushViewController();
                //this.NavigationController.PushViewController(settingsPage, false);
                var settingsPage = new MoreViewController(this.NavigationController);
                settingsPage.PushViewController();
            });

            // create the edit and done buttons
            // the edit button puts the table in edit mode, and the done button returns to normal mode
            var editButtonImage = UIImageCache.GetUIImage("Images/187-pencil.png");
            var editButton = new UIBarButtonItem(editButtonImage, UIBarButtonItemStyle.Plain, null);
            var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done);           
            editButton.Clicked += delegate
            { 
                if (TableView.Editing == false)
                {
                    TableView.SetEditing(true, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, doneButton, flexSpace, settingsButton, flexSpace }, false);
                }
            };
            doneButton.Clicked += delegate
            {
                if (TableView.Editing == true)
                {
                    TableView.SetEditing(false, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, editButton, flexSpace, settingsButton, flexSpace }, false);

                    // trigger a sync with the Service 
                    App.ViewModel.SyncWithService();   
                }
            };

            Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, editButton, flexSpace, settingsButton, flexSpace }, false);
            this.View.AddSubview(Toolbar);
        }
     
		#region Table Delegates
		
		/// <summary>
		/// UITableViewDelegate for this UITableViewController
		/// </summary>
		public class TableDelegate : UITableViewDelegate
		{      
			private FoldersViewController controller;
			 
			public TableDelegate(FoldersViewController c)
			{
				controller = c;  
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				UIViewController nextController = null;
			 
				Folder f = controller.Folders[indexPath.Row];			
				nextController = new ListViewController(this.controller.thisController, f, null);
		 
				if (nextController != null)
					controller.NavigationController.PushViewController(nextController,true);
			}
		}		
		
		/// <summary>
		/// Data Source delegate for this UITableViewController
		/// </summary>
		public class TableDataSource : UITableViewDataSource
		{
			private string cellID;
	        private FoldersViewController controller;

			public TableDataSource(FoldersViewController c)
			{
                controller = c;
				cellID = "folderCellID";
			}

			public override int RowsInSection (UITableView tableview, int section)
			{
				return controller.Folders.Count;
			}
	 
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var row = indexPath.Row;
				UITableViewCell cell = tableView.DequeueReusableCell (cellID);
	 
				if (cell == null) 
                {
					cell = new UITableViewCell (UITableViewCellStyle.Default, cellID);
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
					cell.ImageView.Image = UIImageCache.GetUIImage("Images/appbar.folder.rest.png");
				}
	 
				cell.TextLabel.Text = controller.Folders[row].Name;
	 
				return cell;
			}
            
            public override bool CanMoveRow(UITableView tableView, NSIndexPath indexPath)
            {
                return true;
            }
            
            public override void MoveRow(UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
            {
                int sourceRow = sourceIndexPath.Row;
                int destRow = destinationIndexPath.Row;
                float before, after;
                Folder folder = controller.Folders[sourceRow];
                
                // compute the new sort order for the folder based on the directiom of motion (up or down)
                if (sourceRow < destRow) 
                {
                    // moving down - new position is the average of target position plus next position
                    before = controller.Folders[destRow].SortOrder;
                    if (destRow >= controller.Folders.Count - 1)
                        after = before + 1000f;
                    else
                        after = controller.Folders[destRow + 1].SortOrder;
                }                
                else
                {
                    // moving up - new position is the average of target position plus previous position
                    after = controller.Folders[destRow].SortOrder;
                    if (destRow == 0)
                        before = 0;
                    else
                        before = controller.Folders[destRow - 1].SortOrder;
                }
                float newSortOrder = (before + after) / 2;
                
                // make a copy of the folder for the Update operation
                Folder folderCopy = new Folder(folder);
                folder.SortOrder = newSortOrder;

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Folder>() { folderCopy, folder },
                        BodyTypeName = typeof(Folder).Name,
                        ID = folder.ID
                    });
    
                // save the changes to local storage
                StorageHelper.WriteFolder(folder);
                StorageHelper.WriteFolders(App.ViewModel.Folders);
                
                // re-sort the current folder list, and have the table view update its UI
                controller.SortFolders(); 
                tableView.MoveRow(sourceIndexPath,destinationIndexPath);
            }
            
            public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
            {
                if (editingStyle == UITableViewCellEditingStyle.Delete)
                {
                    // get the folder from the local collection, and refresh it from the viewmodel in case 
                    // it changed underneath us
                    Folder folder = controller.Folders[indexPath.Row];
                    if (App.ViewModel.Folders.Any(f => f.ID == folder.ID))
                        folder = App.ViewModel.Folders.Single(f => f.ID == folder.ID);

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = folder
                        });
        
                    // save the changes to local storage
                    App.ViewModel.FolderDictionary.Remove(folder.ID);
                    App.ViewModel.Folders.Remove(folder);
                    StorageHelper.WriteFolders(App.ViewModel.Folders);
                    StorageHelper.DeleteFolder(folder);

                    // refresh the table UI
                    controller.SortFolders();
                    tableView.DeleteRows(new [] { indexPath }, UITableViewRowAnimation.Fade);
                }
            }
		}
		
		#endregion
	}
}
