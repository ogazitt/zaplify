using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using System.Reflection;
using TaskStoreClientEntities;
using System.Windows.Data;
using System.Runtime.Serialization;
using TaskStoreWinPhoneUtilities;
using System.Text.RegularExpressions;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Interactivity;
using WPKeyboardHelper; 
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Device.Location;
using System.ComponentModel;

namespace TaskStoreWinPhone
{
    public partial class TaskPage : PhoneApplicationPage
    {
        private Task thisTask;
        private Task taskCopy;
        private TaskList taskList;
        private ListType listType;
        private Button moreButton;
        private KeyboardHelper keyboardHelper;

        private bool isInitialized = false;
        private int tabIndex = 0;

        // Constructor
        public TaskPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Task: constructor");

            ConnectedIconImage.DataContext = App.ViewModel;

            this.IsTabStop = true;

            this.Loaded += new RoutedEventHandler(TaskPage_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(TaskPage_BackKeyPress);
        }

        // When page is navigated to set data context to selected item in listType
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("Task: OnNavigatedTo");

            // check to make sure we haven't initialized yet
            if (isInitialized == true)
                return;

            // create the keyboard helper for tabbed navigation
            this.keyboardHelper = new KeyboardHelper(LayoutRoot);

            // reset the tab index
            tabIndex = 0;

            // don't render the tasklist field by default
            bool renderTaskListField = false;

            // find the taskist that this task would belong to
            string taskListIDString = "";
            if (NavigationContext.QueryString.TryGetValue("taskListID", out taskListIDString))
            {
                Guid taskListID = new Guid(taskListIDString);
                if (taskListID != Guid.Empty)
                {
                    try
                    {
                        //taskList = App.ViewModel.TaskLists.Single(tl => tl.ID == taskListID);
                        taskList = App.ViewModel.LoadList(taskListID);
                    }
                    catch (Exception)
                    {
                        taskList = null;
                    }
                }
            }

            // if we haven't found a tasklist, use the default one
            if (taskList == null)
            {
                taskList = App.ViewModel.DefaultTaskList;
                renderTaskListField = true;
            }

            string taskIDString = "";
            // must have a task ID passed (either a valid GUID or "new")
            if (NavigationContext.QueryString.TryGetValue("ID", out taskIDString) == false)
            {
                // trace page navigation
                TraceHelper.StartMessage("Task: Navigate back");

                NavigationService.GoBack();
                return;
            }

            // the task page is used to construct a new item
            if (taskIDString == "new")
            {
                // remove the "actions" tab
                //TaskPagePivotControl.Items.RemoveAt(0);
                //((PivotItem)(TaskPagePivotControl.Items[0])).IsEnabled = false;
                taskCopy = new Task() { TaskListID = taskList.ID };
                thisTask = null;
                RenderViewTask(taskCopy); 
                RenderEditTask(taskCopy, renderTaskListField);

                // navigate the pivot control to the "edit" view
                TaskPagePivotControl.SelectedIndex = 1;
            }
            else 
            {
                // editing an existing item
                Guid id = new Guid(taskIDString);
                //thisTask = App.ViewModel.Tasks.Single(t => t.ID == id);
                //taskList = App.ViewModel.TaskLists.Single(tl => tl.ID == thisTask.TaskListID);
                thisTask = taskList.Tasks.Single(t => t.ID == id);

                // make a deep copy of the task for local binding
                taskCopy = new Task(thisTask);
                DataContext = taskCopy;
                RenderViewTask(taskCopy);
                RenderEditTask(taskCopy, false);
            }
                    
            // set the initialized flag
            isInitialized = true;
        }

        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Task: Navigate back");

            // Navigate back to the tastlist page
            NavigationService.GoBack();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new task, delete just does the same thing as cancel
            if (thisTask == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this task?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = taskCopy
                });

            // remove the task from the local listType
            taskList.Tasks.Remove(thisTask);

            // save the changes to local storage
            //StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);
            StorageHelper.WriteList(taskList);

            // trigger a databinding refresh for tasks
            //App.ViewModel.NotifyPropertyChanged("Tasks");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("Task: Navigate back");

            // Navigate back to the tasklist page
            NavigationService.GoBack();
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            // remove the more button
            EditListBox.Items.Remove(moreButton);

            // render the non-primary fields
            RenderEditTaskFields(taskCopy, listType, false, false);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Hack: need to change the focus to the parent page so as to invoke all the LostFocus handlers 
            // and get all the data written back to the fields
            this.Focus();

            // schedule the Save click implementation on the UI thread
            Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SaveButton_Click_Implementation), sender, e);
        }
            
        private void SaveButton_Click_Implementation(object sender, EventArgs e)
        {
            // update the LastModified timestamp
            taskCopy.LastModified = DateTime.UtcNow;

            ParseFields(taskCopy);

            // if this is a new task, create it
            if (thisTask == null)
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = taskCopy
                        });

                // add the task to the local listType
                taskList.Tasks.Add(taskCopy);
                thisTask = taskCopy;
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Task>() { thisTask, taskCopy },
                        BodyTypeName = "Task",
                        ID = thisTask.ID
                    });

                // save the changes to the existing task
                int index = IndexOf(taskList, thisTask);
                if (index < 0)
                    return; 
                taskList.Tasks[index] = taskCopy;
                thisTask = taskCopy;
            }
            
            // save the changes to local storage
            //StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);
            StorageHelper.WriteList(taskList);

            // trigger a databinding refresh for tasks
            //App.ViewModel.NotifyPropertyChanged("Tasks");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // signal the tasklist that the FirstDue property needs to be recomputed
            taskList.NotifyPropertyChanged("FirstDue");

            // trace page navigation
            TraceHelper.StartMessage("Task: Navigate back");

            // Navigate back to the tasklist page
            NavigationService.GoBack();
        }

        void TaskPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Task: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void TaskPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("Task: Loaded");
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                keyboardHelper.HandleReturnKey();
            }
        }

        #endregion

        #region Helpers

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
        /// Find a task by ID and then return its index 
        /// </summary>
        /// <param name="observableCollection"></param>
        /// <param name="taskList"></param>
        /// <returns></returns>
        private int IndexOf(TaskList tasklist, Task task)
        {
            try
            {
                Task taskRef = tasklist.Tasks.Single(t => t.ID == task.ID);
                return tasklist.Tasks.IndexOf(taskRef);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void ParseFields(Task task)
        {
            string text = task.Description;
            if (text == null || text == "")
                return;

            Match m;

            // parse the text for a phone number
            m = Regex.Match(text, @"(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                task.Phone = m.Value;

            // parse the text for an email address
            m = Regex.Match(text, @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[A-Z]{2}|com|org|net|edu|gov|mil|biz|info|mobi|name|aero|asia|jobs|museum)\b", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                task.Email = m.Value;

            // parse the text for a website
            m = Regex.Match(text, @"((http|https)(:\/\/))?([a-zA-Z0-9]+[.]{1}){2}[a-zA-z0-9]+(\/{1}[a-zA-Z0-9]+)*\/?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                task.Website = m.Value;           
        }

        private void RenderEditTask(Task task, bool renderTaskListField)
        {
            // get listType for this list
            try
            {
                listType = App.ViewModel.ListTypes.Single(lt => lt.ID == taskList.ListTypeID);
            }
            catch (Exception)
            {
                // if can't find the list type, use the first
                listType = App.ViewModel.ListTypes[0];
            }

            // render the primary fields
            RenderEditTaskFields(task, listType, true, renderTaskListField);

            // render more button
            moreButton = new Button() { Content = "more details" };
            moreButton.Click += new RoutedEventHandler(MoreButton_Click);
            EditListBox.Items.Add(moreButton);
        }

        private void RenderEditTaskField(Task task, FieldType fieldType)
        {
            PropertyInfo pi;

            // make sure the property exists on the local type
            try
            {
                pi = task.GetType().GetProperty(fieldType.Name);
                if (pi == null)
                    return;  // see comment below
            }
            catch (Exception)
            {
                // we can't do anything with this property since we don't have it on the local type
                // this indicates that the phone software isn't caught up with the service version
                return;
            }

            // get the value of the property
            var val = pi.GetValue(task, null);

            ListBoxItem listBoxItem = new ListBoxItem();
            StackPanel EditStackPanel = new StackPanel();
            listBoxItem.Content = EditStackPanel;
            EditStackPanel.Children.Add(
                new TextBlock()
                {
                    Text = fieldType.DisplayName,
                    Style = (Style)App.Current.Resources["PhoneTextNormalStyle"]
                });

            // create a textbox (will be used by the majority of field types)
            double minWidth = App.Current.RootVisual.RenderSize.Width;
            if ((int)minWidth == 0)
                minWidth = ((this.Orientation & PageOrientation.Portrait) == PageOrientation.Portrait) ? 480.0 : 800.0;

            TextBox tb = new TextBox() { DataContext = taskCopy, MinWidth = minWidth, IsTabStop = true };
            tb.SetBinding(TextBox.TextProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });

            bool notMatched = false;
            // render the right control based on the type 
            switch (fieldType.DisplayType)
            {
                case "String":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    EditStackPanel.Children.Add(tb);
                    break;
                case "TextBox":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                    tb.AcceptsReturn = true;
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Height = 300;
                    tb.TabIndex = tabIndex++;
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                    EditStackPanel.Children.Add(tb);
                    break;
                case "Phone":
                case "PhoneNumber":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.TelephoneNumber } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    StackPanel innerPanel = RenderEditTaskImageButtonPanel(tb);
                    ImageButton imageButton = (ImageButton)innerPanel.Children[1];
                    imageButton.Click += new RoutedEventHandler(delegate
                    {
                        PhoneNumberChooserTask chooser = new PhoneNumberChooserTask();
                        chooser.Completed += new EventHandler<PhoneNumberResult>((sender, e) =>
                        {
                            if (e.TaskResult == TaskResult.OK && e.PhoneNumber != null && e.PhoneNumber != "")
                                pi.SetValue(task, e.PhoneNumber, null);
                        });
                        chooser.Show();
                    });
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                case "Website":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Url } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    EditStackPanel.Children.Add(tb);
                    break;
                case "Email":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.EmailSmtpAddress } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    innerPanel = RenderEditTaskImageButtonPanel(tb);
                    imageButton = (ImageButton)innerPanel.Children[1];
                    imageButton.Click += new RoutedEventHandler(delegate
                    {
                        EmailAddressChooserTask chooser = new EmailAddressChooserTask();
                        chooser.Completed += new EventHandler<EmailResult>((sender, e) =>
                        {
                            if (e.TaskResult == TaskResult.OK && e.Email != null && e.Email != "")
                                pi.SetValue(task, e.Email, null);
                        });
                        chooser.Show();
                    });
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                case "Location":
                case "Address":
                    tb.InputScope = new InputScope()
                    {
                        Names = 
                            { 
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressStreet },
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressCity },
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressStateOrProvince },
                                new InputScopeName() { NameValue = InputScopeNameValue.AddressCountryName },
                            }
                    };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    innerPanel = RenderEditTaskImageButtonPanel(tb);
                    imageButton = (ImageButton)innerPanel.Children[1];
                    imageButton.Click += new RoutedEventHandler(delegate
                    {
                        // start the location service
                        GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                        watcher.MovementThreshold = 20; // Use MovementThreshold to ignore noise in the signal.
                        watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>((sender, e) =>
                        {
                            if (e.Status == GeoPositionStatus.Ready)
                            {
                                // Use the Position property of the GeoCoordinateWatcher object to get the current location.
                                GeoCoordinate co = watcher.Position.Location;
                                tb.Text = co.Latitude.ToString("0.000") + "," + co.Longitude.ToString("0.000");
                                //Stop the Location Service to conserve battery power.
                                watcher.Stop();
                            }
                        });
                        watcher.Start();
                    });
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                case "Priority":
                    ListPicker lp = new ListPicker() 
                    { 
                        MinWidth = minWidth, 
                        FullModeItemTemplate = (DataTemplate) App.Current.Resources["FullListPickerTemplate"], 
                        IsTabStop = true 
                    };
                    lp.ItemsSource = App.ViewModel.Constants.Priorities;
                    lp.DisplayMemberPath = "Name";
                    int? lpval = (int?)pi.GetValue(taskCopy, null);
                    if (lpval != null)
                        lp.SelectedIndex = (int)lpval;
                    else
                        lp.SelectedIndex = 1;  // HACK: hardcode to "Normal" priority.  this should come from a table.
                    lp.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(taskCopy, lp.SelectedIndex == 1 ? (int?)null : lp.SelectedIndex, null); });
                    lp.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(lp);
                    break;
                case "TaskList":
                    ListPicker taskListPicker = new ListPicker() { MinWidth = minWidth, IsTabStop = true };
                    taskListPicker.ItemsSource = App.ViewModel.TaskLists;
                    taskListPicker.DisplayMemberPath = "Name";
                    TaskList tl = App.ViewModel.TaskLists.FirstOrDefault(list => list.ID == taskList.ID);
                    taskListPicker.SelectedIndex = App.ViewModel.TaskLists.IndexOf(tl);
                    taskListPicker.SelectionChanged += new SelectionChangedEventHandler(delegate { pi.SetValue(taskCopy, App.ViewModel.TaskLists[taskListPicker.SelectedIndex].ID, null); });
                    taskListPicker.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(taskListPicker);
                    break;
                case "Integer":
                    tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Digits } } };
                    tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, Convert.ToInt32(tb.Text), null); });
                    tb.TabIndex = tabIndex++;
                    tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    EditStackPanel.Children.Add(tb);
                    break;
                case "Date":
                    DatePicker dp = new DatePicker() { DataContext = taskCopy, MinWidth = minWidth, IsTabStop = true };
                    dp.SetBinding(DatePicker.ValueProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                    dp.ValueChanged += new EventHandler<DateTimeValueChangedEventArgs>(delegate
                    {
                        pi.SetValue(taskCopy, dp.Value, null);
                        taskList.NotifyPropertyChanged("FirstDue");
                        taskList.NotifyPropertyChanged("FirstDueColor");
                    });
                    dp.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(dp);
                    break;
                case "Boolean":
                    CheckBox cb = new CheckBox() { DataContext = taskCopy, IsTabStop = true };
                    cb.SetBinding(CheckBox.IsCheckedProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                    cb.TabIndex = tabIndex++;
                    EditStackPanel.Children.Add(cb);
                    break;
                case "TagList":
                    TextBox taglist = new TextBox() { MinWidth = minWidth, IsTabStop = true };
                    taglist.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                    taglist.TabIndex = tabIndex++;
                    RenderEditTaskTagList(taglist, taskCopy, pi);
                    EditStackPanel.Children.Add(taglist);
                    break;
                case "ListPointer":
                    innerPanel = RenderEditTaskListPointer(pi, minWidth);
                    EditStackPanel.Children.Add(innerPanel);
                    break;
                default:
                    notMatched = true;
                    break;
            }

            // if wasn't able to match field type by display type, try matching by CLR type
            if (notMatched == true)
            {
                string typename = GetTypeName(pi);
                switch (typename)
                {
                    case "String":
                        tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };
                        tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, tb.Text, null); });
                        tb.TabIndex = tabIndex++;
                        tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                        EditStackPanel.Children.Add(tb);
                        break;
                    case "Int32":
                        tb.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Digits } } };
                        tb.LostFocus += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, Convert.ToInt32(tb.Text), null); });
                        tb.TabIndex = tabIndex++;
                        tb.KeyUp += new KeyEventHandler(TextBox_KeyUp);
                        EditStackPanel.Children.Add(tb);
                        break;
                    case "DateTime":
                        DatePicker dp = new DatePicker() { DataContext = taskCopy, MinWidth = minWidth, IsTabStop = true };
                        dp.SetBinding(DatePicker.ValueProperty, new Binding(pi.Name) { Mode = BindingMode.TwoWay });
                        dp.ValueChanged += new EventHandler<DateTimeValueChangedEventArgs>(delegate
                        {
                            pi.SetValue(taskCopy, dp.Value, null);
                            taskList.NotifyPropertyChanged("FirstDue");
                            taskList.NotifyPropertyChanged("FirstDueColor");
                        });
                        dp.TabIndex = tabIndex++;
                        EditStackPanel.Children.Add(dp);
                        break;
                    case "Boolean":
                        CheckBox cb = new CheckBox() { DataContext = taskCopy, IsTabStop = true };
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
        }

        private void RenderEditTaskFields(Task task, ListType listtype, bool primary, bool renderTaskListField)
        {
            if (renderTaskListField == true)
            {
                FieldType fieldType = new FieldType() { Name = "TaskListID", DisplayName = "list", DisplayType = "TaskList" };
                RenderEditTaskField(task, fieldType);
            }

            // render fields
            foreach (Field f in listtype.Fields.Where(f => f.IsPrimary == primary).OrderBy(f => f.SortOrder))
            {
                FieldType fieldType;
                // get the field type for this field
                try
                {
                    fieldType = App.ViewModel.Constants.FieldTypes.Single(ft => ft.FieldTypeID == f.FieldTypeID);
                }
                catch (Exception)
                {
                    continue;
                }

                // render this field
                RenderEditTaskField(task, fieldType);
            }

            // refresh the keyboard tabstops
            keyboardHelper.RefreshTabbedControls(null);
        }

        private static StackPanel RenderEditTaskImageButtonPanel(TextBox tb)
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

        private StackPanel RenderEditTaskListPointer(PropertyInfo pi, double minWidth)
        {
            StackPanel innerPanel;
            innerPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
            CheckBox listcb = new CheckBox() { DataContext = taskCopy, IsTabStop = true };
            listcb.SetBinding(CheckBox.IsCheckedProperty, new Binding("LinkedTaskListIDBool"));
            listcb.TabIndex = tabIndex++;
            ListPicker listPicker = new ListPicker()
            {
                MinWidth = minWidth,
                FullModeItemTemplate = (DataTemplate)App.Current.Resources["FullListPickerTemplate"],
                DataContext = listcb
            };
            listPicker.SetBinding(ListPicker.IsEnabledProperty, new Binding("IsChecked"));
            listPicker.ItemsSource = App.ViewModel.TaskLists;
            listPicker.DisplayMemberPath = "Name";
            Guid? taskListID = (Guid?)pi.GetValue(taskCopy, null);
            if (taskListID != null)
            {
                try
                {
                    TaskList taskListVal = App.ViewModel.TaskLists.Single(t => t.ID == (Guid)taskListID);
                    if (taskListVal != null)
                        listPicker.SelectedIndex = App.ViewModel.TaskLists.IndexOf(taskListVal);
                }
                catch (Exception)
                {
                    listPicker.SelectedIndex = 0;
                }
            }
            else
                listPicker.SelectedIndex = 0;

            // set the event handlers for the checkbox and listpicker
            listcb.Unchecked += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, null, null); });
            listcb.Checked += new RoutedEventHandler(delegate { pi.SetValue(taskCopy, App.ViewModel.TaskLists[listPicker.SelectedIndex].ID, null); });
            listPicker.SelectionChanged += new SelectionChangedEventHandler(delegate
            {
                if (listcb.IsChecked == false)
                    pi.SetValue(taskCopy, null, null);
                else
                    pi.SetValue(taskCopy, App.ViewModel.TaskLists[listPicker.SelectedIndex].ID, null);
            });
            innerPanel.Children.Add(listcb);
            innerPanel.Children.Add(listPicker);
            return innerPanel;
        }

        private void RenderEditTaskTagList(TextBox taglist, Task task, PropertyInfo pi)
        {
            taglist.InputScope = new InputScope() { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } } };

            // build the comma delimited tag list for this task
            bool addDelimiter = false;
            StringBuilder sb = new StringBuilder();
            var tasktags = (IEnumerable<TaskTag>)pi.GetValue(task, null);
            if (tasktags != null)
            {
                foreach (TaskTag tt in tasktags)
                {
                    if (addDelimiter)
                        sb.Append(",");
                    Tag tag = App.ViewModel.Tags.Single(t => t.ID == tt.TagID);
                    sb.Append(tag.Name);
                    addDelimiter = true;
                }
                taglist.Text = sb.ToString();
            }

            // retrieve the tasktags for the task, creating new tags along the way
            taglist.LostFocus += new RoutedEventHandler(delegate
            {
                //ObservableCollection<TaskTag> existingTags = (ObservableCollection<TaskTag>)pi.GetValue(task, null);
                ObservableCollection<TaskTag> newTags = new ObservableCollection<TaskTag>();
                string[] tags = taglist.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tt in tags)
                {
                    string str = tt.Trim();
                    Tag tag;
                    try
                    {
                        tag = App.ViewModel.Tags.Single(t => t.Name == str);
                        newTags.Add(new TaskTag() { Name = str, TagID = tag.ID, TaskID = task.ID });
                    }
                    catch (Exception)
                    {
                        // this is a new tag that we need to create 
                        tag = new Tag() { Name = str };
                        newTags.Add(new TaskTag() { Name = str, TagID = tag.ID, TaskID = task.ID });

                        // enqueue the Web Request Record 
                        RequestQueue.EnqueueRequestRecord(
                            new RequestQueue.RequestRecord()
                            {
                                ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                                Body = new Tag(tag)
                            });

                        // add the tag to the tag list
                        App.ViewModel.Tags.Add(tag);

                        // save the changes to local storage
                        StorageHelper.WriteTags(App.ViewModel.Tags);
                    }
                }

                // store the new TaskTag collection in the task
                pi.SetValue(task, newTags, null);

                // create the mirror Tags collection in the task
                task.CreateTags(App.ViewModel.Tags);
            });
        }

        private void RenderViewTask(Task task)
        {
            int row = 0;
            // render fields
            foreach (TaskStoreClientEntities.Action action in App.ViewModel.Constants.Actions.OrderBy(a => a.SortOrder))
            {
                PropertyInfo pi;
                // make sure the property exists on the local type
                try
                {
                    pi = task.GetType().GetProperty(action.FieldName);
                    if (pi == null)
                        continue;  // see comment below
                }
                catch (Exception)
                {
                    // we can't do anything with this property since we don't have it on the local type
                    // this indicates that the phone software isn't caught up with the service version
                    // but that's ok - we can keep going
                    continue;
                }

                // get the value of the property
                var val = pi.GetValue(task, null);

                // for our purposes, an empty value is the same as null
                if (val != null && val.GetType() == typeof(String))
                    if ((string)val == "")
                        val = null;

                // render this property if it's not null/empty
                if (val != null)
                {
                    // first make sure that we do want to render (type-specific logic goes here)
                    switch (action.ActionType)
                    {
                        case "Postpone":
                            // if the date is already further in the future than today, omit adding this action
                            if (((DateTime)val).Date > DateTime.Today.Date)
                                continue;
                            break;
                    }

                    // add a new row
                    ViewGrid.RowDefinitions.Add(new RowDefinition() { MaxHeight = 72 });

                    string valueString = val.ToString();
                    Thickness margin = new Thickness(12, 20, 0, 0);  // bounding rectangle of padding

                    // create a new buton for the action (verb)
                    var button = new Button()
                    {
                        Content = action.DisplayName,
                        MinWidth = 200
                    };
                    button.SetValue(Grid.ColumnProperty, 0);
                    button.SetValue(Grid.RowProperty, row);
                    ViewGrid.Children.Add(button);
                    
                    // create a label which holds the noun the verb will act upon 
                    // usually extracted from the task field's contents
                    var valueTextBlock = new TextBlock()
                    {
                        DataContext = task,
                        Style = (Style)App.Current.Resources["PhoneTextNormalStyle"],
                        Margin = margin,
                    };

                    //value.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                    valueTextBlock.SetValue(Grid.ColumnProperty, 1);
                    valueTextBlock.SetValue(Grid.RowProperty, row++);
                    ViewGrid.Children.Add(valueTextBlock);

                    // render the action based on the action type
                    switch (action.ActionType)
                    {
                        case "Navigate":
                            try
                            {
                                TaskList tl = App.ViewModel.TaskLists.Single(t => t.ID == (Guid)val);
                                valueTextBlock.Text = String.Format("to {0}", tl.Name);
                                button.Click += new RoutedEventHandler(delegate
                                {
                                    // trace page navigation
                                    TraceHelper.StartMessage("Task: Navigate to TaskList");

                                    // Navigate to the new page
                                    //NavigationService.Navigate(new Uri("/TaskListPage.xaml?type=TaskList&ID=" + tl.ID.ToString(), UriKind.Relative));
                                    NavigationService.Navigate(new Uri("/ListPage.xaml?type=TaskList&ID=" + tl.ID.ToString(), UriKind.Relative));
                                });
                            }
                            catch (Exception)
                            {
                                valueTextBlock.Text = "(list not found)";
                            }
                            break;
                        case "Postpone":
                            valueTextBlock.Text = "to tomorrow";
                            button.Click += new RoutedEventHandler(delegate
                            {
                                pi.SetValue(task, DateTime.Today.Date.AddDays(1.0), null);
                                taskList.NotifyPropertyChanged("FirstDue");
                                taskList.NotifyPropertyChanged("FirstDueColor");
                            });
                            break;
                        case "AddToCalendar":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding("DueDisplay"));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                taskList.NotifyPropertyChanged("FirstDue");
                                taskList.NotifyPropertyChanged("FirstDueColor");
                            });
                            break;
                        case "Map":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                            button.Click += new RoutedEventHandler(delegate
                            {
#if WINPHONE7 // Pre-MANGO
                                string mapUrl = "maps:";
                                bool space = false;
                                foreach (string part in valueString.Split(' '))
                                {
                                    if (space == true)
                                        mapUrl += "%20";
                                    mapUrl += part;
                                    space = true;
                                }
                                WebBrowserTask mapTask = new WebBrowserTask() { Uri = new Uri(mapUrl) };
#else // MANGO
                                BingMapsTask mapTask = new BingMapsTask() { SearchTerm = valueString };
#endif
                                mapTask.Show();
                            });
                            break;
                        case "Phone":
                            // format as phone number
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                PhoneCallTask phoneCallTask = new PhoneCallTask() { PhoneNumber = (string)val };
                                phoneCallTask.Show();
                            });
                            break;
                        case "TextMessage":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                SmsComposeTask smsTask = new SmsComposeTask() { To = (string)val };
                                smsTask.Show();
                            });
                            break;
                        case "Browse":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                string url = (string)val;
                                if (url.Substring(1, 4) != "http")
                                    url = String.Format("http://{0}", url);
                                WebBrowserTask browserTask = new WebBrowserTask() { Uri = new Uri(url) };
                                browserTask.Show();
                            });
                            break;
                        case "Email":
                            valueTextBlock.SetBinding(TextBlock.TextProperty, new Binding(pi.Name));
                            button.Click += new RoutedEventHandler(delegate
                            {
                                EmailComposeTask emailTask = new EmailComposeTask() { To = (string)val };
                                emailTask.Show();
                            });
                            break;
                    }
                }
            }
        }

        #endregion
    }
}