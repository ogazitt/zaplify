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

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public class ScheduleHelper
    {
        private const int rendersize = 10;  // limit of elements to render immediately

        // local state initialized by constructor
        private Item list;

        public ScheduleHelper(Item list)
        {
            this.list = list;
        }

        // local state which can be set by the caller
        public ListBox ListBox { get; set; }

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
            TraceHelper.AddMessage("Schedule: RenderList");

            // order by correct fields
            itemlist.Items = itemlist.Items.OrderBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection(); 

            // clear the listbox
            ListBox.Items.Clear();

            // if the number of items is smaller than 10, render them all immediately
            string renderDate = null;
            if (itemlist.Items.Count <= rendersize)
            {
                // render the items
                foreach (Item item in itemlist.Items)
                    renderDate = Render(ListBox, item, renderDate);
            }
            else
            {
                // render the first 10 items immediately
                foreach (Item item in itemlist.Items.Take(rendersize))
                    renderDate = Render(ListBox, item, renderDate);
                
                // schedule the rendering of the rest of the items on the UI thread
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (Item item in itemlist.Items.Skip(rendersize))
                        renderDate = Render(ListBox, item, renderDate);
                });
            }

            // trace the event
            TraceHelper.AddMessage("Finished Schedule RenderList");
        }

        private string Render(ListBox listBox, Item item, string renderDate)
        {
            string currentDate = ((DateTime)item.Due).ToShortDateString();
            // if this is a new day, render the section header
            if (currentDate != renderDate)
                listBox.Items.Add(RenderDate(currentDate));
            listBox.Items.Add(RenderItem(item));
            return currentDate;
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

        private ListBoxItem RenderDate(string datestring)
        {
            ListBoxItem listBoxItem = new ListBoxItem();
            StackPanel sp = new StackPanel() { Margin = new Thickness(0, -5, 0, 0), Width = 432d };
            listBoxItem.Content = sp;
            sp.Children.Add(new TextBlock()
            {
                Text = datestring,
                Style = (Style)App.Current.Resources["PhoneTextAccentStyle"],
                FontSize = (double)App.Current.Resources["PhoneFontSizeExtraLarge"],
                FontFamily = (FontFamily)App.Current.Resources["PhoneFontFamilyLight"],
                Margin = new Thickness(0, 10, 0, 0)
            });
            return listBoxItem;
        }

        /// <summary>
        /// Render an item into a ListBoxItem
        /// </summary>
        /// <param name="item">Item to render</param>
        /// <returns>ListBoxItem corresponding to the Item</returns>
        private ListBoxItem RenderItem(Item item)
        {
            FrameworkElement element;
            ListBoxItem listBoxItem = new ListBoxItem() { Tag = item };
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(125d) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            // time
            grid.Children.Add(element = new TextBlock()
            {
                Text = ((DateTime)item.Due).ToShortTimeString(),
                Style = (Style)App.Current.Resources["PhoneTextLargeStyle"],
                Foreground = new SolidColorBrush(GetDisplayColor(item.NameDisplayColor)),
                Margin = new Thickness(0, 12, 0, 0)
            });
            element.SetValue(Grid.ColumnProperty, 0);
            // name
            grid.Children.Add(element = new TextBlock()
            {
                Text = item.Name,
                Style = (Style)App.Current.Resources["PhoneTextLargeStyle"],
                Foreground = new SolidColorBrush(GetDisplayColor(item.NameDisplayColor)),
                Margin = new Thickness(20, 12, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            });
            element.SetValue(Grid.ColumnProperty, 1);
            listBoxItem.Content = grid;

            // return the new ListBoxItem
            return listBoxItem;
        }

        #endregion
    }
}
