using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MonoTouch.Dialog;
using MonoTouch.Dialog.Utilities;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Newtonsoft.Json;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class ItemPage : IImageUpdated
	{
		Item ThisItem = null;
		Item ItemCopy = null;
		Folder folder = null;
		UINavigationController controller;
		RootElement root = null;
        DialogViewController actionsViewController;
        DialogViewController editViewController;
		
		public ItemPage(UINavigationController c, Item item)
		{
			// trace event
            TraceHelper.AddMessage("Item: constructor");
			controller = c;
			ThisItem = item;
		}
		
		public void PushViewController()
		{
			// trace event
            TraceHelper.AddMessage("Item: PushViewController");
			
            try
            {
                folder = App.ViewModel.LoadFolder(ThisItem.FolderID);
            }
            catch (Exception)
            {
                folder = null;
				return;
            }

	        // make a deep copy of the item which stores the previous values
            // the iphone implementation will make changes to the "live" copy 
	        ItemCopy = new Item(ThisItem);
			root = RenderViewItem(ThisItem);			
			actionsViewController = new DialogViewController (root, true);
   
            // create an Edit button which pushes the edit view onto the nav stack
			actionsViewController.NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Edit, delegate {
                var editRoot = RenderEditItem(ThisItem, false);
				editViewController = new DialogViewController(editRoot, true);
                editViewController.NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Done, delegate {
                    // save the item and trigger a sync with the service  
                    SaveButton_Click(null, null);
                    // navigate back to the list page
                    TraceHelper.StartMessage("Item: Navigate back");
                    NavigateBack();
                });

                UIImage actionsBackButtonImage = UIImageCache.GetUIImage("Images/actions-back-button.png");
                UIImage actionsBackButtonImageSelected = UIImageCache.GetUIImage("Images/actions-back-button-selected.png");                
                UIButton actionsBackButton = UIButton.FromType(UIButtonType.Custom);
                actionsBackButton.SetImage(actionsBackButtonImage, UIControlState.Normal);
                actionsBackButton.SetImage(actionsBackButtonImageSelected, UIControlState.Selected);
                actionsBackButton.SetImage(actionsBackButtonImageSelected, UIControlState.Highlighted);
                actionsBackButton.Frame = new System.Drawing.RectangleF(0, 0, actionsBackButtonImage.Size.Width, actionsBackButtonImage.Size.Height);
                actionsBackButton.TouchUpInside += delegate {
                    // save the item and trigger a sync with the service  
                    SaveButton_Click(null, null);
                    // reload the Actions page 
                    var oldroot = root;
                    root = RenderViewItem(ThisItem);
                    actionsViewController.Root = root;
                    actionsViewController.ReloadData();
                    oldroot.Dispose();
                    // pop back to actions page
                    controller.PopViewControllerAnimated(true);
                };
                UIBarButtonItem actionsBackBarItem = new UIBarButtonItem(actionsBackButton);
                editViewController.NavigationItem.LeftBarButtonItem = actionsBackBarItem;
                editViewController.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
                controller.PushViewController(editViewController, true);
			});
			
            // if moving from the item page to its parent (e.g. the schedule tab), call ViewDidAppear on that controller 
            actionsViewController.ViewDisappearing += (sender, e) => 
            {
                /* 
                if (UIDevice.CurrentDevice.CheckSystemVersion(5, 0)) 
                {
                    // this property should be called "IsMovingToParentViewController" - it is a bug in the property naming, 
                    // not a bug in the code
                    if (actionsViewController.IsMovingFromParentViewController)
                        controller.ViewDidAppear(false);
                }
                */
                // the IsMovingToParentViewController method is only available on iOS 5.0 so the code above doesn't work generally.
                // it does not appear to hurt anything to always call ViewDidAppear (even when pushing deeper into the nav stack)
                controller.ViewDidAppear(false);
            };

			// push the "view item" view onto the nav stack
            actionsViewController.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
			controller.PushViewController(actionsViewController, true);
		}
				
        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the list page
			NavigateBack();
        }
		
		private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new item, delete just does the same thing as cancel
            if (ThisItem == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this item?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = ThisItem
                });

            // remove the item (and all subitems) from the local folder (and local storage)
            App.ViewModel.RemoveItem(ThisItem);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the folder page
			NavigateBack();
        }
		
		public void SaveButton_Click(object sender, EventArgs e)
        {
            // update the LastModified timestamp
            ThisItem.LastModified = DateTime.UtcNow;
			
			// parse common regexps out of the description and into the appropriate
			// fields (phone, email, URL, etc)
            ParseFields(ThisItem);
            
            // remove any NEW FieldValues (i.e. ones which didn't exist on the original item) 
            // which contain null Values (we don't want to burden the item with
            // extraneous null fields)
            List<FieldValue> fieldValues = new List<FieldValue>(ThisItem.FieldValues);
            foreach (var fv in fieldValues)
                if (fv.Value == null && (ItemCopy == null || ItemCopy.GetFieldValue(fv.FieldName, false) == null))
                    ThisItem.RemoveFieldValue(fv);

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { ItemCopy, ThisItem },
                    BodyTypeName = "Item",
                    ID = ThisItem.ID
                });

            // create a copy of the new baseline
            ItemCopy = new Item(ThisItem, true);

            // reload the folder in case it changed
            folder = App.ViewModel.Folders.Single(f => f.ID == folder.ID);

            // save the changes to local storage
            if (folder.Items.Any(i => i.ID == ThisItem.ID))
            {
                var existingItem = folder.Items.Single(i => i.ID == ThisItem.ID);
                if (existingItem != ThisItem)
                    existingItem.Copy(ThisItem, true);
            }
            else
            {
                TraceHelper.AddMessage("ItemPage: cannot find existing item to update");
                folder.Items.Add(ThisItem);
            }
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();
        }

		#region Helpers
  
        private string CreateCommaDelimitedList(Item list)
        {
            if (list == null)
                return null;
            
            // build a comma-delimited list of names to display in a control
            List<string> names = list.Items.Select(it => it.Name).ToList();
            StringBuilder sb = new StringBuilder();
            bool comma = false;
            foreach (var name in names)
            {
                if (comma)
                    sb.Append(", ");
                else
                    comma = true;
                sb.Append(name);
            }
            return sb.ToString();
        }
        
        private Item CreateItemCopyWithChildren(Guid itemID)
        {
            Item item = new Item(App.ViewModel.Items.Single(it => it.ID == itemID));
            item.Items = App.ViewModel.Items.Where(it => it.ParentID == itemID).ToObservableCollection();    
            return item;
        }       
        
        private Item CreateValueList(Item item, Field field, Guid itemID)
        {
            Item list;
            if (itemID == Guid.Empty) 
            {
                list = new Item()
                {
                    ID = itemID, // signal new list
                    Name = field.Name,
                    IsList = true,
                    FolderID = folder.ID,
                    ParentID = item.ID,
                    ItemTypeID = SystemItemTypes.Reference,
                };
            }
            else
            {
                // get the current value list
                list = CreateItemCopyWithChildren((Guid) itemID);
            }
            return list;
        }
        
        private string GetTypeName(PropertyInfo pi)
        {
            string typename = pi.PropertyType.Name;
            // if it's a generic type, get the underlying type (this is for Nullables)
            if (pi.PropertyType.IsGenericType)
            {
                typename = pi.PropertyType.FullName;
                string del = "[[System.";  // delimiter
                int index = typename.IndexOf(del);
                index = index < 0 ? index : index + del.Length;  // add length of delimiter
                int index2 = index < 0 ? index : typename.IndexOf(",", index);
                // if anything went wrong, default to String
                if (index < 0 || index2 < 0)
                    typename = "String";
                else
                    typename = typename.Substring(index, index2 - index);
            }
            return typename;
        }

		/// <summary>
        /// Find a item by ID and then return its index 
        /// </summary>
        /// <param name="observableCollection"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        private int IndexOf(Folder folder, Item item)
        {
            try
            {
                Item itemRef = folder.Items.Single(t => t.ID == item.ID);
                return folder.Items.IndexOf(itemRef);
            }
            catch (Exception)
            {
                return -1;
            }
        }
		
		private void NavigateBack()
		{
			// since we're in the edit page, we need to pop twice
			controller.PopViewControllerAnimated(false);
			controller.PopViewControllerAnimated(true);
			root.Dispose();
		}

        private void ParseFields(Item item)
        {
            string text = item.Description;
            if (text == null || text == "")
                return;

            Match m;

            // parse the text for a phone number
            m = Regex.Match(text, @"(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Phone = m.Value;

            // parse the text for an email address
            m = Regex.Match(text, @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[A-Z]{2}|com|org|net|edu|gov|mil|biz|info|mobi|name|aero|asia|jobs|museum)\b", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Email = m.Value;

            // parse the text for a website
            m = Regex.Match(text, @"((http|https)(:\/\/))?([a-zA-Z0-9]+[.]{1}){2}[a-zA-z0-9]+(\/{1}[a-zA-Z0-9]+)*\/?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Website = m.Value;           
        }
		
		private RootElement RenderEditItem(Item item, bool renderListInfo)
        {
            // get itemType for this item
			ItemType itemType = null;
            try
            {
                itemType = App.ViewModel.ItemTypes.Single(it => it.ID == item.ItemTypeID);
            }
            catch (Exception)
            {
                // if can't find the folder type, use the first
                itemType = App.ViewModel.ItemTypes[0];
            }

            // render the primary fields
			Section primarySection = RenderEditItemFields(item, itemType, true);

            // HACK: insert a dummy element inside the primary section.  This dummy element
            // implements IElementSizing and returns a 0-sized cell (so it is invisible).  
            // This is to work around an issue in MT.Dialog where it doesn't honor IElementSizing
            // on elements that are added after a dialog is already drawn.  This element must be 
            // inserted after the first element but before the last element, to avoid losing the 
            // rounded rectangle look of the first and last elements of a Section.
            if (primarySection.Count > 0)
                primarySection.Insert(1, new DummyElement());

			// render more button
            var moreButton = new Button() { Background = "Images/darkgreybutton.png", Caption = "more details" };
            var sse = new ButtonListElement() { Margin = 0f };
            sse.Buttons.Add(moreButton);
            var moreSection = new Section() { sse };

            // render save/delete buttons
            var actionButtons = new ButtonListElement() 
            {
                //new Button() { Caption = "Save", Background = "Images/greenbutton.png", Clicked = SaveButton_Click },
                new Button() { Caption = "Delete", Background = "Images/redbutton.png", Clicked = DeleteButton_Click }, 
            };
            actionButtons.Margin = 0f;
            
			// create the dialog with the primary section
			RootElement editRoot = new RootElement(item.Name)
			{
				primarySection,
                moreSection,
                new Section() { actionButtons },
			};

            moreButton.Clicked += (s, e) => 
			{
                // remove the "more" button
            	editRoot.Remove(moreSection);

                // render the non-primary fields as a new section
                editRoot.Insert(1, RenderEditItemFields(item, itemType, false));
				
                // create a separate section with the advanced information (parent, type)
                var advancedSection = new Section();

                Field field = new Field() { Name = "ParentID", DisplayName = "List", DisplayType = DisplayTypes.Lists };
                advancedSection.Add(RenderEditItemField(item, field));

                field = new Field() { Name = "ItemTypeID", DisplayName = "Type", DisplayType = DisplayTypes.ItemTypes };
                advancedSection.Add(RenderEditItemField(item, field));

                editRoot.Insert(2, advancedSection);
            };
			
			return editRoot;
        }
		
        private Element RenderEditItemField(Item item, Field field)
        {
            PropertyInfo pi = null;
            object currentValue = null;
            object container = null;

            // get the current field value.
            // the value can either be in a strongly-typed property on the item (e.g. Name),
            // or in one of the FieldValues 
            try
            {
                // get the strongly typed property
                pi = item.GetType().GetProperty(field.Name);
                if (pi != null)
                {
                    // store current item's value for this field
                    currentValue = pi.GetValue(item, null);

                    // set the container - this will be the object that will be passed 
                    // to pi.SetValue() below to poke new values into
                    container = item;
                }
            }
            catch (Exception)
            {
                // an exception indicates this isn't a strongly typed property on the Item
                // this is NOT an error condition
            }

            // if couldn't find a strongly typed property, this property is stored as a 
            // FieldValue on the item
            if (pi == null)
            {
                // get current item's value for this field, or create a new FieldValue
                // if one doesn't already exist
                FieldValue fieldValue = item.GetFieldValue(field.ID, true);
                currentValue = fieldValue.Value;

                // get the value property of the current fieldvalue (this should never fail)
                pi = fieldValue.GetType().GetProperty("Value");
                if (pi == null)
                    return null;

                // set the container - this will be the object that will be passed 
                // to pi.SetValue() below to poke new values into
                container = fieldValue;
            }
			
			// most elements will be Entry Elements - default to this
			EntryElement entryElement = new EntryElement(field.DisplayName, "", "");
			Element element = entryElement;			
			
            bool notMatched = false;
            // render the right control based on the type 
            switch (field.DisplayType)
            {
                case DisplayTypes.Text:
					//StyledMultilineElement stringElement = new StyledMultilineElement(field.DisplayName, (string) currentValue);
					entryElement.KeyboardType = UIKeyboardType.Default;
                    entryElement.Value = (string) currentValue;
					entryElement.AutocorrectionType = UITextAutocorrectionType.Yes;
                    entryElement.Changed += delegate { 
                        pi.SetValue(container, entryElement.Value, null); };
					//element = stringElement;
                    break;
                case DisplayTypes.TextArea:
                    MultilineEntryElement multilineElement = new MultilineEntryElement(field.DisplayName, (string) currentValue) { Lines = 3 };
                    multilineElement.Changed += delegate { pi.SetValue(container, multilineElement.Value, null); };
                    //var multilineElement = new MultilineEntryElement3(field.DisplayName, (string) currentValue) { Editable = true };
                    //multilineElement.Changed += delegate { pi.SetValue(container, multilineElement.Value, null); };
                    element = multilineElement;
                    //entryElement.Value = (string) currentValue;
                    //entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case DisplayTypes.Phone:
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.PhonePad;
                    entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case DisplayTypes.Link:
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.Url;
                    entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case DisplayTypes.Email:
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.EmailAddress;
                    entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case DisplayTypes.Address:
                    entryElement.Value = (string) currentValue;
                    entryElement.AutocorrectionType = UITextAutocorrectionType.Yes;
                    entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case DisplayTypes.Priority:
					var priorities = new RadioGroup(field.DisplayName, 0);
					priorities.Selected = 
						((int?) currentValue) != null ? 
						(int) currentValue : 
						1;  // HACK: hardcode to "Normal" priority.  this should come from a table.
                    var priSection = new Section();
                    priSection.AddAll(
                        from pr in App.ViewModel.Constants.Priorities 
                             select (Element) new RadioEventElement(pr.Name, field.DisplayName));
                    var priorityElement = new ThemedRootElement(field.DisplayName, priorities) { priSection };
					// augment the radio elements with the right index and event handler
					int i = 0;
					foreach (var radio in priorityElement[0].Elements)
					{
						RadioEventElement radioEventElement = (RadioEventElement) radio;
						int index = i++;
                        radioEventElement.OnSelected += delegate { pi.SetValue(container, index, null); };
					}
					element = priorityElement;
                    break;
                case DisplayTypes.Lists:
                    // create a collection of lists in this folder, and add the folder as the first entry
                    var lists = App.ViewModel.Items.
                        Where(li => li.FolderID == item.FolderID && li.IsList == true && li.ItemTypeID != SystemItemTypes.Reference).
                            OrderBy(li => li.Name).
                            ToObservableCollection();
                    lists.Insert(0, new Item()
                    {
                        ID = Guid.Empty,
                        Name = folder.Name
                    });
                    // a null value for the "list" field indicates a Folder as a parent (i.e. this item is a top-level item)
                    if (currentValue == null)
                        currentValue = Guid.Empty;
                    Item currentList = lists.FirstOrDefault(li => li.ID == (Guid) currentValue);
					var listsGroup = new RadioGroup (field.DisplayName, 0);
					listsGroup.Selected = Math.Max(lists.IndexOf(currentList), 0);
                    var listsSection = new Section();
                    listsSection.AddAll(
                        from l in lists
                            select (Element) new RadioEventElement(l.Name, field.DisplayName));
                    var listsElement = new ThemedRootElement(field.DisplayName, listsGroup) { listsSection };
					// augment the radio elements with the right index and event handler
					int index = 0;
					foreach (var radio in listsElement[0].Elements)
					{
						int currentIndex = index;  // make a local copy for the closure									
						RadioEventElement radioEventElement = (RadioEventElement) radio;
						radioEventElement.OnSelected += delegate(object sender, EventArgs e)
						{
							pi.SetValue(container, lists[currentIndex].ID, null); 
						};
						index++;
					}
					element = listsElement;
                    break;
                case DisplayTypes.DatePicker:
                    DateTime? dateTime = String.IsNullOrEmpty((string) currentValue) ? (DateTime?) null : Convert.ToDateTime((string) currentValue);
					DateEventElement dateElement = new DateEventElement(field.DisplayName, dateTime);
					dateElement.ValueSelected += delegate 
                    {
                        pi.SetValue(container, ((DateTime)dateElement.DateValue).ToString("yyyy/MM/dd"), null);
                        folder.NotifyPropertyChanged("FirstDue");
                        folder.NotifyPropertyChanged("FirstDueColor");
                    };
					element = dateElement;
                    break;
                case DisplayTypes.DateTimePicker:
                    DateTime? dt = String.IsNullOrEmpty((string) currentValue) ? (DateTime?) null : Convert.ToDateTime((string) currentValue);
                    DateTimeEventElement dateTimeElement = new DateTimeEventElement(field.DisplayName, dt);
                    dateTimeElement.ValueSelected += (s, e) => 
                    {
                        pi.SetValue(container, dateTimeElement.DateValue == null ? null : ((DateTime) dateTimeElement.DateValue).ToString(), null);
                        folder.NotifyPropertyChanged("FirstDue");
                        folder.NotifyPropertyChanged("FirstDueColor");
                    };
                    element = dateTimeElement;
                    break;
                case DisplayTypes.Checkbox:
					CheckboxElement checkboxElement = new CheckboxElement(field.DisplayName, currentValue == null ? false : (bool) currentValue);
                    checkboxElement.Tapped += delegate { pi.SetValue(container, checkboxElement.Value, null); };
					element = checkboxElement;
                    break;
                case DisplayTypes.TagList:
                    // TODO                   
                    element = null;
                    break;
                case DisplayTypes.ImageUrl:
                    // TODO: wire up to picture picker, and upload to an image service
                    element = null;
                    break;
                case DisplayTypes.LinkArray:
                    var linkArrayElement = new MultilineEntryElement(field.DisplayName, (string) currentValue) { Lines = 3, AcceptReturns = true };
                    if (!String.IsNullOrEmpty((string) currentValue))
                    {
                        try
                        {
                            var linkList = JsonConvert.DeserializeObject<List<Link>>((string)currentValue);
                            linkArrayElement.Value = String.Concat(linkList.Select(l => l.Name != null ? l.Name + "," + l.Url + "\n" : l.Url + "\n").ToList());
                        }
                        catch (Exception)
                        {
                        }
                    }
                    linkArrayElement.Changed += delegate 
                    {
                        // the expected format is a newline-delimited list of Name, Url pairs
                        var linkArray = linkArrayElement.Value.Split(new char[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var linkList = new List<Link>();
                        foreach (var link in linkArray)
                        {
                            var nameval = link.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (nameval.Length == 0)
                                continue;
                            if (nameval.Length == 1)
                                linkList.Add(new Link() { Url = nameval[0].Trim() });
                            else
                                linkList.Add(new Link() { Name = nameval[0].Trim(), Url = nameval[1].Trim() });
                        }
                        var json = JsonConvert.SerializeObject(linkList);
                        pi.SetValue(container, json, null); 
                    };
                    element = linkArrayElement;
                    break;
                case DisplayTypes.Hidden:
                    // skip rendering
                    element = null;
                    break;
                case DisplayTypes.ContactList:
                    StringElement contactsElement = new StringElement(field.DisplayName);
                    Item currentContacts = CreateValueList(item, field, currentValue == null ? Guid.Empty : new Guid((string) currentValue));
                    contactsElement.Value = CreateCommaDelimitedList(currentContacts);
                    Item contacts = new Item()
                    {
                        Items = App.ViewModel.Items.
                            Where(it => it.ItemTypeID == SystemItemTypes.Contact && it.IsList == false).
                            Select(it => new Item() { Name = it.Name, FolderID = folder.ID, ItemTypeID = SystemItemTypes.Reference, ParentID = currentContacts.ID, ItemRef = it.ID }).
                            ToObservableCollection(),
                    };
                    contactsElement.Tapped += delegate 
                    {
                        // put up the list picker dialog
                        ListPickerPage listPicker = new ListPickerPage(
                            SystemItemTypes.Contact,
                            editViewController.NavigationController, 
                            contactsElement,
                            pi,
                            container,
                            field.DisplayName, 
                            currentContacts, 
                            contacts);
                        listPicker.PushViewController();
                    };
                    element = contactsElement;
                    break;
                case DisplayTypes.LocationList:
                    StringElement locationsElement = new StringElement(field.DisplayName);
                    Item currentLocations = CreateValueList(item, field, currentValue == null ? Guid.Empty : new Guid((string) currentValue));
                    locationsElement.Value = CreateCommaDelimitedList(currentLocations);
                    Item locations = new Item()
                    {
                        Items = App.ViewModel.Items.
                            Where(it => it.ItemTypeID == SystemItemTypes.Location && it.IsList == false).
                            Select(it => new Item() { Name = it.Name, FolderID = folder.ID, ItemTypeID = SystemItemTypes.Reference, ParentID = currentLocations.ID, ItemRef = it.ID }).
                            ToObservableCollection(),
                    };
                    locationsElement.Tapped += delegate 
                    {
                        // put up the list picker dialog
                        ListPickerPage listPicker = new ListPickerPage(
                            SystemItemTypes.Location,
                            editViewController.NavigationController, 
                            locationsElement,
                            pi,
                            container,
                            field.DisplayName, 
                            currentLocations, 
                            locations);
                        listPicker.PushViewController();
                    };
                    element = locationsElement;
                    break;
                case DisplayTypes.ItemTypes:
                    var itemTypePickerElement = new ItemTypePickerElement("Type", (Guid) currentValue);
                    itemTypePickerElement.OnSelected += (sender, e) => { pi.SetValue(container, itemTypePickerElement.SelectedItemType, null); };
                    element = itemTypePickerElement;
                    break;
                default:
                    notMatched = true;
                    break;
            }
			
            // if wasn't able to match field type by display type, try matching by FieldType
            if (notMatched == true)
            {
                switch (field.FieldType)
                {
                    case FieldTypes.String:
                    default:
                        entryElement.KeyboardType = UIKeyboardType.Default;
                        entryElement.Value = (string) currentValue;
                        entryElement.AutocorrectionType = UITextAutocorrectionType.Yes;
                        entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                        break;
                    case FieldTypes.Integer:
                        entryElement.Value = (string) currentValue;
                        entryElement.KeyboardType = UIKeyboardType.NumberPad;
                        entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                        break;
                    case FieldTypes.DateTime:
                        DateTime dateTime = currentValue == null ? DateTime.Now.Date : Convert.ToDateTime ((string) currentValue);
                        DateEventElement dateElement = new DateEventElement(field.DisplayName, dateTime);
                        dateElement.ValueSelected += delegate 
                        {
                            pi.SetValue(container, ((DateTime)dateElement.DateValue).ToString("yyyy/MM/dd"), null);
                            folder.NotifyPropertyChanged("FirstDue");
                            folder.NotifyPropertyChanged("FirstDueColor");
                        };
                        element = dateElement;
                        break;
                    case FieldTypes.Boolean:
                        CheckboxElement checkboxElement = new CheckboxElement(field.DisplayName, currentValue == null ? false : (bool) currentValue);
                        checkboxElement.Tapped += delegate { pi.SetValue(container, checkboxElement.Value, null); };
                        element = checkboxElement;
                        break;
                }
            }

			return element;
        }
        
        private Section RenderEditItemFields(Item item, ItemType itemtype, bool primary)
        {
            Section section = new Section();
			
            // render fields
            foreach (Field f in itemtype.Fields.Where(f => f.IsPrimary == primary).OrderBy(f => f.SortOrder))
            {
                var e = RenderEditItemField(item, f);
                if (e != null)
                    section.Add(e);
            }
            
			return section;
        }
		
		/*
        private static StackPanel RenderEditItemImageButtonPanel(TextBox tb)
        {
            tb.MinWidth -= 64;
            StackPanel innerPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
            innerPanel.Children.Add(tb);
            ImageButton imageButton = new ImageButton()
            {
                Image = new BitmapImage(new Uri("/Images/button.search.png", UriKind.Relative)),
                PressedImage = new BitmapImage(new Uri("/Images/button.search.pressed.png", UriKind.Relative)),
                Width = 48,
                Height = 48,
                Template = (ControlTemplate)App.Current.Resources["ImageButtonControlTemplate"]
            };
            innerPanel.Children.Add(imageButton);
            return innerPanel;
        }

        private void RenderEditItemTagList(TextBox taglist, Item item, PropertyInfo pi)
        {
            taglist.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };

            // build the comma delimited tag folder for this item
            bool addDelimiter = false;
            StringBuilder sb = new StringBuilder();
            var itemtags = (IEnumerable<ItemTag>)pi.GetValue(item, null);
            if (itemtags != null)
            {
                foreach (ItemTag tt in itemtags)
                {
                    if (addDelimiter)
                        sb.Append(",");
                    Tag tag = App.ViewModel.Tags.Single(t => t.ID == tt.TagID);
                    sb.Append(tag.Name);
                    addDelimiter = true;
                }
                taglist.Text = sb.ToString();
            }

            // retrieve the itemtags for the item, creating new tags along the way
            taglist.LostFocus += new RoutedEventHandler(delegate
            {
                //ObservableCollection<ItemTag> existingTags = (ObservableCollection<ItemTag>)pi.GetValue(item, null);
                ObservableCollection<ItemTag> newTags = new ObservableCollection<ItemTag>();
                string[] tags = taglist.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tt in tags)
                {
                    string str = tt.Trim();
                    Tag tag;
                    try
                    {
                        tag = App.ViewModel.Tags.Single(t => t.Name == str);
                        newTags.Add(new ItemTag() { Name = str, TagID = tag.ID, ItemID = item.ID });
                    }
                    catch (Exception)
                    {
                        // this is a new tag that we need to create 
                        tag = new Tag() { Name = str };
                        newTags.Add(new ItemTag() { Name = str, TagID = tag.ID, ItemID = item.ID });

                        // enqueue the Web Request Record 
                        RequestQueue.EnqueueRequestRecord(
                            new RequestQueue.RequestRecord()
                            {
                                ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                                Body = new Tag(tag)
                            });

                        // add the tag to the tag folder
                        App.ViewModel.Tags.Add(tag);

                        // save the changes to local storage
                        StorageHelper.WriteTags(App.ViewModel.Tags);
                    }
                }

                // store the new ItemTag collection in the item
                pi.SetValue(item, newTags, null);

                // create the mirror Tags collection in the item
                item.CreateTags(App.ViewModel.Tags);
            });
        }
		 */
        private RootElement RenderViewItem(Item item)
        {
            // get the item type
            ItemType itemType = null;
            if (ItemType.ItemTypes.TryGetValue(item.ItemTypeID, out itemType) == false)
                return null;
			
            Section name = new Section(item.Name);
            Section section = new Section(item.Description);

            var picFV = item.GetFieldValue(FieldNames.Picture);
            if (picFV != null && !String.IsNullOrWhiteSpace(picFV.Value))
            {
                var image = ImageLoader.DefaultRequestImage(new Uri(picFV.Value), this);//new ImageLoaderCallback(actionsViewController, name));
                if (image != null)
                    name.FooterView = RenderViewItemPicture(image);
            }

            // create a list of all the first subitems in each of the sublists of the item (e.g. Contacts, Places, etc)
            var subLists = App.ViewModel.Items.Where(i => i.ParentID == item.ID && i.IsList == true).ToList();
            var subItems = new List<Item>();
            foreach (var subList in subLists)
            {
                var itemRef = App.ViewModel.Items.FirstOrDefault(i => i.ParentID == subList.ID && i.ItemTypeID == SystemItemTypes.Reference);
                if (itemRef != null)
                {
                    var target = App.ViewModel.Items.FirstOrDefault(i => i.ID == itemRef.ItemRef);
                    if (target != null)
                        subItems.Add(target);
                }
            }

            // render fields
            foreach (ActionType action in App.ViewModel.Constants.ActionTypes.OrderBy(a => a.SortOrder))
            {
                FieldValue fieldValue = item.FieldValues.FirstOrDefault(fv => fv.FieldName == action.FieldName);
                if (fieldValue == null)
                {
                    bool found = false;
                    foreach (var i in subItems)
                    {
                        fieldValue = i.FieldValues.FirstOrDefault(fv => fv.FieldName == action.FieldName);
                        if (fieldValue != null)
                        {
                            found = true;
                            break;
                        }
                    }

                    // if fieldvalue isn't found on this item or other subitems, don't process the action
                    if (found == false)
                        continue;
                }

                // get the value of the property
                string currentValue = fieldValue.Value;

                // for our purposes, an empty value is the same as null
                if (currentValue == "")
                    currentValue = null;

                // render this property if it's not null/empty
                if (currentValue != null)
                {
                    // first make sure that we do want to render (type-specific logic goes here)
                    switch (action.ActionName)
                    {
                        case "Postpone":
                            // if the date is already further in the future than today, omit adding this action
                            if (Convert.ToDateTime(currentValue).Date > DateTime.Today.Date)
                                continue;
                            break;
                    }
                    
                    // Create a StyledStringElement to hold the action.  The Caption is the verb (action), 
					// while the Value is the noun that the verb will act upon 
                    // (usually extracted from the item field's contents)
                    StyledStringElement stringElement = new StyledStringElement(action.DisplayName, currentValue, UITableViewCellStyle.Value1);
					Element element = stringElement; 
                    
                    // render the action based on the action type
                    switch (action.ActionName)
                    {
                        case ActionNames.Navigate:
						    try
                            {
                                Item newItem = App.ViewModel.Items.Single(it => it.ID == Guid.Parse(currentValue));
                                //stringElement.Value = String.Format("to {0}", newItem.Name);
                                stringElement.Value = "";
                                stringElement.Tapped += delegate 
                                {
                                    // Navigate to the new page
                                    if (newItem != null)
                                    {
                                        if (newItem.IsList == true)
                                        {
                                            // Navigate to the list page
                                            UIViewController nextController = new ListViewController(this.controller, folder, newItem.ID);  
                                            TraceHelper.StartMessage("Item: Navigate to ListPage");
                                            this.controller.PushViewController(nextController, true);
                                        }
                                        else
                                        {
                                            // if the item is a reference, traverse to the target
                                            while (newItem.ItemTypeID == SystemItemTypes.Reference && newItem.ItemRef != null)
                                            {
                                                try 
                                                {
                                                    newItem = App.ViewModel.Items.Single(it => it.ID == newItem.ItemRef);
                                                }
                                                catch
                                                {
                                                    TraceHelper.AddMessage(String.Format("Couldn't find item reference for name {0}, id {1}, ref {2}", 
                                                                                         newItem.Name, newItem.ID, newItem.ItemRef));
                                                    break;
                                                }
                                            }
                                            ItemPage itemPage = new ItemPage(controller.NavigationController, newItem);
                                            TraceHelper.StartMessage("Item: Navigate to ItemPage");
                                            itemPage.PushViewController();
                                        }
                                    }
                                };
                            }
                            catch (Exception)
                            {
                                stringElement.Value = "(item not found)";
                            }
                            break;
                        case ActionNames.Postpone:
                            stringElement.Value = "to tomorrow";
                            stringElement.Tapped += delegate
                            {
                                TimeSpan time = Convert.ToDateTime(currentValue).TimeOfDay;
                                fieldValue.Value = (DateTime.Today.Date.AddDays(1.0) + time).ToString();
                                // save the item and trigger a sync with the service  
                                SaveButton_Click(null, null);
                                // reload the Actions page 
                                var oldroot = root;
                                root = RenderViewItem(ThisItem);
                                actionsViewController.Root = root;
                                actionsViewController.ReloadData();
                                oldroot.Dispose();
                                
                                folder.NotifyPropertyChanged("FirstDue");
                                folder.NotifyPropertyChanged("FirstDueColor");
                            };
                            break;
                        case ActionNames.AddToCalendar:
                            DateTime dt = Convert.ToDateTime(currentValue);
                            stringElement.Value = dt.TimeOfDay == TimeSpan.FromSeconds(0d) ? dt.Date.ToString() : dt.ToString();
                            stringElement.Tapped += delegate
                            {
                                folder.NotifyPropertyChanged("FirstDue");
                                folder.NotifyPropertyChanged("FirstDueColor");
                            };
                            break;
                        case ActionNames.Map:
                            stringElement.Tapped += delegate
                            {
                                // try to use the maps: URL scheme
                                string url = "maps://" + currentValue.Replace(" ", "%20");
                                if (UIApplication.SharedApplication.CanOpenUrl(new NSUrl(url)))
                                    UIApplication.SharedApplication.OpenUrl(new NSUrl(url));
                                else
                                {
                                    // open the google maps website
                                    url = url.Replace("maps://", "http://maps.google.com/maps?q=");
                                    UIApplication.SharedApplication.OpenUrl(new NSUrl(url));
                                }                                    
                            };                         
                            break;
                        case ActionNames.Call:
                           stringElement.Tapped += delegate
                            {
                                // construct the correct URL
                                List<string> urlComponents = new List<string>() { "tel://" };
                                urlComponents.AddRange(currentValue.Split('(', ')', '-', ' '));
                                string url = String.Concat(urlComponents);                               
                                if (UIApplication.SharedApplication.OpenUrl(new NSUrl(url)) == false)
                                    MessageBox.Show("Can't make a phone call");
                            };
                            break;
                        case ActionNames.TextMessage:
                            stringElement.Tapped += delegate
                            {
                                // construct the correct URL
                                List<string> urlComponents = new List<string>() { "sms:" };
                                urlComponents.AddRange(currentValue.Split('(', ')', '-', ' '));
                                string url = String.Concat(urlComponents);                               
                                if (UIApplication.SharedApplication.OpenUrl(new NSUrl(url)) == false)
                                    MessageBox.Show("Can't send a text message");
                            };                             
                            break;
                        case ActionNames.Browse:
                            // construct the correct URL
                            var linkList = JsonConvert.DeserializeObject<List<Link>>(currentValue);
                            if (linkList.Count == 0)
                                continue;
                            string linkUrl = linkList[0].Url.Replace(" ", "%20");
                            if (linkUrl.Substring(0, 4) != "http")
                                linkUrl = String.Format("http://{0}", linkUrl);
                            StyledHtmlElement browserElement = new StyledHtmlElement(action.DisplayName, linkUrl, UITableViewCellStyle.Value1, linkUrl);
							element = browserElement;
                            break;
                        case ActionNames.SendEmail:
                            stringElement.Tapped += delegate
                            {
                                // construct the correct URL
                                string emailUrl = currentValue.Trim();                              
                                if (UIApplication.SharedApplication.OpenUrl(new NSUrl(emailUrl)) == false)
                                    MessageBox.Show("Can't launch the email application");
                            };                             
                            break;                 
                    }
					
					// add the element to the section (note that the reference may have been
					// reset in the switch statement)
					section.Add (element);
				}
			}
			
			return new RootElement("Actions") { name, section };
        }

        private UIView RenderViewItemPicture(UIImage image)
        {
            var width = controller.View.Frame.Width;
            var wrapperView = new UIView(new RectangleF(0, 0, 60, 60)) { AutosizesSubviews = false };
            var imageView = new UIImageView(image) { Frame = new RectangleF(width - 70, -35, 60, 60) };
            imageView.Layer.BorderColor = UIColor.Gray.CGColor;
            imageView.Layer.BorderWidth = 2.0f;
            wrapperView.AddSubview(imageView);
            return wrapperView;
        }

        void IImageUpdated.UpdatedImage(Uri uri)
        {
            if (uri != null)
                actionsViewController.TableView.ReloadData();
        }

        #endregion
	}
}

