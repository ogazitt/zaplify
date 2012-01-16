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
using BuiltSteady.Zaplify.Devices.Utilities;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public class FolderHelper
    {
        private const int rendersize = 10;  // limit of elements to render immediately

        // local state initialized by constructor
        private Folder folder;
        private RoutedEventHandler checkBoxClickEvent;
        private RoutedEventHandler tagClickEvent;

        public FolderHelper(Folder folder, RoutedEventHandler checkBoxClickEvent, RoutedEventHandler tagClickEvent)
        {
            this.folder = folder;
            this.checkBoxClickEvent = checkBoxClickEvent;
            this.tagClickEvent = tagClickEvent;
        }

        // local state which can be set by the caller
        public string OrderBy { get; set; }
        public ListBox ListBox { get; set; }

        /// <summary>
        /// Add a new item to the Items collection and the ListBox
        /// </summary>
        /// <param name="tl">Folder to add to</param>
        /// <param name="item">Item to add</param>
        public void AddItem(Folder tl, Item item)
        {
            // add the item
            tl.Items.Add(item);

            tl.Items = OrderItems(tl.Items);

            // get the correct index based on the current sort
            int newIndex = tl.Items.IndexOf(item);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderItem(item));
        }

        /// <summary>
        /// Render a folder (in lieu of databinding)
        /// </summary>
        /// <param name="tl">Folder to render</param>
        public void RenderList(Folder tl)
        {
            // if the folder is null, nothing to do
            if (tl == null)
                return;

            // trace the event
            TraceHelper.AddMessage("List: RenderList");

            // order by correct fields
            tl.Items = OrderItems(tl.Items);

            /*
            // create the top-level grid
            FrameworkElement element;
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40d) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(element = new TextBlock() 
            { 
                Text = tl.Name, 
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
            if (tl.Items.Count <= rendersize)
            {
                // render the items
                foreach (Item t in tl.Items)
                    ListBox.Items.Add(RenderItem(t));
            }
            else
            {
                // render the first 10 items immediately
                foreach (Item t in tl.Items.Take(rendersize))
                    ListBox.Items.Add(RenderItem(t));

                // schedule the rendering of the rest of the items on the UI thread
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (Item t in tl.Items.Skip(rendersize))
                        ListBox.Items.Add(RenderItem(t));
                });
            }

            // set the content for the pivot item (which will trigger the rendering)
            //((PivotItem)PivotControl.SelectedItem).Content = grid;

            // trace the event
            TraceHelper.AddMessage("Finished List RenderList");
        }

        /// <summary>
        /// Remove a item in the folder and the ListBox
        /// </summary>
        /// <param name="tl">Folder that the item belongs to</param>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(Folder tl, Item item)
        {
            // get the current index based on the current sort
            int currentIndex = tl.Items.IndexOf(item);

            // remove the item from the folder
            tl.Items.Remove(item);

            // remove the item's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);
        }

        /// <summary>
        /// ReOrder a item in the folder and the ListBox
        /// </summary>
        /// <param name="tl">Folder that the item belongs to</param>
        /// <param name="item">Item to reorder</param>
        public void ReOrderItem(Folder tl, Item item)
        {
            // get the current index based on the current sort
            int currentIndex = tl.Items.IndexOf(item);

            // order the folder by the correct fields
            tl.Items = OrderItems(tl.Items);

            // get the correct index based on the current sort
            int newIndex = tl.Items.IndexOf(item);

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
        /// Find a folder by ID and then return its index 
        /// </summary>
        /// <param name="observableCollection"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        private int IndexOf(ObservableCollection<Folder> folders, Folder folder)
        {
            try
            {
                Folder folderRef = folders.Single(tl => tl.ID == folder.ID);
                return folders.IndexOf(folderRef);
            }
            catch (Exception)
            {
                return -1;
            }
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
        /// Render a item into a ListBoxItem
        /// </summary>
        /// <param name="t">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItem(Item t)
        {
            FrameworkElement element;
            ListBoxItem listBoxItem = new ListBoxItem() { Tag = t };
            StackPanel sp = new StackPanel() { Margin = new Thickness(0, -5, 0, 0), Width = 432d };
            listBoxItem.Content = sp;

            // first line (priority icon, checkbox, name)
            Grid itemLineOne = new Grid();
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            itemLineOne.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemLineOne.Children.Add(element = new Image() { Source = new BitmapImage(new Uri(t.PriorityIDIcon, UriKind.Relative)), Margin = new Thickness(0, 2, 0, 0) });
            element.SetValue(Grid.ColumnProperty, 0);
            itemLineOne.Children.Add(element = new CheckBox() { IsChecked = t.Complete, Tag = t.ID });
            element.SetValue(Grid.ColumnProperty, 1);
            ((CheckBox)element).Click += new RoutedEventHandler(checkBoxClickEvent);
            itemLineOne.Children.Add(element = new TextBlock()
            {
                Text = t.Name,
                Style = (Style)App.Current.Resources["PhoneTextLargeStyle"],
                Foreground = new SolidColorBrush(GetDisplayColor(t.NameDisplayColor)),
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
                Text = t.DueDisplay,
                FontSize = (double)App.Current.Resources["PhoneFontSizeNormal"],
                Foreground = new SolidColorBrush(GetDisplayColor(t.DueDisplayColor)),
                Margin = new Thickness(32, -17, 0, 0)
            });
            element.SetValue(Grid.ColumnProperty, 0);

            // render tag panel
            if (t.Tags != null)
            {
                StackPanel tagStackPanel = new StackPanel()
                {
                    Margin = new Thickness(32, -17, 0, 0),
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                };
                tagStackPanel.SetValue(Grid.ColumnProperty, 1);
                foreach (var tag in t.Tags)
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
