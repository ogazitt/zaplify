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
using TaskStoreClientEntities;
using System.Windows.Media.Imaging;
using System.Linq;
using TaskStoreWinPhoneUtilities;

namespace TaskStoreWinPhone
{
    public class TaskListHelper
    {
        private const int rendersize = 10;  // limit of elements to render immediately

        // local state initialized by constructor
        private TaskList taskList;
        private RoutedEventHandler checkBoxClickEvent;
        private RoutedEventHandler tagClickEvent;

        public TaskListHelper(TaskList taskList, RoutedEventHandler checkBoxClickEvent, RoutedEventHandler tagClickEvent)
        {
            this.taskList = taskList;
            this.checkBoxClickEvent = checkBoxClickEvent;
            this.tagClickEvent = tagClickEvent;
        }

        // local state which can be set by the caller
        public string OrderBy { get; set; }
        public ListBox ListBox { get; set; }

        /// <summary>
        /// Add a new task to the Tasks collection and the ListBox
        /// </summary>
        /// <param name="tl">TaskList to add to</param>
        /// <param name="task">Task to add</param>
        public void AddTask(TaskList tl, Task task)
        {
            // add the task
            tl.Tasks.Add(task);

            tl.Tasks = OrderTasks(tl.Tasks);

            // get the correct index based on the current sort
            int newIndex = tl.Tasks.IndexOf(task);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderTask(task));
        }

        /// <summary>
        /// Render a list (in lieu of databinding)
        /// </summary>
        /// <param name="tl">TaskList to render</param>
        public void RenderList(TaskList tl)
        {
            // if the tasklist is null, nothing to do
            if (tl == null)
                return;

            // trace the event
            TraceHelper.AddMessage("List: RenderList");

            // order by correct fields
            tl.Tasks = OrderTasks(tl.Tasks);

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

            // if the number of tasks is smaller than 10, render them all immediately
            if (tl.Tasks.Count <= rendersize)
            {
                // render the tasks
                foreach (Task t in tl.Tasks)
                    ListBox.Items.Add(RenderTask(t));
            }
            else
            {
                // render the first 10 tasks immediately
                foreach (Task t in tl.Tasks.Take(rendersize))
                    ListBox.Items.Add(RenderTask(t));

                // schedule the rendering of the rest of the tasks on the UI thread
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (Task t in tl.Tasks.Skip(rendersize))
                        ListBox.Items.Add(RenderTask(t));
                });
            }

            // set the content for the pivot item (which will trigger the rendering)
            //((PivotItem)PivotControl.SelectedItem).Content = grid;

            // trace the event
            TraceHelper.AddMessage("Finished List RenderList");
        }

        /// <summary>
        /// Remove a task in the list and the ListBox
        /// </summary>
        /// <param name="tl">TaskList that the task belongs to</param>
        /// <param name="task">Task to remove</param>
        public void RemoveTask(TaskList tl, Task task)
        {
            // get the current index based on the current sort
            int currentIndex = tl.Tasks.IndexOf(task);

            // remove the task from the tasklist
            tl.Tasks.Remove(task);

            // remove the task's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);
        }

        /// <summary>
        /// ReOrder a task in the list and the ListBox
        /// </summary>
        /// <param name="tl">TaskList that the task belongs to</param>
        /// <param name="task">Task to reorder</param>
        public void ReOrderTask(TaskList tl, Task task)
        {
            // get the current index based on the current sort
            int currentIndex = tl.Tasks.IndexOf(task);

            // order the list by the correct fields
            tl.Tasks = OrderTasks(tl.Tasks);

            // get the correct index based on the current sort
            int newIndex = tl.Tasks.IndexOf(task);

            // remove the task's ListBoxItem from the current place
            ListBox.Items.RemoveAt(currentIndex);

            // reinsert it at the correct place
            ListBox.Items.Insert(newIndex, RenderTask(task));
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
        /// Find a tasklist by ID and then return its index 
        /// </summary>
        /// <param name="observableCollection"></param>
        /// <param name="taskList"></param>
        /// <returns></returns>
        private int IndexOf(ObservableCollection<TaskList> lists, TaskList taskList)
        {
            try
            {
                TaskList taskListRef = lists.Single(tl => tl.ID == taskList.ID);
                return lists.IndexOf(taskListRef);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Order a collection of tasks by the right sort
        /// </summary>
        /// <param name="tasks">Collection of tasks</param>
        /// <returns>Ordered collection</returns>
        private ObservableCollection<Task> OrderTasks(ObservableCollection<Task> tasks)
        {
            // order the list by the correct fields
            switch (OrderBy)
            {
                case "due":
                    return tasks.OrderBy(t => t.Complete).ThenBy(t => t.DueSort).ThenBy(t => t.Name).ToObservableCollection();
                case "priority": // by pri
                    return tasks.OrderBy(t => t.Complete).ThenByDescending(t => t.PriorityIDSort).ThenBy(t => t.Name).ToObservableCollection();
                case "name": // by name
                    return tasks.OrderBy(t => t.Complete).ThenBy(t => t.Name).ToObservableCollection();
            }
            return null;
        }

        /// <summary>
        /// Render a task into a ListBoxItem
        /// </summary>
        /// <param name="t">Task to render</param>
        /// <returns>ListBoxItem corresponding to the Task</returns>
        private ListBoxItem RenderTask(Task t)
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
