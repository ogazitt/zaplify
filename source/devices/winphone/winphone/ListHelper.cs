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

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public class ListHelper
    {
        private const int rendersize = 10;  // limit of elements to render immediately

        // local state initialized by constructor
        private Item list;
        private RoutedEventHandler checkBoxClickEvent;
        private RoutedEventHandler tagClickEvent;

        public ListHelper(Item list, RoutedEventHandler checkBoxClickEvent, RoutedEventHandler tagClickEvent)
        {
            this.list = list;
            this.checkBoxClickEvent = checkBoxClickEvent;
            this.tagClickEvent = tagClickEvent;
        }

        // local state which can be set by the caller
        public string OrderBy { get; set; }
        public ListBox ListBox { get; set; }

        /// <summary>
        /// Add a new item to the Items collection and the ListBox
        /// </summary>
        /// <param name="itemlist">List to add to</param>
        /// <param name="item">Item to add</param>
        public void AddItem(Item itemlist, Item item)
        {
            // add the item to the list
            itemlist.Items.Add(item);

            itemlist.Items = OrderItems(itemlist.Items);

            // get the correct index based on the current sort
            int newIndex = itemlist.Items.IndexOf(item);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderItem(item));
        }

        /// <summary>
        /// Render a list (in lieu of databinding)
        /// </summary>
        /// <param name="itemlist">Item list to render</param>
        public void RenderList(Item itemlist)
        {
            // if the list is null, nothing to do
            if (itemlist == null)
                return;

            // trace the event
            TraceHelper.AddMessage("List: RenderList");

            // order by correct fields
            itemlist.Items = OrderItems(itemlist.Items);

            /*
            // create the top-level grid
            FrameworkElement element;
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40d) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(element = new TextBlock() 
            { 
                Text = folder.Name, 
                Margin = new Thickness(10, -28, 0, 10),
                Style = (Style)App.Current.Resources["PhoneTextAccentStyle"],
                FontSize = (double)App.Current.Resources["PhoneFontSizeExtraLarge"],
                FontFamily = (FontFamily)App.Current.Resources["PhoneFontFamilySemiLight"]
            });
            element.SetValue(Grid.RowProperty, 0);
            ListBox lb = new ListBox() { Margin = new Thickness(0, 0, 0, 0) };
            lb.SetValue(Grid.RowProperty, 1);
            lb.SelectionChanged += new SelectionChangedEventHandler(ListBox_SelectionChanged);
            grid.Children.Add(lb);
            */

            // clear the listbox
            ListBox.Items.Clear();

            // if the number of items is smaller than 10, render them all immediately
            if (itemlist.Items.Count <= rendersize)
            {
                // render the items
                foreach (Item t in itemlist.Items)
                    ListBox.Items.Add(RenderItem(t));
            }
            else
            {
                // render the first 10 items immediately
                foreach (Item t in itemlist.Items.Take(rendersize))
                    ListBox.Items.Add(RenderItem(t));

                // schedule the rendering of the rest of the items on the UI thread
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (Item t in itemlist.Items.Skip(rendersize))
                        ListBox.Items.Add(RenderItem(t));
                });
            }

            // set the content for the pivot item (which will trigger the rendering)
            //((PivotItem)PivotControl.SelectedItem).Content = grid;

            // trace the event
            TraceHelper.AddMessage("Finished List RenderList");
        }

        /// <summary>
        /// Remove a item in the list and the ListBox
        /// </summary>
        /// <param name="itemlist">List that the item belongs to</param>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(Item itemlist, Item item)
        {
            // get the current index based on the current sort
            int currentIndex = itemlist.Items.IndexOf(item);

            // remove the item from the list
            itemlist.Items.Remove(item);

            // remove the item's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);
        }

        /// <summary>
        /// ReOrder a item in the list and the ListBox
        /// </summary>
        /// <param name="itemlist">List that the item belongs to</param>
        /// <param name="item">Item to reorder</param>
        public void ReOrderItem(Item itemlist, Item item)
        {
            // get the current index based on the current sort
            int currentIndex = itemlist.Items.IndexOf(item);

            // order the list by the correct fields
            itemlist.Items = OrderItems(itemlist.Items);

            // get the correct index based on the current sort
            int newIndex = itemlist.Items.IndexOf(item);

            // remove the item's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderItem(item));
        }

        #region Helpers

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
        private ObservableCollection<Item> OrderItems(ObservableCollection<Item> items)
        {
            // order the folder by the correct fields
            switch (OrderBy)
            {
                case "due":
                    return items.OrderBy(t => t.Complete).ThenBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection();
                case "priority": // by pri
                    return items.OrderBy(t => t.Complete).ThenByDescending(t => t.PriorityIDSort).ThenBy(t => t.Name).ToObservableCollection();
                case "name": // by name
                    return items.OrderBy(t => t.Complete).ThenBy(t => t.Name).ToObservableCollection();
            }
            return null;
        }

        /// <summary>
        /// Render an item into a ListBoxItem
        /// </summary>
        /// <param name="i">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItem(Item i)
        {
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
            itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(item.PriorityIDIcon, UriKind.Relative)), Margin = new Thickness(0, 2, 0, 0) });
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
            itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(item.PriorityIDIcon, UriKind.Relative)), Margin = new Thickness(0, 2, 0, 0) });
            element.SetValue(Grid.ColumnProperty, 0);
            // if the icon string is empty, render a checkbox
            if (icon == null)
            {
                itemLineOne.Children.Add(element = new CheckBox() { IsChecked = (item.Complete == null ? false : item.Complete), Tag = item.ID });
                element.SetValue(Grid.ColumnProperty, 1);
                ((CheckBox)element).Click += new RoutedEventHandler(checkBoxClickEvent);
            }
            else
            {
                if (icon.StartsWith("/Images/") == false && icon.StartsWith("http://") == false && icon.StartsWith("www.") == false)
                    icon = "/Images/" + icon;
                //var margin = new Thickness(5, 11, 16, 8);
                //var sz = 48;
                itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(icon, UriKind.RelativeOrAbsolute)), Width = 48, Height = 48, Margin = new Thickness(5, 11, 16, 8) });
                //itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(icon, UriKind.RelativeOrAbsolute)), Width = sz, Height = sz, Margin = margin });
                element.SetValue(Grid.ColumnProperty, 1);
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
                        Foreground = new SolidColorBrush(GetDisplayColor(tag.Color)),
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
