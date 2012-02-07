using MonoTouch.UIKit;
using System.Drawing;
using System;
using MonoTouch.Foundation;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientViewModels;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class ListViewController : UITableViewController
	{
		private Folder folder;
		private Item list;
		private Guid? listID;
		private UIViewController parentController;
		
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}
		 		
		public ListViewController(UIViewController parent, Folder f, Guid? currentID) : base(UITableViewStyle.Plain)
		{
			this.Title = f.Name;
			folder = f;
			listID = currentID;
			parentController = parent;
		}
		
		public override void ViewDidLoad () 
		{
			TableView.DataSource = new ListTableDataSource(this);
			TableView.Delegate = new ListTableDelegate(this);

			base.ViewDidLoad ();

			// load the folder and construct the list of Items that will be rendered
			try
            {
                folder = App.ViewModel.LoadFolder(folder.ID);

                // if the load failed, this folder has been deleted
                if (folder == null)
                {
                    // the folder isn't found - this can happen when the folder we were just 
                    // editing was removed in FolderEditor, which then goes back to ListPage.
                    // this will send us back to the MainPage which is appropriate.

                    // trace page navigation
                    TraceHelper.StartMessage("ListPage: Navigate back");

                    // navigate back
					this.NavigationController.PopToViewController(parentController, true);
                    return;
                }

                // get the current list name
                string listName = null;
                if (listID == null || listID == Guid.Empty)
                    listName = folder.Name;
                else
                    listName = folder.Items.Single(i => i.ID == listID).Name;

                // construct a synthetic item that represents the list of items for which the 
                // ParentID is the parent.  this also works for the root list in a folder, which
                // is represented with a ParentID of Guid.Empty.
                list = new Item()
                {
                    ID = (listID == null) ? Guid.Empty : (Guid) listID,
                    Name = listName,
                    FolderID = folder.ID,
                    IsList = true,
                    Items = folder.Items.Where(i => i.ParentID == listID).ToObservableCollection()
                };
            }
            catch (Exception ex)
            {
                // the folder isn't found - this can happen when the folder we were just 
                // editing was removed in FolderEditor, which then goes back to ListPage.
                // this will send us back to the MainPage which is appropriate.

                // trace page navigation
                TraceHelper.StartMessage(String.Format("ListPage: Navigate back (exception: {0})", ex.Message));

                // navigate back
				this.NavigationController.PopToViewController(parentController, true);
                return;
            }

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
		/// List table delegate is an inner class of ListViewController
		/// that acts as the UITableViewDelegate for this TableViewController
		/// </summary>
		public class ListTableDelegate : UITableViewDelegate
		{      
			private ListViewController controller;
			 
			public ListTableDelegate(ListViewController c)
			{
				controller = c;  
			}
			 
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				Item item = controller.list.Items[indexPath.Row];
				if (item != null)
				{
					if (item.IsList == true)
		            {
		                // Navigate to the list page
						UITableViewController nextController = new ListViewController(controller, controller.folder, item.ID);	
		            	TraceHelper.StartMessage("ListPage: Navigate to List");
						controller.NavigationController.PushViewController(nextController,true);
		            }
		            else
		            {
		                ItemPage itemPage = new ItemPage(controller.NavigationController, item);
						TraceHelper.StartMessage("ListPage: Navigate to Item");
						itemPage.NavigateTo();
		            }
				}
			}
		}
				
		/// <summary>
		/// List table data source is an inner class that acts as the 
		/// Data Source delegate for this UITableViewController
		/// </summary>
		public class ListTableDataSource : UITableViewDataSource
		{
			private string cellID;
			private ListViewController controller;
	 
			public ListTableDataSource (ListViewController c)
			{
				cellID = "listCellID";
				controller = c;
			}
	 
			public override int RowsInSection (UITableView tableview, int section)
			{
				return controller.list.Items.Count;
			}
	 
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				
				var row = indexPath.Row;
				UITableViewCell cell = tableView.DequeueReusableCell (cellID);
	 
				if (cell == null) {
					// See the styles demo for different UITableViewCellAccessory
					cell = new UITableViewCell (UITableViewCellStyle.Default, cellID);
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}
	 
				cell.TextLabel.Text = controller.list.Items[row].Name;
	 
				return cell;
			}
		}
		
		#endregion
	}
}
