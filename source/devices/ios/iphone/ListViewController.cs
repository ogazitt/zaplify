using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using MonoTouch.AddressBookUI;
using MonoTouch.Dialog;
using MonoTouch.Dialog.Utilities;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class ListViewController : UIViewController
	{
		private UIViewController parentController;
        private Guid? listID;
        private Folder folder;
        
        private UITableView TableView;
        private UIToolbar Toolbar;
        private ListTableDataSource Source;
        
        private static UIImage editButtonImage;
        private static UIImage emailButtonImage;
        private static UIImage sortButtonImage;

		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}
		 		
		public ListViewController(UIViewController parent, Folder f, Guid? currentID) : base()
		{
            folder = f;
            listID = (currentID == Guid.Empty) ? (Guid?) null : (Guid?) currentID;
			parentController = parent;
        }

        public override void ViewDidLoad()
        {
            TraceHelper.AddMessage("ListView: ViewDidLoad");
            Source = new ListTableDataSource(this);
            InitializeComponent();
            TableView.DataSource = Source;
            TableView.Delegate = new ListTableDelegate(this);
            base.ViewDidLoad();
        }

        public override void ViewDidUnload()
        {
            TraceHelper.AddMessage("ListView: ViewDidUnload");

            // Release any retained subviews of the main view.
            Cleanup();
            base.ViewDidUnload ();
        }
        
		public override void ViewDidAppear(bool animated)
		{
            // trace event
            TraceHelper.AddMessage("ListView: ViewDidAppear");

            // set the background
            TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            Toolbar.TintColor = UIColorHelper.FromString(App.ViewModel.Theme.ToolbarBackground);
            NavigationController.NavigationBar.TintColor = UIColorHelper.FromString(App.ViewModel.Theme.ToolbarBackground);

            // load the folder and construct the list of Items that will be rendered
			try
            {
                Source.Folder = App.ViewModel.LoadFolder(Source.Folder.ID);

                // if the load failed, this folder has been deleted
                if (Source.Folder == null)
                {
                    // the folder isn't found - this can happen when the folder we were just 
                    // editing was removed in FolderEditor, which then goes back to ListPage.
                    // this will send us back to the MainPage which is appropriate.

                    // trace page navigation
                    TraceHelper.StartMessage("ListPage: Navigate back");

                    // navigate back
					this.NavigationController.PopViewControllerAnimated(true);
                    return;
                }

                // initialize all the controls 
                string listName = null;
                if (listID == null || listID == Guid.Empty)
                    listName = Source.Folder.Name;
                else
                    listName = Source.Folder.Items.Single(i => i.ID == listID).Name;
                this.Title = listName;
   
                // construct a synthetic item that represents the list of items for which the 
                // ParentID is the parent.  this also works for the root list in a folder, which
                // is represented with a ParentID of Guid.Empty.
                Guid? itemTypeID = null;
                Guid? parentID = null;
                if (listID != null)
                {
                    Item list = App.ViewModel.Items.Single(i => i.ID == (Guid) listID);
                    itemTypeID = list.ItemTypeID;
                    parentID = list.ParentID;
                }

                // create the data source's item list
                Source.List = new Item()
                {
                    ID = (listID == null) ? Guid.Empty : (Guid) listID,
                    Name = this.Title,
                    FolderID = Source.Folder.ID,
                    ParentID = (parentID == null) ? null : parentID,
                    ItemTypeID = (itemTypeID == null) ? Source.Folder.ItemTypeID : (Guid) itemTypeID,
                    IsList = true,
                };
                
                // get the sort order from client settings and sort the list
                Source.OrderBy = ListMetadataHelper.GetListSortOrder(
                    App.ViewModel.PhoneClientFolder, 
                    Source.List.ID == Guid.Empty ? (ClientEntity) Source.Folder : (ClientEntity) Source.List);
                Source.SortList();
            }
            catch (Exception ex)
            {
                // the folder isn't found - this can happen when the folder we were just 
                // editing was removed in FolderEditor, which then goes back to ListPage.
                // this will send us back to the MainPage which is appropriate.

                // trace page navigation
                TraceHelper.StartMessage(String.Format("ListPage: Navigate back (exception: {0})", ex.Message));

                // navigate back
                //this.NavigationController.PopToViewController(parentController, true);
                this.NavigationController.PopViewControllerAnimated(true);
                return;
            }
			
			// reload the data
			TableView.ReloadData();

            // HACK: touch the ViewControllers array to refresh it (in case the user popped the nav stack)
            // this is to work around a bug in monotouch (https://bugzilla.xamarin.com/show_bug.cgi?id=1889)
            // where the UINavigationController leaks UIViewControllers when the user pops the nav stack
            if (this.NavigationController.ViewControllers.Length > 0) {}

            base.ViewDidAppear(animated);
        }
		
        public override void ViewDidDisappear(bool animated)
        {
            TraceHelper.AddMessage("ListView: ViewDidDisappear");

            // search for the current controller in the nav stack
            bool found = false;
            if (this.NavigationController != null)
            {
                foreach (var nav in this.NavigationController.ViewControllers)
                {
                    if (nav == this)
                    {
                        found = true;
                        break;
                    }
                }
            }

            // if didn't find current controller in nav stack, we popped
            if (found == false)
            {
                // clean up all resources associated with this controller
                Cleanup();
                //this.Dispose();
            }

            base.ViewDidDisappear(animated);
        }

		public override void DidReceiveMemoryWarning ()
		{
            // trace event
            TraceHelper.AddMessage("ListView: DidReceiveMemoryWarning");

            // Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
            // TODO:
            //editButtonImage;
            //emailButtonImage;
            //sortButtonImage;
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
  
        #region Helpers

        private void Cleanup()
        {
            folder = null;
            listID = null;
            parentController = null;

            if (TableView != null)
            {
                if (TableView.Delegate != null)
                    TableView.Delegate.Dispose();
                TableView.Delegate = null;
                if (TableView.DataSource != null)
                    TableView.DataSource.Dispose();
                TableView.DataSource = null;
                TableView.Dispose();
            }
            TableView = null;
            if (Toolbar != null)
                Toolbar.Dispose();
            Toolbar = null;
            Source = null;
        }
  
        private UIBarButtonItem CreateEmailButton()
        {
            // if haven't loaded the email image yet, do so now
            if (emailButtonImage == null)
                emailButtonImage = UIImageCache.GetUIImage("Images/18-envelope.png");
            
            // clicking the sort button and its event handler, which creates a new DialogViewController to host the sort picker
            var emailButton = new UIBarButtonItem(emailButtonImage, UIBarButtonItemStyle.Plain, delegate {
                var mailHelper = new MailHelper(this)
                {
                    Subject = "Zaplify List: " + Source.List.Name,
                    Body = GetListAsText()
                };
                mailHelper.SendMail();
            });

            return emailButton;
        }

        private UIBarButtonItem CreateSortButton()
        {
            // if haven't loaded the sort image yet, do so now
            if (sortButtonImage == null)
                sortButtonImage = UIImageCache.GetUIImage("Images/appbar.sort.rest.png");
            
            // clicking the sort button and its event handler, which creates a new DialogViewController to host the sort picker
            var sortButton = new UIBarButtonItem(sortButtonImage, UIBarButtonItemStyle.Plain, delegate {
                // create the remove button
                var removeButton = new Button() 
                { 
                    Caption = "Remove Sort", 
                    Background = "Images/darkgreybutton.png", 
                };
                var removeButtonList = new ButtonListElement() { removeButton };
                removeButtonList.Margin = 0f;
                
                // find the current sort field if any
                var itemType = App.ViewModel.ItemTypes.Single(it => it.ID == Source.List.ItemTypeID);
                var fields = itemType.Fields.Where(f => f.IsPrimary == true).ToList();
                var selectedSortIndex = 0;
                if (Source.OrderBy != null && fields.Any(f => f.DisplayName == Source.OrderBy))
                {
                    var selectedSortField = fields.Single(f => f.DisplayName == Source.OrderBy);
                    selectedSortIndex = Math.Max(fields.IndexOf(selectedSortField), 0);
                }

                // create the sort picker                
                var sortPickerSection = new Section();
                sortPickerSection.AddAll(from f in fields select (Element) new RadioElement(f.DisplayName));
                var sortPicker = new ThemedRootElement("Sort by", new RadioGroup(null, selectedSortIndex)) { sortPickerSection };
                
                // create the "Choose Sort" form
                var root = new ThemedRootElement("Choose Sort")
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
                    Source.OrderBy = field.Name;
        
                    // store the sort order
                    ListMetadataHelper.StoreListSortOrder(
                        App.ViewModel.PhoneClientFolder,
                        Source.List.ID == Guid.Empty ? (ClientEntity) Source.Folder : (ClientEntity) Source.List,
                        Source.OrderBy);
        
                    // sync with the service
                    // (do not sync for operations against $ClientSettings)
                    //App.ViewModel.SyncWithService();

                    // return to parent
                    dvc.NavigationController.PopViewControllerAnimated(true);
                });
                dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
                
                // add the click handler for the remove button
                removeButton.Clicked += delegate 
                {
                    // clear the sort
                    Source.OrderBy = null;
        
                    // store the sort order
                    ListMetadataHelper.StoreListSortOrder(
                        App.ViewModel.PhoneClientFolder,
                        Source.List.ID == Guid.Empty ? (ClientEntity) Source.Folder : (ClientEntity) Source.List,
                        null);
        
                    // sync with the service
                    // (do not sync for operations against $ClientSettings)
                    //App.ViewModel.SyncWithService();                            

                    // return to parent
                    dvc.NavigationController.PopViewControllerAnimated(true);
                };
    
                // display the form
                this.NavigationController.PushViewController(dvc, true);
            });         
            
            return sortButton;
        }

        public string GetListAsText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Source.List.Name);
            sb.AppendLine();

            foreach (var section in Source.Sections)
            {
                // render a separator (section heading)
                sb.AppendLine(section.Name);

                foreach (var item in section.Items)
                {
                    // skip lists
                    if (item.IsList)
                        continue;
    
                    // indent item
                    sb.Append("    ");
    
                    // render a unicode checkbox (checked or unchecked) if the item has a complete field
                    if (App.ViewModel.ItemTypes.Any(it => it.ID == item.ItemTypeID && it.Fields.Any(f => f.Name == FieldNames.Complete)))
                    {
                        var complete = item.GetFieldValue(FieldNames.Complete);
                        if (complete != null && Convert.ToBoolean(complete.Value) == true)
                            sb.Append("\u2612 ");
                        else
                            sb.Append("\u2610 ");
                    }
    
                    // render the item name
                    sb.Append(item.Name);
    
                    var duedate = item.GetFieldValue(FieldNames.DueDate);
                    if (duedate != null && duedate.Value != null)
                        sb.Append(", due on " + Convert.ToDateTime(duedate.Value).ToString("d"));
    
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        void InitializeComponent()
        {            
            // get the current list name
            string listName = null;
            if (listID == null || listID == Guid.Empty)
                listName = Source.Folder.Name;
            else
                listName = Source.Folder.Items.Single(i => i.ID == listID).Name;
            this.Title = listName;

            // calculate the frame sizes
            float navBarHeight = 
                parentController.NavigationController != null ? 
                parentController.NavigationController.NavigationBar.Bounds.Height : 
                new UINavigationController().NavigationBar.Bounds.Height;
            float tabBarHeight = 0;

            float availableHeight = View.Bounds.Height - navBarHeight - tabBarHeight;
            float toolbarHeight = navBarHeight;
            float tableHeight = availableHeight - toolbarHeight;

            // create the tableview
            TableView = new UITableView() { Frame = new RectangleF(0, 0, View.Bounds.Width, tableHeight) };
            TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.TableBackground);
            TableView.SeparatorColor = UIColorHelper.FromString(App.ViewModel.Theme.TableSeparatorBackground);
            this.View.AddSubview(TableView);
            
            // create the toolbar - edit button, add button, sort button
            Toolbar = new UIToolbar() { Frame = new RectangleF(0, tableHeight, View.Bounds.Width, toolbarHeight) };                            
            var flexSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);            

            // create the add button
            var addButton = new UIBarButtonItem(UIBarButtonSystemItem.Add);
            addButton.Clicked += (sender, e) => { AddButton_Click(sender, e); };

            // create the email button
            var emailButton = CreateEmailButton();

            // create the sort button along with the action, which will instantiate a new DialogViewController
            var sortButton = CreateSortButton();
                      
            // create the edit and done buttons
            // the edit button puts the table in edit mode, and the done button returns to normal mode
            if (editButtonImage == null)
                editButtonImage = UIImageCache.GetUIImage("Images/187-pencil.png");
            var editButton = new UIBarButtonItem(editButtonImage, UIBarButtonItemStyle.Plain, null);
            var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done);           
            editButton.Clicked += delegate
            { 
                if (TableView.Editing == false)
                {
                    TableView.SetEditing(true, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, sortButton, flexSpace, emailButton, flexSpace, doneButton, flexSpace }, false);
                }
            };
            doneButton.Clicked += delegate
            {
                if (TableView.Editing == true)
                {
                    TableView.SetEditing(false, true);
                    Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, sortButton, flexSpace, emailButton, flexSpace, editButton, flexSpace  }, false);

                    // trigger a sync with the Service 
                    App.ViewModel.SyncWithService();   
                    
                    Source.Folder = App.ViewModel.LoadFolder(Source.Folder.ID);
                }
            };

            // create the toolbar with all its buttons
            Toolbar.SetItems(new UIBarButtonItem[] { flexSpace, addButton, flexSpace, sortButton, flexSpace, emailButton, flexSpace, editButton, flexSpace }, false);
            this.View.AddSubview(Toolbar);

            // create the properties right bar button item
            this.NavigationItem.RightBarButtonItem = new UIBarButtonItem("Properties", UIBarButtonItemStyle.Bordered, delegate {
                if (listID == null || listID == Guid.Empty)
                {
                    FolderEditor folderEditor = new FolderEditor(this.NavigationController, Source.Folder);
                    folderEditor.PushViewController();
                }
                else
                {
                    ListEditor listEditor = new ListEditor(this.NavigationController, Source.Folder, Source.List, null);
                    listEditor.PushViewController();
                }
            });               
        }
        
        #endregion Helpers
             
		#region Event Handlers
		
        private void AddButton_Click(object sender, EventArgs ea)
        {
            if (Source.List.ItemTypeID == SystemItemTypes.Contact)
            {
                var picker = new ABPeoplePickerNavigationController();
                picker.SelectPerson += delegate(object s, ABPeoplePickerSelectPersonEventArgs e) {
                    // process the contact - add the new contact or update the existing contact's info from the address book
                    var contact = ContactPickerHelper.ProcessContact(e.Person);
                    if (contact != null)
                        App.ViewModel.SyncWithService();
                    picker.DismissModalViewControllerAnimated(true);
                };

                picker.Cancelled += delegate {
                    picker.DismissModalViewControllerAnimated(true);
                };

                // present the contact picker
                NavigationController.PresentModalViewController(picker, true);
                return;
            }

            var button = sender as UIBarButtonItem;
            if (button == null)
                return;

            SpeechDialog speechDialog = new SpeechDialog(this);
            speechDialog.Open(button);
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
			 
            protected override void Dispose(bool disposing)
            {
                controller = null;
                base.Dispose(disposing);
            }
     
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				// turn off the highlight for the cell that's selected
				tableView.CellAt (indexPath).SetHighlighted(false, false);
				
				// get the item at the row and depending on whether it's a list 
				// or a singleton, navigate to the right page
				Item item = controller.Source.Sections[indexPath.Section].Items[indexPath.Row];
				if (item != null)
				{
					if (item.IsList == true)
		            {
		                // Navigate to the list page
						UIViewController nextController = new ListViewController(controller, controller.Source.Folder, item.ID);	
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
            private Guid? listID;
			const float ImageViewWidth = 38f;
			const float ImageViewHeight = 44f;
	 
            public Item List { get; set; }
            public Folder Folder { get; set; }
            public List<Item> Sections { get; set; }
            public string OrderBy { get; set; }

            public ListTableDataSource (ListViewController c)
			{
				controller = c;
                Folder = c.folder;
                listID = c.listID;
                Sections = new List<Item>();
			}

            protected override void Dispose(bool disposing)
            {
                controller = null;
                base.Dispose(disposing);
            }
	 
            public override int NumberOfSections(UITableView tableView)
            {
                 return Sections.Count;
            }
            
            public override int RowsInSection(UITableView tableview, int section)
            {
                return Sections[section].Items.Count;
            }
            
			public override string TitleForHeader (UITableView tableView, int section)
            {
                return Sections[section].Name;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
			{
                Item item = Sections[indexPath.Section].Items[indexPath.Row];

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
				UITableViewCell cell = itemType.HasField(FieldNames.Complete) ? null : tableView.DequeueReusableCell(itemType.Name);
                cell = null;  // update: with pictures, other cells are too hard to cache as well.  give up on caching cells.
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
                    cell.DetailTextLabel.Text = item.DueDisplay; //item.Due != null ? ((DateTime) item.Due).ToString("d") : "";
					cell.DetailTextLabel.TextColor = UIColorHelper.FromString(item.DueDisplayColor);
					icon = itemType.Icon;

                    if (itemType != null && itemType.HasField(FieldNames.Complete))
                    {
						// a "Complete" field means this item is a task that can be completed - so render a checkbox instead of a 
						// static icon
						bool complete = item.Complete == null ? false : (bool) item.Complete;
						
                        // load the image for calculation purposes
                        icon = complete ? "Images/checkbox.on.png" : "Images/checkbox.off.png";
                        cell.ImageView.Image = UIImageCache.GetUIImage(icon);
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

                        checkBox.Clicked += CompleteCheckbox_Click;
                            /*
                            (sender, e) => 
                        {
                            // trace data
                            TraceHelper.StartMessage("CompleteCheckbox Click");

                            // get the item that was just updated, and ensure the Complete flag is in the correct state
                            UICheckbox cb = (UICheckbox) sender;
                            Item thisItem = (Item) cb.UserState;
                            //Item thisItem = item;
                
                            // create a copy of that item
                            Item itemCopy = new Item(thisItem);
                
                            // toggle the complete flag to reflect the checkbox click
                            thisItem.Complete = !item.Complete;
                
                            // bump the last modified timestamp
                            thisItem.LastModified = DateTime.UtcNow;
                
                            if (thisItem.Complete == true)
                                thisItem.CompletedOn = thisItem.LastModified.ToString("d");
                            else
                                thisItem.CompletedOn = null;
                
                            // enqueue the Web Request Record
                            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                                new RequestQueue.RequestRecord()
                                {
                                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                                    Body = new List<Item>() { itemCopy, thisItem },
                                    BodyTypeName = "Item",
                                    ID = item.ID
                                });

                            // save the changes to local storage
                            //StorageHelper.WriteFolder(Folder);

                            // reorder the item in the folder and the ListBox
                            SortList();
                            //TableView.ReloadData();
                
                            // trigger a sync with the Service 
                            App.ViewModel.SyncWithService();
                
                            // trace data
                            TraceHelper.AddMessage("Finished CompleteCheckbox Click");
                        };
                        */
                 
						checkBox.UserInteractionEnabled = true;	
						cell.AddSubview(checkBox);
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
    					cell.ImageView.Image = UIImageCache.GetUIImage(icon);
                }
                
                // if there is a picture, render it (potentially on top of the icon just rendered)
                // render a picture if one exists 
                // this picture will layer on top of the existing icon - in case the picture is unavailable (e.g. disconnected)
                var picFV = item.GetFieldValue(FieldNames.Picture, false);
                if (item.IsList == false && 
                    !itemType.HasField(FieldNames.Complete) && 
                    picFV != null && !String.IsNullOrEmpty(picFV.Value))
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
                return OrderBy == null;
            }
            
            public override void MoveRow(UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
            {
                if (sourceIndexPath.Section != 0 || 
                    destinationIndexPath.Section != 0)
                    return;
                
                int sourceRow = sourceIndexPath.Row;
                int destRow = destinationIndexPath.Row;
                float before, after;
                //Item item = controller.Sections[0].Items[sourceRow];
                Item item = Sections[0].Items[sourceRow];

                // compute the new sort order for the folder based on the directiom of motion (up or down)
                if (sourceRow < destRow) 
                {
                    // moving down - new position is the average of target position plus next position
                    before = Sections[0].Items[destRow].SortOrder;
                    if (destRow >= Sections[0].Items.Count - 1)
                        after = before + 1000f;
                    else
                        after = Sections[0].Items[destRow + 1].SortOrder;
                }                
                else
                {
                    // moving up - new position is the average of target position plus previous position
                    after = Sections[0].Items[destRow].SortOrder;
                    if (destRow == 0)
                        before = 0;
                    else
                        before = Sections[0].Items[destRow - 1].SortOrder;
                }
                float newSortOrder = (before + after) / 2;
                
                // make a copy of the item for the Update operation
                Item itemCopy = new Item(item);
                item.SortOrder = newSortOrder;

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { itemCopy, item },
                        BodyTypeName = typeof(Item).Name,
                        ID = item.ID
                    });
    
                // save the changes to local storage
                StorageHelper.WriteFolder(Folder);

                // re-sort the current list, and have the table view update its UI
                SortList(); 
                tableView.MoveRow(sourceIndexPath,destinationIndexPath);
            }
            
            public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
            {
                if (editingStyle == UITableViewCellEditingStyle.Delete)
                {
                    //Item item = controller.Sections[indexPath.Section].Items[indexPath.Row];
                    Item item = Sections[indexPath.Section].Items[indexPath.Row];

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = item
                        });
        
                    // save the changes to local storage and refresh the folder
                    App.ViewModel.RemoveItem(item);
                    Folder = App.ViewModel.LoadFolder(Folder.ID);
     
                    // re-sort the current list, and have the table view update its UI
                    SortList(); 
                    //tableView.DeleteRows(new [] { indexPath }, UITableViewRowAnimation.Fade);
                    tableView.ReloadData();
                }
            }

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
                UICheckbox checkBox = (UICheckbox) sender;
                Item item = (Item) checkBox.UserState;
    
                // create a copy of that item
                Item itemCopy = new Item(item);
    
                // toggle the complete flag to reflect the checkbox click
                item.Complete = !item.Complete;
    
                // bump the last modified timestamp
                item.LastModified = DateTime.UtcNow;
    
                if (item.Complete == true)
                    item.CompletedOn = item.LastModified.ToString("o");
                else
                    item.CompletedOn = null;
    
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
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
                controller.TableView.ReloadData();
    
                // trigger a sync with the Service 
                App.ViewModel.SyncWithService();
    
                // trace data
                TraceHelper.AddMessage("Finished CompleteCheckbox Click");
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
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Complete.ToString()).ThenBy(t => (!t.IsList).ToString()).ThenBy(t => t.DueSort.ToString("u")).ThenBy(t => t.Name).ToObservableCollection();
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Priority:
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenByDescending(t => t.PrioritySort).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Complete.ToString()).ThenBy(t => (!t.IsList).ToString()).ThenByDescending(t => t.PrioritySort.ToString()).ThenBy(t => t.Name).ToObservableCollection();
                        //sorted = sorted.OrderBy(t => t.Complete).ThenByDescending(t => t.PrioritySort).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Name:
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Complete.ToString()).ThenBy(t => (!t.IsList).ToString()).ThenBy(t => t.Name).ToObservableCollection();
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Address:
                        //sorted = sorted.OrderBy(t => t.Address).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Address).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Phone:
                        //sorted = sorted.OrderBy(t => t.Phone).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Phone).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Email:
                        //sorted = sorted.OrderBy(t => t.Email).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Email).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Complete:
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Complete.ToString()).ThenBy(t => (!t.IsList).ToString()).ThenBy(t => t.Name).ToObservableCollection();
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case FieldNames.Category:
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Category).ThenBy(t => t.Name).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Complete.ToString()).ThenBy(t => (!t.IsList).ToString()).ThenBy(t => t.Category).ThenBy(t => t.Name).ToObservableCollection();
                        //sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => t.Category).ThenBy(t => t.Name).ToObservableCollection();
                        break;
                    case null:
                        //sorted = sorted.OrderBy(t => t.SortOrder).ThenBy(t => !t.IsList).ToObservableCollection();
                        sorted = sorted.OrderBy(t => t.Complete.ToString()).ThenBy(t => (!t.IsList).ToString()).ToObservableCollection();
                        //sorted = sorted.OrderBy(t => t.SortOrder).ToObservableCollection();
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
                    currentSectionHeading = item.IsList == true ? "lists" : currentSectionHeading;
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
        
            #endregion Helpers
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
