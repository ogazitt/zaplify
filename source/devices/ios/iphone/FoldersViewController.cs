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
			this.Title = NSBundle.MainBundle.LocalizedString ("Folders", "Folders");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/33-cabinet.png");
            InitializeComponent();
		}
        
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }
		
		public override void ViewDidAppear (bool animated)
		{
            SortFolders();
            TableView.DataSource = new TableDataSource(this);
            TableView.Delegate = new TableDelegate(this);
            this.thisController = this;
            //this.TableView.BackgroundColor = UIColor.Purple;
            TableView.ReloadData();
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
        
        public void SortFolders()
        {
            Folders = App.ViewModel.Folders.OrderBy(f => f.SortOrder).ToObservableCollection();
        }
		
        void InitializeComponent()
        {
            // calculate the frame sizes
            float navBarHeight = new UINavigationController().NavigationBar.Bounds.Height;
            float tabBarHeight = new UITabBarController().TabBar.Bounds.Height;
            float availableHeight = View.Bounds.Height - navBarHeight - tabBarHeight;
            float toolbarHeight = navBarHeight;
            float tableHeight = availableHeight - toolbarHeight;
            
            // create the toolbar and the tableview
            TableView = new UITableView() { Frame = new RectangleF(0, 0, View.Bounds.Width, tableHeight) };
            this.View.AddSubview(TableView);
            Toolbar = new UIToolbar() { Frame = new RectangleF(0, tableHeight, View.Bounds.Width, toolbarHeight) };
            var flexSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);            
            
            //var addButton = new UIBarButtonItem("\u2795" /* big plus */ + "List", UIBarButtonItemStyle.Plain, delegate { 
            var addButton = new UIBarButtonItem(UIBarButtonSystemItem.Add, delegate { 
                FolderEditor folderEditor = new FolderEditor(this.NavigationController, null);
                folderEditor.PushViewController();
            });
            
            var editButton = new UIBarButtonItem(UIBarButtonSystemItem.Edit);
            var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done);           
            editButton.Clicked += delegate
            { 
                if (TableView.Editing == false)
                {
                    TableView.SetEditing(true, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, doneButton, flexSpace }, false);
                }
            };
            doneButton.Clicked += delegate
            {
                if (TableView.Editing == true)
                {
                    TableView.SetEditing(false, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, editButton, flexSpace }, false);

                    // trigger a sync with the Service 
                    App.ViewModel.SyncWithService();   
                }
            };

            Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, editButton, flexSpace }, false);
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
				// For more information on why this is necessary, see the Apple docs
				var row = indexPath.Row;
				UITableViewCell cell = tableView.DequeueReusableCell (cellID);
	 
				if (cell == null) {
					// See the styles demo for different UITableViewCellAccessory
					cell = new UITableViewCell (UITableViewCellStyle.Default, cellID);
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
					cell.ImageView.Image = new UIImage("Images/appbar.folder.rest.png");
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
                RequestQueue.EnqueueRequestRecord(
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
                    RequestQueue.EnqueueRequestRecord(
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
