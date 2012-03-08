using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

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
			folder = f;
            listID = (currentID == Guid.Empty) ? (Guid?) null : (Guid?) currentID;
			parentController = parent;
		}
		
		public override void ViewDidAppear(bool animated)
		{
			TableView.DataSource = new ListTableDataSource(this);
			TableView.Delegate = new ListTableDelegate(this);

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
				this.Title = listName;

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
			
			// reload the data
			TableView.ReloadData();

			// call the base class implementation
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
		
		#region Event Handlers
		
		/// <summary>
        /// Handle click event on Complete checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompleteCheckbox_Click(object sender, EventArgs e)
        {
			// trace data
            TraceHelper.StartMessage("CompleteCheckbox Click");

            // get the item that was just updated, and ensure the Complete flag is in the correct state
            //Item item = folder.Items.Single<Item>(t => t.ID == itemID);
			UICheckbox checkBox = (UICheckbox) sender;
			Item item = (Item) checkBox.UserState;

            // create a copy of that item
            Item itemCopy = new Item(item);

            // toggle the complete flag to reflect the checkbox click
            item.Complete = !item.Complete;

            // bump the last modified timestamp
            item.LastModified = DateTime.UtcNow;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { itemCopy, item },
                    BodyTypeName = "Item",
                    ID = item.ID
                });
            
            // reorder the item in the folder and the ListBox
            //ListHelper.ReOrderItem(list, item);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace data
            TraceHelper.AddMessage("Finished CompleteCheckbox Click");
        }

		#endregion
		
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
				// turn off the highlight for the cell that's selected
				tableView.CellAt (indexPath).SetHighlighted(false, false);
				
				// get the item at the row and depending on whether it's a list 
				// or a singleton, navigate to the right page
				Item item = controller.list.Items[indexPath.Row];
				if (item != null)
				{
					if (item.IsList == true)
		            {
		                // Navigate to the list page
						UITableViewController nextController = new ListViewController(controller, controller.folder, item.ID);	
		            	TraceHelper.StartMessage("ListPage: Navigate to List");
						controller.NavigationController.PushViewController(nextController, true);
		            }
		            else
		            {
                        // if the item is a reference, traverse to the target
                        while (item.ItemRef != null)
                        {
                            try 
                            {
                                item = App.ViewModel.Items.Single(it => it.ID == item.ItemRef);
                            }
                            catch
                            {
                                TraceHelper.AddMessage(String.Format("Couldn't find item reference for name {0}, id {1}, ref {2}", 
                                                                     item.Name, item.ID, item.ItemRef));
                                return;
                            }
                        }
		                ItemPage itemPage = new ItemPage(controller.NavigationController, item);
						TraceHelper.StartMessage("ListPage: Navigate to Item");
						itemPage.PushViewController();
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
			private ListViewController controller;
			const float ImageViewWidth = 38f;
			const float ImageViewHeight = 44f;
	 
			public ListTableDataSource (ListViewController c)
			{
				controller = c;
			}
	 
			public override int RowsInSection (UITableView tableview, int section)
			{
				return controller.list.Items.Count;
			}
	 
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				Item item = controller.list.Items[indexPath.Row];
				ItemType itemType = ItemType.ItemTypes[item.ItemTypeID];
				
				// note that item types with "Complete" fields are complicated to cache - bad behavior happens when we reuse a cell
				UITableViewCell cell = itemType.HasField(FieldNames.Complete) ? null : tableView.DequeueReusableCell (itemType.Name);
				if (cell == null) 
                {
					cell = new UITableViewCell (UITableViewCellStyle.Subtitle, itemType.Name);
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}
	 			
				string icon;
				cell.TextLabel.Text = item.Name;
				cell.TextLabel.TextColor = UIColorHelper.FromString(item.NameDisplayColor);
				if (item.IsList)
				{	// render a "list" icon
					icon = "Images/179-notepad.png";
				}
				else
				{	// render an item
					cell.DetailTextLabel.Text = item.Due != null ? ((DateTime) item.Due).ToString("d") : "";
					cell.DetailTextLabel.TextColor = UIColorHelper.FromString(item.DueDisplayColor);
					icon = itemType.Icon;

                    if (itemType != null && itemType.HasField(FieldNames.Complete))
                    {
						// a "Complete" field means this item is a task that can be completed - so render a checkbox instead of a 
						// static icon
						bool complete = item.Complete == null ? false : (bool) item.Complete;
						icon = complete ? "Images/checkbox.on.png" : "Images/checkbox.off.png";
						cell.ImageView.Image = new UIImage(icon);
						cell.ImageView.UserInteractionEnabled = false;
						UICheckbox checkBox = new UICheckbox() 
						{ 
							Value = complete, 
							UserState = item 
						};
						if (cell.ImageView.Frame.IsEmpty)
						{
							float width = cell.ImageView.Image.Size.Width;
							float height = cell.ImageView.Image.Size.Height;
							float x = (ImageViewWidth - width) / 2;
							float y = (ImageViewHeight- height) / 2;
							checkBox.Frame = new RectangleF(x, y, width, height);
						}
						else
							checkBox.Frame = cell.ImageView.Frame;
						checkBox.Clicked += (sender, e) => 
						{
							// invoke the Complete click handler to update the Item
							controller.CompleteCheckbox_Click(sender, e);
							
							// Complete state change resets the text colors
							cell.TextLabel.TextColor = UIColorHelper.FromString(item.NameDisplayColor);
							cell.DetailTextLabel.TextColor = UIColorHelper.FromString(item.DueDisplayColor);							
						};
						checkBox.UserInteractionEnabled = true;		
						cell.Add(checkBox);
					}				
				}
                
                // if the icon was specified, render it
                if (icon != null)
                {   
    				// on iOS, the image path is relative, so transform "/Images/foo.png" to "Images/foo.png"
    				if (icon.StartsWith ("/Images/"))
    				    icon = icon.Substring(1);
                    if (icon.StartsWith("Images/") == false && icon.StartsWith("http://") == false && icon.StartsWith("www.") == false)
                        icon = "Images/" + icon;
    				if (cell.ImageView.Image == null)
    					cell.ImageView.Image = new UIImage(icon);
                }
                
				return cell;
			}
		}
		
		#endregion
	}
}
