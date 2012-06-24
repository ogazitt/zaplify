using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Windows.Media.Imaging;
using System.Linq;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;
using System.Reflection;
using System.Text;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public class ListHelper
    {
        private const int rendersize = 10;  // limit of elements to render immediately

        // local state initialized by constructor
        private Item list;
        private RoutedEventHandler checkBoxClickEvent;
        private RoutedEventHandler tagClickEvent;

        public ListHelper(RoutedEventHandler checkBoxClickEvent, RoutedEventHandler tagClickEvent)
        {
            this.checkBoxClickEvent = checkBoxClickEvent;
            this.tagClickEvent = tagClickEvent;
        }

        // local state which can be set by the caller
        public string OrderBy { get; set; }
        public ListBox ListBox { get; set; }

        /// <summary>
        /// Add a new item to the Items collection and the ListBox
        /// </summary>
        /// <param name="item">Item to add</param>
        public void AddItem(Item itemlist, Item item)
        {
            // if this is a categorized sort, we need to rebuild the list completely
            if (Categorize())
            {
                RenderList(itemlist);
                return;
            }

            // add the item to the list
            list.Items.Add(item);

            list.Items = OrderItems();

            // get the correct index based on the current sort
            int newIndex = list.Items.IndexOf(item);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderItem(item));
        }

        public string GetAsText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(list.Name);
            sb.AppendLine();

            foreach (Item item in list.Items)
            {
                // render a separator (section heading)
                if (item.ItemTypeID == SystemItemTypes.System)
                {
                    sb.AppendLine(item.Name);
                    continue;
                }
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

            return sb.ToString();
        }

        /// <summary>
        /// Render a list (in lieu of databinding)
        /// </summary>
        /// <param name="itemlist">Item list to render</param>
        public void RenderList(Item itemlist)
        {
            // trace the event
            TraceHelper.AddMessage("List: RenderList");

            // if the list is null, nothing to do
            if (itemlist == null)
                return;

            // store a copy of the list, with a new Items collection that imposes the order on the items
            this.list = new Item(itemlist, false);
            foreach (var i in itemlist.Items)
                list.Items.Add(i);

            // order by correct fields
            list.Items = OrderItems();

            // clear the listbox
            ListBox.Items.Clear();

            // if the number of items is smaller than 10, render them all immediately
            if (list.Items.Count <= rendersize)
            {
                // render the items
                foreach (Item i in list.Items)
                    ListBox.Items.Add(RenderItem(i));
            }
            else
            {
                // render the first 10 items immediately
                foreach (Item i in list.Items.Take(rendersize))
                    ListBox.Items.Add(RenderItem(i));

                // schedule the rendering of the rest of the items on the UI thread
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (Item i in list.Items.Skip(rendersize))
                        ListBox.Items.Add(RenderItem(i));
                });
            }

            // trace the event
            TraceHelper.AddMessage("Finished List RenderList");
        }

        /// <summary>
        /// Remove a item in the list and the ListBox
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(Item itemlist, Item item)
        {
            // if this is a categorized sort, we need to rebuild the list completely
            if (Categorize())
            {
                RenderList(itemlist);
                return;
            }

            // get the current index based on the current sort
            int currentIndex = list.Items.IndexOf(item);

            // remove the item from the list
            list.Items.Remove(item);

            // remove the item's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);
        }

        /// <summary>
        /// ReOrder a item in the list and the ListBox
        /// </summary>
        /// <param name="item">Item to reorder</param>
        public void ReOrderItem(Item itemlist, Item item)
        {
            // if this is a categorized sort, we need to rebuild the list completely
            if (list == null || Categorize())
            {
                RenderList(itemlist);
                return;
            }

            // get the current index based on the current sort
            int currentIndex = list.Items.IndexOf(item);

            if (currentIndex == -1)
            {
                TraceHelper.AddMessage("ReOrderItem: Could not find item " + item.Name);
                return;
            }

            // order the list by the correct fields
            list.Items = OrderItems();

            // get the correct index based on the current sort
            int newIndex = list.Items.IndexOf(item);

            // remove the item's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderItem(item));
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

        /// <summary>
        /// Get System.Windows.Media.Colors from a string color name
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private System.Windows.Media.Color GetDisplayColor(string c)
        {
            switch (c)
            {
                case "White":
                    return Colors.White;
                case "Blue":
                    return Colors.Blue;
                case "Brown":
                    return Colors.Brown;
                case "Green":
                    return Colors.Green;
                case "Orange":
                    return Colors.Orange;
                case "Purple":
                    return Colors.Purple;
                case "Red":
                    return Colors.Red;
                case "Yellow":
                    return Colors.Yellow;
                case "Gray":
                    return Colors.Gray;
            }
            return Colors.White;
        }

        /// <summary>
        /// Order a collection of items by the right sort
        /// </summary>
        /// <param name="items">Collection of items</param>
        /// <returns>Ordered collection</returns>
        private ObservableCollection<Item> OrderItems()
        {
            // create a new collection without any system itemtypes (which are used for section headings)
            var sorted = new ObservableCollection<Item>();
            foreach (var i in list.Items)
                if (i.ItemTypeID != SystemItemTypes.System)
                    sorted.Add(i);
            
            // order the folder by the correct fields
            switch (OrderBy)
            {
                case FieldNames.DueDate:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Priority:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenByDescending(t => t.PrioritySort).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Name:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Address:
                    sorted = sorted.OrderBy(t => t.Address).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Phone:
                    sorted = sorted.OrderBy(t => t.Phone).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Email:
                    sorted = sorted.OrderBy(t => t.Email).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Complete:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case FieldNames.Category:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Category).ThenBy(t => t.Name).ToObservableCollection();
                    break;
                case null:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.SortOrder).ToObservableCollection();
                    break;
                default:
                    sorted = sorted.OrderBy(t => t.Complete).ThenBy(t => !t.IsList).ThenBy(t => t.Name).ToObservableCollection();
                    break;
            }

            // if we aren't categorizing then there is no need to create section headings
            if (!Categorize())
                return sorted;

            // insert separators for section headings
            string separator = null;
            var finalList = new ObservableCollection<Item>();
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
                    finalList.Add(new Item() { Name = currentSectionHeading, ItemTypeID = SystemItemTypes.System }); // System itemtype designates separator
                    separator = currentSectionHeading;
                }
                finalList.Add(item);
            }

            return finalList;
        }

        /// <summary>
        /// Render an item into a ListBoxItem
        /// </summary>
        /// <param name="i">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItem(Item i)
        {
            if (i.ItemTypeID == SystemItemTypes.System)
                return RenderItemAsSeparator(i);
            if (i.IsList)
                return RenderItemAsList(i);
            else
                return RenderItemAsSingleton(i);
        }

        /// <summary>
        /// Render a item which is itself a list into a ListBoxItem
        /// </summary>
        /// <param name="item">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItemAsList(Item item)
        {
            FrameworkElement element;
            ListBoxItem listBoxItem = new ListBoxItem() { Tag = item };
            StackPanel sp = new StackPanel() { Margin = new Thickness(0, -5, 0, 0), Width = 432d };
            listBoxItem.Content = sp;

            // first line (name, item count)
            Grid itemLineOne = new Grid();
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(item.PriorityIcon, UriKind.Relative)), Margin = new Thickness(0, 2, 0, 0) });
            element.SetValue(Grid.ColumnProperty, 0);  // this is a dummy element - will always be a blank png - added here to get the spacing right
            itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri("/Images/appbar.folder.rest.png", UriKind.Relative)), Width = 64, Height = 64, Margin = new Thickness(-3, 3, 0, 0) });
            element.SetValue(Grid.ColumnProperty, 1);
            itemLineOne.Children.Add(element = new TextBlock()
            {
                Text = item.Name,
                Style = (Style)App.Current.Resources["PhoneTextLargeStyle"],
                Foreground = new SolidColorBrush(GetDisplayColor(item.NameDisplayColor)),
                Margin = new Thickness(6, 10, 0, 0)
            });
            element.SetValue(Grid.ColumnProperty, 2);
            
            // number of items in the list
            Folder f = App.ViewModel.LoadFolder(item.FolderID);
            int count = f.Items.Where(i => i.ParentID == item.ID).Count();
            itemLineOne.Children.Add(element = new TextBlock()
            {
                Text = count.ToString(),
                Style = (Style)App.Current.Resources["PhoneTextAccentStyle"],
                FontSize = (double)App.Current.Resources["PhoneFontSizeLarge"],
                FontFamily = (FontFamily)App.Current.Resources["PhoneFontFamilyLight"],
                TextAlignment = TextAlignment.Right,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            });
            element.SetValue(Grid.ColumnProperty, 2);
            
            sp.Children.Add(itemLineOne);   
            
            // second line (first item due)
            element = new TextBlock()
            {
                Text = item.DueDisplay, /* FirstDue */
                Style = (Style)App.Current.Resources["PhoneTextSubtleStyle"],
                FontSize = (double)App.Current.Resources["PhoneFontSizeNormal"],
                Foreground = new SolidColorBrush(GetDisplayColor(item.DueDisplayColor)), /* FirstDueColor */
                Margin = new Thickness(12, -6, 12, 0)
            };
            sp.Children.Add(element);

            // return the new ListBoxItem
            return listBoxItem;
        }

        /// <summary>
        /// Render a separator into a ListBoxItem
        /// </summary>
        /// <param name="item">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItemAsSeparator(Item item)
        {
            ListBoxItem listBoxItem = new ListBoxItem() { Tag = item };
            listBoxItem.Content = new TextBlock()
            {
                Text = item.Name,
                Style = (Style)App.Current.Resources["PhoneTextAccentStyle"],
                FontSize = (double)App.Current.Resources["PhoneFontSizeLarge"],
                FontFamily = (FontFamily)App.Current.Resources["PhoneFontFamilyLight"],
                Margin = new Thickness(10, 10, 0, 0)
            };
            return listBoxItem;
        }

        /// <summary>
        /// Render a singleton item into a ListBoxItem
        /// </summary>
        /// <param name="item">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItemAsSingleton(Item item)
        {
            // get the icon for the itemtype 
            ItemType itemType = null;
            string icon = null;
            if (ItemType.ItemTypes.TryGetValue(item.ItemTypeID, out itemType))
                icon = itemType.Icon;

            FrameworkElement element;
            ListBoxItem listBoxItem = new ListBoxItem() { Tag = item };
            StackPanel sp = new StackPanel() { Margin = new Thickness(0, -5, 0, 0), Width = 432d };
            listBoxItem.Content = sp;

            // first line (priority icon, checkbox / icon, name)
            Grid itemLineOne = new Grid();
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(item.PriorityIcon, UriKind.Relative)), Margin = new Thickness(0, 2, 0, 0) });
            element.SetValue(Grid.ColumnProperty, 0);
            // render a checkbox if has Complete field
            if (itemType != null && itemType.HasField(FieldNames.Complete))
            {
                itemLineOne.Children.Add(element = new CheckBox() { IsChecked = (item.Complete == null ? false : item.Complete), Tag = item.ID });
                element.SetValue(Grid.ColumnProperty, 1);
                ((CheckBox)element).Click += new RoutedEventHandler(checkBoxClickEvent);
            }
            else
            {
                // render an icon if one exists
                if (icon != null)
                {
                    if (icon.StartsWith("/Images/") == false && icon.StartsWith("http") == false && icon.StartsWith("www.") == false)
                        icon = "/Images/" + icon;
                    itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(icon, UriKind.RelativeOrAbsolute)), Width = 48, Height = 48, Margin = new Thickness(5, 11, 16, 8) });
                    element.SetValue(Grid.ColumnProperty, 1);
                }
                // render a picture if one exists 
                // this picture will layer on top of the existing icon - in case the picture is unavailable (e.g. disconnected)
                var picFV = item.GetFieldValue(FieldNames.Picture, false);
                if (picFV != null && !String.IsNullOrEmpty(picFV.Value))
                {
                    icon = picFV.Value;
                    if (icon.StartsWith("/Images/") == false && icon.StartsWith("http") == false && icon.StartsWith("www.") == false)
                        icon = "/Images/" + icon;
                    itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(icon, UriKind.RelativeOrAbsolute)), Width = 48, Height = 48, Margin = new Thickness(5, 11, 16, 8) });
                    element.SetValue(Grid.ColumnProperty, 1);
                }
            }
            itemLineOne.Children.Add(element = new TextBlock()
            {
                Text = item.Name,
                Style = (Style)App.Current.Resources["PhoneTextLargeStyle"],
                Foreground = new SolidColorBrush(GetDisplayColor(item.NameDisplayColor)),
                Margin = new Thickness(0, 12, 0, 0)
            });
            element.SetValue(Grid.ColumnProperty, 2);
            sp.Children.Add(itemLineOne);

            // second line (duedate, tags)
            Grid itemLineTwo = new Grid();
            itemLineTwo.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineTwo.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemLineTwo.Children.Add(element = new TextBlock()
            {
                Text = item.DueDisplay,
                FontSize = (double)App.Current.Resources["PhoneFontSizeNormal"],
                Foreground = new SolidColorBrush(GetDisplayColor(item.DueDisplayColor)),
                Margin = new Thickness(32, -17, 0, 0)
            });
            element.SetValue(Grid.ColumnProperty, 0);

            // render tag panel
            if (item.Tags != null)
            {
                StackPanel tagStackPanel = new StackPanel()
                {
                    Margin = new Thickness(32, -17, 0, 0),
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                };
                tagStackPanel.SetValue(Grid.ColumnProperty, 1);
                foreach (var tag in item.Tags)
                {
                    HyperlinkButton button;
                    tagStackPanel.Children.Add(button = new HyperlinkButton()
                    {
                        ClickMode = ClickMode.Release,
                        Content = tag.Name,
                        FontSize = (double)App.Current.Resources["PhoneFontSizeNormal"],
                        Foreground = new SolidColorBrush(GetDisplayColor(App.ViewModel.Constants.LookupColor(tag.ColorID))),
                        Tag = tag.ID
                    });
                    button.Click += tagClickEvent;
                }
                itemLineTwo.Children.Add(tagStackPanel);
            }
            sp.Children.Add(itemLineTwo);

            // return the new ListBoxItem
            return listBoxItem;
        }

        #endregion
    }
}
