using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using MonoTouch.Dialog.Utilities;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;
using MonoTouch.Dialog;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class ListViewController : UIViewController
	{
		public Folder Folder { get; set; }
		public Item List { get; set; }
        public List<Item> Sections { get; set; }
        
		private Guid? listID;
		private UIViewController parentController;
        
        private UITableView TableView;
        private UIToolbar Toolbar;
        
        private static UIImage sortButtonImage;
        private string OrderBy;

		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}
		 		
		public ListViewController(UIViewController parent, Folder f, Guid? currentID) : base()
		{
			Folder = f;
            listID = (currentID == Guid.Empty) ? (Guid?) null : (Guid?) currentID;
			parentController = parent;
            Sections = new List<Item>();
            InitializeComponent();
        }
            
		public override void ViewDidAppear(bool animated)
		{
			// load the folder and construct the list of Items that will be rendered
			try
            {
                Folder = App.ViewModel.LoadFolder(Folder.ID);

                // if the load failed, this folder has been deleted
                if (Folder == null)
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

                // initialize all the controls 
                string listName = null;
                if (listID == null || listID == Guid.Empty)
                    listName = Folder.Name;
                else
                    listName = Folder.Items.Single(i => i.ID == listID).Name;
                this.Title = listName;
   
                // construct a synthetic item that represents the list of items for which the 
                // ParentID is the parent.  this also works for the root list in a folder, which
                // is represented with a ParentID of Guid.Empty.
                Guid? itemTypeID = null;
                if (listID != null)
                {
                    Item list = App.ViewModel.Items.Single(i => i.ID == (Guid) listID);
                    itemTypeID = list.ItemTypeID;
                }
                List = new Item()
                {
                    ID = (listID == null) ? Guid.Empty : (Guid) listID,
                    Name = this.Title,
                    FolderID = Folder.ID,
                    ItemTypeID = (itemTypeID == null) ? Folder.ItemTypeID : (Guid) itemTypeID,
                    IsList = true,
                };
                
                // get the sort order from client settings and sort the list
                OrderBy = ClientSettingsHelper.GetListSortOrder(
                    App.ViewModel.ClientSettings, 
                    List.ID == Guid.Empty ? (ClientEntity) Folder : (ClientEntity) List);
                SortList();
                
                this.NavigationItem.RightBarButtonItem = new UIBarButtonItem("Properties", UIBarButtonItemStyle.Bordered, delegate {
                    if (listID == null || listID == Guid.Empty)
                    {
                        FolderEditor folderEditor = new FolderEditor(this.NavigationController, Folder);
                        folderEditor.PushViewController();
                    }
                    else
                    {
                        ListEditor listEditor = new ListEditor(this.NavigationController, Folder, List, null);
                        listEditor.PushViewController();
                    }
                });               
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
            TableView.DataSource = new ListTableDataSource(this);
            TableView.Delegate = new ListTableDelegate(this);
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
		
        public void SortList()
        {
            // refresh the items in the current List from the folder
            List.Items = Folder.Items.Where(i => i.ParentID == listID).ToObservableCollection();

            // create a new collection without any system itemtypes (which are used for section headings)
            var sorted = new ObservableCollection<Item>();
            foreach (var i in List.Items)
                if (i.ItemTypeID != SystemItemTypes.System)
                    sorted.Add(i);
            
            // order the folder by the correct fields
            switch (OrderBy)
            {
                case FieldNames.DueDate:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Priority:
                    sorted = sorted.OrderBy(t => t.Complete).ThenByDescending(t => t.PrioritySort).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Name:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Address:
                    sorted = sorted.OrderBy(t => t.Address).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Phone:
                    sorted = sorted.OrderBy(t => t.Phone).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Email:
                    sorted = sorted.OrderBy(t => t.Email).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Complete:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Category:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.Category).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case null:
                    sorted = sorted.OrderBy(t => t.SortOrder).ToObservableCollection();
                    break;
                default:
                    sorted = sorted.OrderBy(t => t.SortOrder).ToObservableCollection();
                    break;
            }

            // if we aren't categorizing then there is no need to create section headings
            Sections.Clear();            
            if (!Categorize())
            {
                var list = new Item(List, false) { Items = sorted };
                Sections.Add(list);
                return;
            }

            // insert separators for section headings
            string separator = null;
            Item currentList = null;
            foreach (var item in sorted)
            {
                ItemType itemType = App.ViewModel.ItemTypes.Single(it => it.ID == item.ItemTypeID);
                string displayType = DisplayTypes.Text;
                string value = null;
                if (itemType.Fields.Any(f => f.Name == OrderBy))
                {
                    Field field = itemType.Fields.Single(f => f.Name == OrderBy);
                    FieldValue fv = item.GetFieldValue(field, false);
                    displayType = field.DisplayType;
                    value = fv != null ? fv.Value : null;
                }
                string currentSectionHeading = item.Complete == true ? "completed" : FormatSectionHeading(displayType, value);
                if (currentSectionHeading != separator)
                {
                    currentList = new Item(List, false) { Name = currentSectionHeading };
                    Sections.Add(currentList);
                    separator = currentSectionHeading;
                }
                currentList.Items.Add(item);
            }

            //List.Items = finalList;
        }
  
        #region Helpers
        
        private bool Categorize()
        {
            switch (OrderBy)
            {
                case FieldNames.DueDate:
                case FieldNames.Priority:
                case FieldNames.Category:
                    return true;
                case FieldNames.Name:
                case FieldNames.Address:
                case FieldNames.Phone:
                case FieldNames.Email:
                case FieldNames.Complete:
                case null:
                default:
                    return false;
            }
        }
  
        private UIBarButtonItem CreateSortButton()
        {
            // if haven't loaded the sort image yet, do so now
            if (sortButtonImage == null)
                sortButtonImage = new UIImage("Images/appbar.sort.rest.png");
            
            // clicking the sort button and its event handler, which creates a new DialogViewController to host the sort picker
            var sortButton = new UIBarButtonItem(sortButtonImage, UIBarButtonItemStyle.Plain, delegate {
                // create the remove button
                var removeButton = new Button() 
                { 
                    Caption = "Remove Sort", 
                    Background = "Images/redbutton.png", 
                };
                var removeButtonList = new ButtonListElement() { removeButton };
                removeButtonList.Margin = 0f;
                
                // find the current sort field if any
                var itemType = App.ViewModel.ItemTypes.Single(it => it.ID == List.ItemTypeID);
                var fields = itemType.Fields.Where(f => f.IsPrimary == true).ToList();
                var selectedSortIndex = 0;
                if (OrderBy != null && fields.Any(f => f.DisplayName == OrderBy))
                {
                    var selectedSortField = fields.Single(f => f.DisplayName == OrderBy);
                    selectedSortIndex = Math.Max(fields.IndexOf(selectedSortField), 0);
                }

                // create the sort picker                
                var sortPickerSection = new Section();
                sortPickerSection.AddAll(from f in fields select (Element) new RadioElement(f.DisplayName));
                var sortPicker = new RootElement("Sort by", new RadioGroup(null, selectedSortIndex)) { sortPickerSection };
                
                // create the "Choose Sort" form
                var root = new RootElement("Choose Sort")
                {
                    new Section() { sortPicker },
                    new Section() { removeButtonList },
                };
                
                // create the DVC and add a "Done" button and handler
                var dvc = new DialogViewController(root);
                dvc.NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Done, delegate
                {
                    // store the current listbox and orderby field, and re-render the list
                    var field = fields[sortPicker.RadioSelected];
                    OrderBy = field.DisplayName;
        
                    // store the sort order
                    ClientSettingsHelper.StoreListSortOrder(
                        App.ViewModel.ClientSettings,
                        List.ID == Guid.Empty ? (ClientEntity) Folder : (ClientEntity) List,
                        OrderBy);
        
                    // sync with the service
                    App.ViewModel.SyncWithService();

                    // return to parent
                    dvc.NavigationController.PopViewControllerAnimated(true);
                });
                
                // add the click handler for the remove button
                removeButton.Clicked += delegate 
                {
                    // clear the sort
                    OrderBy = null;
        
                    // store the sort order
                    ClientSettingsHelper.StoreListSortOrder(
                        App.ViewModel.ClientSettings,
                        List.ID == Guid.Empty ? (ClientEntity) Folder : (ClientEntity) List,
                        null);
        
                    // sync with the service
                    App.ViewModel.SyncWithService();                            

                    // return to parent
                    dvc.NavigationController.PopViewControllerAnimated(true);
                };
    
                // display the form
                this.NavigationController.PushViewController(dvc, true);
            });         
            
            return sortButton;
        }
        
        private string FormatSectionHeading(string displayType, string value)
        {
            switch (displayType)
            {
                case DisplayTypes.Priority:
                    int pri = value == null ? 1 : Convert.ToInt32(value);
                    return App.ViewModel.Constants.Priorities[pri].Name;
                case DisplayTypes.DatePicker:
                case DisplayTypes.DateTimePicker:
                    if (value == null)
                        return "none";
                    DateTime dt = Convert.ToDateTime(value);
                    return dt.ToShortDateString();
                case DisplayTypes.Text:
                case DisplayTypes.TextArea:
                case DisplayTypes.Phone:
                case DisplayTypes.Link:
                case DisplayTypes.Email:
                case DisplayTypes.Address:
                default:
                    return value ?? "none";
            }
        }
        
        void InitializeComponent()
        {            
            // get the current list name
            string listName = null;
            if (listID == null || listID == Guid.Empty)
                listName = Folder.Name;
            else
                listName = Folder.Items.Single(i => i.ID == listID).Name;
            this.Title = listName;

            // calculate the frame sizes
            float navBarHeight = 
                parentController.NavigationController != null ? 
                parentController.NavigationController.NavigationBar.Bounds.Height : 
                new UINavigationController().NavigationBar.Bounds.Height;
            float tabBarHeight = 
                parentController.TabBarController.TabBar != null ? 
                parentController.TabBarController.TabBar.Bounds.Height : 
                new UINavigationController().NavigationBar.Bounds.Height;
            float availableHeight = View.Bounds.Height - navBarHeight - tabBarHeight;
            float toolbarHeight = navBarHeight;
            float tableHeight = availableHeight - toolbarHeight;

            // create the tableview
            TableView = new UITableView() { Frame = new RectangleF(0, 0, View.Bounds.Width, tableHeight) };
            this.View.AddSubview(TableView);
            
            // create the toolbar - edit button, add button, sort button
            Toolbar = new UIToolbar() { Frame = new RectangleF(0, tableHeight, View.Bounds.Width, toolbarHeight) };                            
            var flexSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);            

            //var addButton = new UIBarButtonItem("\u2795" /* big plus */ + "List", UIBarButtonItemStyle.Plain, delegate { 
            var addButton = new UIBarButtonItem(UIBarButtonSystemItem.Add, delegate { 
                ListEditor listEditor = new ListEditor(this.NavigationController, Folder, null, List.ID);
                listEditor.PushViewController();
            });

            // create the sort button along with the action, which will instantiate a new DialogViewController
            var sortButton = CreateSortButton();
                      
            // create the edit and done buttons
            // the edit button puts the table in edit mode, and the done button returns to normal mode
            var editButton = new UIBarButtonItem(UIBarButtonSystemItem.Edit);
            var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done);           
            editButton.Clicked += delegate
            { 
                if (TableView.Editing == false)
                {
                    TableView.SetEditing(true, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, sortButton, flexSpace, doneButton, flexSpace }, false);
                }
            };
            doneButton.Clicked += delegate
            {
                if (TableView.Editing == true)
                {
                    TableView.SetEditing(false, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, sortButton, flexSpace, editButton, flexSpace }, false);

                    // trigger a sync with the Service 
                    App.ViewModel.SyncWithService();   
                    
                    Folder = App.ViewModel.LoadFolder(Folder.ID);
                }
            };

            Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, sortButton, flexSpace, editButton, flexSpace }, false);
            this.View.AddSubview(Toolbar);
        }
        
        #endregion Helpers
             
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
            
            // save the changes to local storage
            StorageHelper.WriteFolder(Folder);

            // reorder the item in the folder and the ListBox
            SortList();
            TableView.ReloadData();

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
				Item item = controller.Sections[indexPath.Section].Items[indexPath.Row];
				if (item != null)
				{
					if (item.IsList == true)
		            {
		                // Navigate to the list page
						UIViewController nextController = new ListViewController(controller, controller.Folder, item.ID);	
		            	TraceHelper.StartMessage("ListPage: Navigate to List");
						controller.NavigationController.PushViewController(nextController, true);
		            }
		            else
		            {
                        // if the item is a reference, traverse to the target
                        while (item.ItemTypeID == SystemItemTypes.Reference && item.ItemRef != null)
                        {
                            try 
                            {
                                item = App.ViewModel.Items.Single(it => it.ID == item.ItemRef);
                            }
                            catch
                            {
                                TraceHelper.AddMessage(String.Format("Couldn't find item reference for name {0}, id {1}, ref {2}", 
                                                                     item.Name, item.ID, item.ItemRef));
                                break;
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
	 
            public override int NumberOfSections(UITableView tableView)
            {
                 return controller.Sections.Count;
            }
            
            public override int RowsInSection(UITableView tableview, int section)
            {
                return controller.Sections[section].Items.Count;
            }
            
			public override string TitleForHeader (UITableView tableView, int section)
            {
                return controller.Sections[section].Name;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
			{
				Item item = controller.Sections[indexPath.Section].Items[indexPath.Row];
                
                // if the item is a reference, traverse to the target
                while (item.ItemTypeID == SystemItemTypes.Reference && item.ItemRef != null)
                {
                    try 
                    {
                        item = App.ViewModel.Items.Single(it => it.ID == item.ItemRef);
                    }
                    catch
                    {
                        TraceHelper.AddMessage(String.Format("Couldn't find item reference for name {0}, id {1}, ref {2}", 
                                                             item.Name, item.ID, item.ItemRef));
                        break;
                    }
                }
                
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
                    if (icon.StartsWith("Images/") == false && icon.StartsWith("http") == false && icon.StartsWith("www.") == false)
                        icon = "Images/" + icon;
    				if (cell.ImageView.Image == null)
    					cell.ImageView.Image = new UIImage(icon);
                }
                
                // if there is a picture, render it (potentially on top of the icon just rendered)
                // render a picture if one exists 
                // this picture will layer on top of the existing icon - in case the picture is unavailable (e.g. disconnected)
                var picFV = item.GetFieldValue(FieldNames.Picture, false);
                if (picFV != null && !String.IsNullOrEmpty(picFV.Value))
                {
                    var callback = new ImageLoaderCallback(controller, cell.ImageView, indexPath);
                    //var image = ImageLoader.DefaultRequestImage(new Uri(picFV.Value), callback);
                    cell.ImageView.Image = ImageLoader.DefaultRequestImage(new Uri(picFV.Value), callback);
                    //callback.Image = image;
                }
                
				return cell;
			}	
            
            public override bool CanMoveRow(UITableView tableView, NSIndexPath indexPath)
            {
                return controller.OrderBy == null;
            }
            
            public override void MoveRow(UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
            {
                if (sourceIndexPath.Section != 0 || 
                    destinationIndexPath.Section != 0)
                    return;
                
                int sourceRow = sourceIndexPath.Row;
                int destRow = destinationIndexPath.Row;
                float before, after;
                Item item = controller.Sections[0].Items[sourceRow];
                
                // compute the new sort order for the folder based on the directiom of motion (up or down)
                if (sourceRow < destRow) 
                {
                    // moving down - new position is the average of target position plus next position
                    before = controller.Sections[0].Items[destRow].SortOrder;
                    if (destRow >= controller.Sections[0].Items.Count - 1)
                        after = before + 1000f;
                    else
                        after = controller.Sections[0].Items[destRow + 1].SortOrder;
                }                
                else
                {
                    // moving up - new position is the average of target position plus previous position
                    after = controller.Sections[0].Items[destRow].SortOrder;
                    if (destRow == 0)
                        before = 0;
                    else
                        before = controller.Sections[0].Items[destRow - 1].SortOrder;
                }
                float newSortOrder = (before + after) / 2;
                
                // make a copy of the item for the Update operation
                Item itemCopy = new Item(item);
                item.SortOrder = newSortOrder;

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { itemCopy, item },
                        BodyTypeName = typeof(Item).Name,
                        ID = item.ID
                    });
    
                // save the changes to local storage
                StorageHelper.WriteFolder(controller.Folder);

                // re-sort the current list, and have the table view update its UI
                controller.SortList(); 
                tableView.MoveRow(sourceIndexPath,destinationIndexPath);
            }
            
            public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
            {
                if (editingStyle == UITableViewCellEditingStyle.Delete)
                {
                    Item item = controller.Sections[indexPath.Section].Items[indexPath.Row];

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = item
                        });
        
                    // save the changes to local storage and refresh the folder
                    App.ViewModel.RemoveItem(item);
                    controller.Folder = App.ViewModel.LoadFolder(controller.Folder.ID);
     
                    // re-sort the current list, and have the table view update its UI
                    controller.SortList(); 
                    //tableView.DeleteRows(new [] { indexPath }, UITableViewRowAnimation.Fade);
                    tableView.ReloadData();
                }
            }
        }
  
        // callback class for the MonoTouch.Dialog image loader utility
        private class ImageLoaderCallback : IImageUpdated
        {
            private ListViewController controller;
            private UIImageView imageView;
            private NSIndexPath indexPath;
            
            public ImageLoaderCallback(ListViewController c, UIImageView view, NSIndexPath path)
            {
                controller = c;
                imageView = view;
                indexPath = path;
            }
            
            public UIImage Image { get; set; }
                
            void IImageUpdated.UpdatedImage(Uri uri)
            {
                if (uri == null)
                    return;
                if (Image != null)
                    imageView.Image = Image;
                // refresh the display for the row of the image that just got updated
                controller.TableView.ReloadRows(new NSIndexPath [] { indexPath }, UITableViewRowAnimation.None);                
            }
        }
        
		#endregion
	}
}
