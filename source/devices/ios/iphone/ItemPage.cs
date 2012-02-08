using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using System.Text.RegularExpressions;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class ItemPage
	{
		Item thisItem = null;
		Item itemCopy = null;
		Folder folder = null;
		UINavigationController controller;
		
		public ItemPage(UINavigationController c, Item item)
		{
			// trace event
            TraceHelper.AddMessage("Item: constructor");
			controller = c;
			thisItem = item;
		}
		
		public void NavigateTo()
		{
			// trace event
            TraceHelper.AddMessage("Item: NavigateTo");

			// make a copy - thisItem is what the winphone project uses
			//Item thisItem = item;
			//Item itemCopy = null;
			
            try
            {
                folder = App.ViewModel.LoadFolder(thisItem.FolderID);
            }
            catch (Exception)
            {
                folder = null;
				return;
            }

	         // make a deep copy of the item for local binding
	        itemCopy = new Item(thisItem);
			var root = RenderViewItem(itemCopy);			
			var dvc = new DialogViewController (root, true);
			
			// create an Edit button which pushes the edit view onto the nav stack
			dvc.NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Edit, delegate {
				var editRoot = RenderEditItem(itemCopy, true /* render the list field */);
				var editdvc = new DialogViewController(editRoot, true);
				controller.PushViewController(editdvc, true);
			});
			
			// push the "view item" view onto the nav stack
			controller.PushViewController (dvc, true);
			
			//return dvc;
		}
				
        private void CancelButton_Click(object sender, EventArgs e)
        //private void CancelButton_Click()
        {
            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the tastlist page
            //NavigationService.GoBack();
        }
		
		private void DeleteButton_Click(object sender, EventArgs e)
		//private void DeleteButton_Click()
        {
            // if this is a new item, delete just does the same thing as cancel
            if (thisItem == null)
            {
                CancelButton_Click(sender, e);
                //CancelButton_Click();
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this item?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = itemCopy
                });

            // remove the item from the local itemType
            folder.Items.Remove(thisItem);

            // save the changes to local storage
            //StorageHelper.WriteFolders(App.ViewModel.Folders);
            StorageHelper.WriteFolder(folder);

            // trigger a databinding refresh for items
            //App.ViewModel.NotifyPropertyChanged("Items");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the folder page
            //NavigationService.GoBack();
        }
		
		private void SaveButton_Click(object sender, EventArgs e)
		//private void SaveButton_Click()
        {
            // update the LastModified timestamp
            itemCopy.LastModified = DateTime.UtcNow;

            //ParseFields(itemCopy);

            // if this is a new item, create it
            if (thisItem == null)
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = itemCopy
                        });

                // add the item to the local itemType
                folder.Items.Add(itemCopy);
                thisItem = itemCopy;
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { thisItem, itemCopy },
                        BodyTypeName = "Item",
                        ID = thisItem.ID
                    });

                // save the changes to the existing item
                int index = IndexOf(folder, thisItem);
                if (index < 0)
                    return; 
                folder.Items[index] = itemCopy;
                thisItem = itemCopy;
            }
            
            // save the changes to local storage
            //StorageHelper.WriteFolders(App.ViewModel.Folders);
            StorageHelper.WriteFolder(folder);

            // trigger a databinding refresh for items
            //App.ViewModel.NotifyPropertyChanged("Items");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // signal the folder that the FirstDue property needs to be recomputed
            folder.NotifyPropertyChanged("FirstDue");

            // trace page navigation
            TraceHelper.StartMessage("Item: Navigate back");

            // Navigate back to the folder page
            //NavigationService.GoBack();
        }

		#region Helpers
		
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
		
		private RootElement RenderEditItem(Item item, bool renderFolderField)
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
			Section primarySection = RenderEditItemFields(item, itemType, true, renderFolderField);

			// render more button
			var sse = new StyledStringElement("more details")			                                  
			{ 
				Accessory = UITableViewCellAccessory.DisclosureIndicator,
			};
			// add the more button to the section
			primarySection.Add (sse);
			
			// create the dialog with the primary section
			RootElement root = new RootElement(item.Name)
			{
				primarySection,
				new Section() 
				{ 
					new ButtonListElement() 
					{
						new Button() { Caption = "Save", Color = UIColor.Green, Clicked = SaveButton_Click },
						new Button() { Caption = "Delete", Color = UIColor.Red, Clicked = DeleteButton_Click }, 
					},
				},
			};

			sse.Tapped += delegate 
			{
				// render the non-primary fields as a new section
            	root.Insert(1, RenderEditItemFields(item, itemType, false, false));
				
				// remove the "more" button
				primarySection.Remove (sse);
			};			
			
			return root;
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
            }

            // if couldn't find a strongly typed property, this property is stored as a 
            // FieldValue on the item
            if (pi == null)
            {
                FieldValue fieldValue = null;
                // get current item's value for this field
                try
                {
                    fieldValue = item.FieldValues.Single(fv => fv.FieldID == field.ID);
                    currentValue = fieldValue.Value;
                }
                catch (Exception)
                {
                }

                // get the item copy's fieldvalue for this field
                // we use this to write changes to the field's value
                try
                {
                    fieldValue = item.FieldValues.Single(fv => fv.FieldID == field.ID);
                }
                catch (Exception)
                {
                    fieldValue = new FieldValue()
                    {
                        FieldID = field.ID,
                        ItemID = item.ID,
                    };
                }

                // get the value property of the current fieldvalue
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
                case "String":
					//StyledMultilineElement stringElement = new StyledMultilineElement(field.DisplayName, (string) currentValue);
					entryElement.KeyboardType = UIKeyboardType.Default;
                    entryElement.Value = (string) currentValue;
					entryElement.AutocorrectionType = UITextAutocorrectionType.Yes;
					entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
					//element = stringElement;
                    break;
				case "TextBox":
					MultilineElement multilineElement = new MultilineElement(field.DisplayName, (string) currentValue);
					multilineElement.Tapped += delegate 
					{
					};
					element = multilineElement;
                    break;
                case "Phone":
                case "PhoneNumber":
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.PhonePad;
					entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case "Website":
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.Url;
					entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case "Email":
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.EmailAddress;
					entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case "Location":
                case "Address":
                    entryElement.Value = (string) currentValue;
                    entryElement.AutocorrectionType = UITextAutocorrectionType.Yes;
					entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
				case "Priority":
					var priorities = new RadioGroup (field.DisplayName, 0);
					priorities.Selected = 
						((int?) currentValue) != null ? 
						(int) currentValue : 
						1;  // HACK: hardcode to "Normal" priority.  this should come from a table.
					var priorityElement = new RootElement(field.DisplayName, priorities)
					{
						new Section () 
						{ 
							from pr in App.ViewModel.Constants.Priorities 
								select (Element) new RadioEventElement(pr.Name, field.DisplayName)
						}
					};
					// augment the radio elements with the right index and event handler
					int i = 0;
					foreach (var radio in priorityElement[0].Elements)
					{
						RadioEventElement radioEventElement = (RadioEventElement) radio;
						radioEventElement.Value = i.ToString();
						i++;
						radioEventElement.OnSelected += delegate(object sender, EventArgs e)
						{
							pi.SetValue(container, Convert.ToInt32(((RadioEventElement)sender).Value), null); 
						};
					}
					element = priorityElement;
		            //var root = priorityElement.GetImmediateRootElement ();
		            //root.Reload (se, UITableViewRowAnimation.Fade);

                    break;
				/*
                case "Folder":
                    ListPicker folderPicker = new ListPicker() { MinWidth = minWidth, IsTabStop = true };
                    folderPicker.ItemsSource = App.ViewModel.Folders;
                    folderPicker.DisplayMemberPath = "Name";
                    Folder tl = App.ViewModel.Folders.FirstOrDefault(list => list.ID == folder.ID);
                    folderPicker.SelectedIndex = App.ViewModel.Folders.IndexOf(tl);
                    folderPicker.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(container, App.ViewModel.Folders[folderPicker.SelectedIndex].ID, null); });
                    folderPicker.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(folderPicker);
                    break;
                case "List":
                    ListPicker listPicker = new ListPicker()
                    {
                        MinWidth = minWidth,
                        FullModeItemTemplate = (DataTemplate)App.Current.Resources["FullListPickerTemplate"],
                        IsTabStop = true
                    };
                    var lists = App.ViewModel.Items.Where(i => i.FolderID == item.FolderID && i.IsList == true).OrderBy(i => i.Name).ToObservableCollection();
                    lists.Insert(0, new Item()
                    {
                        ID = Guid.Empty,
                        Name = folder.Name
                    });
                    listPicker.ItemsSource = lists;
                    listPicker.DisplayMemberPath = "Name";
                    Item thisItem = lists.FirstOrDefault(i => i.ID == item.ParentID);
                    listPicker.SelectedIndex = lists.IndexOf(thisItem);
                    listPicker.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(container, lists[listPicker.SelectedIndex].ID, null); });
                    listPicker.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(listPicker);
                    break;
                    */
                case "Integer":
                    entryElement.Value = (string) currentValue;
                    entryElement.KeyboardType = UIKeyboardType.NumberPad;
					entryElement.Changed += delegate { pi.SetValue(container, entryElement.Value, null); };
                    break;
                case "Date":
					DateEventElement dateElement = new DateEventElement(field.DisplayName, Convert.ToDateTime((string) currentValue));
					dateElement.ValueSelected += delegate 
                    {
                        //pi.SetValue(container, dp.Value, null);
                        pi.SetValue(container, ((DateTime)dateElement.DateValue).ToString("d"), null);
                        folder.NotifyPropertyChanged("FirstDue");
                        folder.NotifyPropertyChanged("FirstDueColor");
                    };
					element = dateElement;
                    break;
                case "Boolean":
				/*
					BooleanImageElement boolElement = new BooleanImageElement(field.DisplayName, (bool) currentValue, 
				    	new UIImage("Images/first.png"), new UIImage("Images/second.png"));
					boolElement.ValueChanged += delegate { pi.SetValue(container, boolElement.Value, null); };
					element = boolElement;
					*/
					CheckboxElement checkboxElement = new CheckboxElement(field.DisplayName, (bool) currentValue);
					checkboxElement.Tapped += delegate { pi.SetValue(container, checkboxElement.Value, null); };
					element = checkboxElement;
                    break;
				/*
                case "TagList":
                    TextBox taglist = new TextBox() { MinWidth = minWidth, IsTabStop = true };
                    taglist.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    taglist.TabIndex = tabIndex++;
                    RenderEditItemTagList(taglist, (Item) container, pi);
                    EditStackPanel.Children.Add(taglist);
                    break;
                case "ListPointer":
                    innerPanel = RenderEditFolderPointer(pi, minWidth);
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                    */
                default:
                    notMatched = true;
                    break;
            }
			
			/*
            // if wasn't able to match field type by display type, try matching by CLR type
            if (notMatched == true)
            {
                string typename = GetTypeName(pi);
                switch (typename)
                {
                    case "String":
                        tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                        tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, tb.Text, null); });
                        tb.TabIndex = tabIndex++;
                        tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                        EditStackPanel.Children.Add(tb);
                        break;
                    case "Int32":
                        tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Digits } } };
                        tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(container, Convert.ToInt32(tb.Text), null); });
                        tb.TabIndex = tabIndex++;
                        tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                        EditStackPanel.Children.Add(tb);
                        break;
                    case "DateTime":
                        DatePicker dp = new DatePicker() { DataContext = container, MinWidth = minWidth, IsTabStop = true };
                        dp.SetBinding(DatePicker.ValueProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                        dp.ValueChanged += new EventHandler<DateTimeValueChangedEventArgs>(delegate
                        {
                            pi.SetValue(container, dp.Value, null);
                            folder.NotifyPropertyChanged("FirstDue");
                            folder.NotifyPropertyChanged("FirstDueColor");
                        });
                        dp.TabIndex = tabIndex++;
                        EditStackPanel.Children.Add(dp);
                        break;
                    case "Boolean":
                        CheckBox cb = new CheckBox() { DataContext = container, IsTabStop = true };
                        cb.SetBinding(CheckBox.IsEnabledProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                        cb.TabIndex = tabIndex++;
                        EditStackPanel.Children.Add(cb);
                        break;
                    default:
                        break;
                }
            }

            // add the listboxitem to the listbox
            EditListBox.Items.Add(listBoxItem);
            */
			
			return element;
        }
        
        private Section RenderEditItemFields(Item item, ItemType itemtype, bool primary, bool renderListField)
        {
            Section section = new Section(primary ? "" : "Other");
			
			if (renderListField == true)
            {
                //FieldType fieldType = new FieldType() { Name = "FolderID", DisplayName = "folder", DisplayType = "Folder" };
                Field field = new Field() { Name = "ParentID", DisplayName = "list", DisplayType = "List" };
                section.Add(RenderEditItemField(item, field));
            }

            // render fields
            foreach (Field f in itemtype.Fields.Where(f => f.IsPrimary == primary).OrderBy(f => f.SortOrder))
                section.Add(RenderEditItemField(item, f));
			
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
			
            // render fields
            foreach (ActionType action in App.ViewModel.Constants.ActionTypes.OrderBy(a => a.SortOrder))
            {
                FieldValue fieldValue = null;
                
                // find out if the property exists on the current item
                try
                {
                    Field field = itemType.Fields.Single(f => f.Name == action.FieldName);
                    fieldValue = item.FieldValues.Single(fv => fv.FieldID == field.ID);
                }
                catch (Exception)
                {
                    // we can't do anything with this field since we don't have it on the local type
                    // but that's ok - we can keep going
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
                        case "Navigate":
						    try
                            {
                                Folder f = App.ViewModel.Folders.Single(t => t.ID == Guid.Parse(currentValue));
                                stringElement.Value = String.Format("to {0}", f.Name);
                                stringElement.Tapped += delegate 
                                {
                                    // trace page navigation
                                    TraceHelper.StartMessage("Item: Navigate to Folder");

                                    // Navigate to the new page
                                    //NavigationService.Navigate(new Uri("/ListPage.xaml?type=Folder&ID=" + f.ID.ToString(), UriKind.Relative));
                                };
                            }
                            catch (Exception)
                            {
                                stringElement.Value = "(folder not found)";
                            }
                            break;
                        case "Postpone":
                            stringElement.Value = "to tomorrow";
                            stringElement.Tapped += delegate
                            {
                                item.DueDate = DateTime.Today.Date.AddDays(1.0).ToString("yyyy-MM-dd");
                                folder.NotifyPropertyChanged("FirstDue");
                                folder.NotifyPropertyChanged("FirstDueColor");
                            };
                            break;
                        case "AddToCalendar":
                            stringElement.Value = item.DueDisplay;
                            stringElement.Tapped += delegate
                            {
                                folder.NotifyPropertyChanged("FirstDue");
                                folder.NotifyPropertyChanged("FirstDueColor");
                            };
                            break;
                        case "Map":
                            stringElement.Tapped += delegate
                            {
                            };
                            break;
                        case "Phone":
                            stringElement.Tapped += delegate
                            {
                            };
                            break;
                        case "TextMessage":
                            stringElement.Tapped += delegate
                            {
                            };
                            break;
                        case "Browse":
                            string url = (string)currentValue;
                            if (url.Substring(0, 4) != "http")
                                url = String.Format("http://{0}", url);
                            var browserElement = new StyledHtmlElement(action.DisplayName, url, UITableViewCellStyle.Value1, url);
							element = browserElement;
                            break;
                        case "Email":
                            stringElement.Tapped += delegate
                            {
                                // To = (string)currentValue 
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

        #endregion
	}
}

