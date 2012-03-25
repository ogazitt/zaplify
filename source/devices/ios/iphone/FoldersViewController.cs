using BuiltSteady.Zaplify.Devices.ClientEntities;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class FoldersViewController : UITableViewController
	{
		private UIViewController thisController;
		
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public FoldersViewController(UITableViewStyle style) : base(style)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Folders", "Folders");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/33-cabinet.png");
		}
		
		public override void ViewDidAppear (bool animated)
		{
            TableView.DataSource = new TableDataSource();
            TableView.Delegate = new TableDelegate(this);
            this.thisController = this;

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
				UITableViewController nextController = null;
			 
				Folder f = App.ViewModel.Folders[indexPath.Row];			
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
	 
			public TableDataSource ()
			{
				cellID = "folderCellID";
			}
	 	 
			public override int RowsInSection (UITableView tableview, int section)
			{
				return App.ViewModel.Folders.Count;
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
	 
				cell.TextLabel.Text = App.ViewModel.Folders[row].Name;
	 
				return cell;
			}
		}
		
		#endregion
	}
}
