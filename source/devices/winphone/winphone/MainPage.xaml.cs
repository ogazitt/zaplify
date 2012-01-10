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
using System.Reflection;
using System.Windows.Data;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using TaskStoreClientEntities;
using TaskStoreWinPhoneUtilities;
using System.Xml.Linq;
using System.Windows.Resources;
using System.IO;
using Microsoft.Phone.Tasks;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;
using System.Collections.ObjectModel;

namespace TaskStoreWinPhone
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private bool addedTasksPropertyChangedHandler = false;
        private bool initialSync = false;
        TaskList taskList;
        TaskListHelper TaskListHelper;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Main: constructor");

            // Set the data context of the page to the main view model
            DataContext = App.ViewModel;

            // set the data context of the search header to this page
            SearchHeader.DataContext = this;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private string searchTerm;
        /// <summary>
        /// Search Term to filter task collection on
        /// </summary>
        /// <returns></returns>
        public string SearchTerm
        {
            get
            {
                return searchTerm == null ? null : String.Format("search results on {0}", searchTerm);
            }
            set
            {
                if (value != searchTerm)
                {
                    searchTerm = value;
                    NotifyPropertyChanged("SearchTerm");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
                // do the below instead to avoid Invalid cross-thread access exception
                //Deployment.Current.Dispatcher.BeginInvoke(() => { handler(this, new PropertyChangedEventArgs(propertyName)); });
            }
        }

        #region Event handlers

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to About");

            // Navigate to the about page
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void DebugMenuItem_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Debug");

            // Navigate to the about page
            NavigationService.Navigate(new Uri("/DebugPage.xaml", UriKind.Relative));
        }

        private void EmailMenuItem_Click(object sender, EventArgs e)
        {
            // create email body
            StringBuilder sb = new StringBuilder("TaskStore Data:\n\n");
            foreach (TaskList tl in App.ViewModel.TaskLists)
            {
                sb.AppendLine(tl.Name);

                ListType listType;
                // get listType for this list
                try
                {
                    listType = App.ViewModel.ListTypes.Single(lt => lt.ID == tl.ListTypeID);
                }
                catch (Exception)
                {
                    // if can't find the list type, use the first
                    listType = App.ViewModel.ListTypes[0];
                }

                foreach (Task task in tl.Tasks)
                {
                    sb.AppendLine("    " + task.Name);
                    foreach (Field f in listType.Fields.OrderBy(f => f.SortOrder))
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

                        // already printed out the task name
                        if (fieldType.DisplayName == "Name")
                            continue;

                        PropertyInfo pi;
                        // make sure the property exists on the local type
                        try
                        {
                            pi = task.GetType().GetProperty(fieldType.Name);
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

                        // skip the uninteresting fields
                        if (pi.CanWrite == false ||
                            pi.PropertyType == typeof(Guid) ||
                            pi.PropertyType == typeof(Guid?) || 
                            pi.Name == "TaskTags" ||
                            pi.Name == "Created" ||
                            pi.Name == "LastModified")
                            continue;

                        // get the value of the property
                        var val = pi.GetValue(task, null);
                        if (val != null)
                        {
                            switch (pi.Name)
                            {
                                case "Due":
                                    sb.AppendFormat("        {0}: {1}\n", pi.Name, ((DateTime)val).ToString("d"));
                                    break;
                                case "PriorityID":
                                    sb.AppendFormat("        {0}: {1}\n", "Priority", Task.PriorityNames[(int)val]);
                                    break;
                                default:
                                    sb.AppendFormat("        {0}: {1}\n", pi.Name, val.ToString());
                                    break;
                            }
                        }
                    }
                }
            }

            EmailComposeTask emailComposeTask = new EmailComposeTask();
            emailComposeTask.Subject = "TaskStore Data";
            emailComposeTask.Body = sb.ToString();
            emailComposeTask.Show();
        }

        private void EraseMenuItem_Click(object sender, EventArgs e)
        {
            // confirm the delete and return if the user cancels
            MessageBoxResult result = MessageBox.Show(
                "are you sure you want to erase all data on the phone?  unless you paired the phone to an account, your data will be not be retrievable.",
                "confirm erasing all data",
                MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            foreach (var tl in App.ViewModel.TaskLists)
                StorageHelper.DeleteList(tl);
            StorageHelper.WriteConstants(null);
            StorageHelper.WriteDefaultTaskListID(null);
            StorageHelper.WriteListTypes(null);
            StorageHelper.WriteTags(null);
            StorageHelper.WriteTaskLists(null);
            StorageHelper.WriteUserCredentials(null);
            RequestQueue.DeleteQueue();
        }

        // Handle selection changed on ListBox
        private void ListsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (ListsListBox.SelectedIndex == -1)
                return;

            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to List");

            TaskList tasklist = App.ViewModel.TaskLists[ListsListBox.SelectedIndex];
            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/TaskListPage.xaml?type=TaskList&ID=" + tasklist.ID.ToString(), UriKind.Relative));
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=TaskList&ID=" + tasklist.ID.ToString(), UriKind.Relative));

            // Reset selected index to -1 (no selection)
            ListsListBox.SelectedIndex = -1;
        }

        // Event handlers for Lists tab
        private void Lists_AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to ListEditor");

            // Navigate to the ListEditor page
            NavigationService.Navigate(
                new Uri("/TaskListEditor.xaml?ID=new",
                UriKind.Relative));
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            TraceHelper.AddMessage("Main: Loaded");
          
            // if data isn't loaded from storage yet, load the app data
            if (!App.ViewModel.IsDataLoaded)
            {
                // Load app data from local storage (user creds, about tab data, constants, list types, tasklists, etc)
                App.ViewModel.LoadData();
            }

            // if haven't synced with web service yet, try now
            if (initialSync == false)
            {
                // attempt to sync with the Service
                App.ViewModel.SyncWithService();

                initialSync = true;
            }

            taskList = new TaskList() { Tasks = FilterTasks(App.ViewModel.Tasks) };

            // create the TaskListHelper
            TaskListHelper = new TaskStoreWinPhone.TaskListHelper(
                taskList, 
                new RoutedEventHandler(Tasks_CompleteCheckbox_Click), 
                new RoutedEventHandler(Tag_HyperlinkButton_Click));

            // store the current listbox and ordering
            TaskListHelper.ListBox = TasksListBox;
            TaskListHelper.OrderBy = "due";

            // render the tasks
            TaskListHelper.RenderList(taskList);

            // add a property changed handler for the Tasks property
            if (!addedTasksPropertyChangedHandler)
            {
                App.ViewModel.PropertyChanged += new PropertyChangedEventHandler((s, args) =>
                {
                    // if the Tasks property was signaled, re-filter and re-render the tasks list
                    if (args.PropertyName == "Tasks")
                    {
                        taskList.Tasks = FilterTasks(App.ViewModel.Tasks);
                        TaskListHelper.RenderList(taskList);
                    }
                });
                addedTasksPropertyChangedHandler = true;
            }

            // set the datacontext
            SearchHeader.DataContext = this;

            // trace exit
            TraceHelper.AddMessage("Exiting Main Loaded");
        }

        // When page is navigated to, switch to the specified tab
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string tabString = "";
            // check for the optional "Tab" parameter
            if (NavigationContext.QueryString.TryGetValue("Tab", out tabString) == false)
            {
                return;
            }

            switch (tabString)
            {
                case "Tasks":
                    MainPivot.SelectedIndex = 0;  // switch to tasks tab
                    break;
                case "Lists":
                    MainPivot.SelectedIndex = 1;  // switch to lists tab
                    break;
                case "Tags":
                    MainPivot.SelectedIndex = 2;  // switch to tags tab
                    break;
                default:
                    break;
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // get a reference to the add button (always first) and remove any eventhandlers
            var AddButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            try
            {
                AddButton.Click -= new EventHandler(Tasks_AddButton_Click);
                AddButton.Click -= new EventHandler(Lists_AddButton_Click);
                AddButton.Click -= new EventHandler(Tags_AddButton_Click);

                // remove the last button (in case it was added)
                ApplicationBar.Buttons.RemoveAt(3);
            }
            catch (Exception)
            {
            }

            // do tab-specific processing (e.g. adding the right Add button handler)
            switch (MainPivot.SelectedIndex)
            {
                case 0: // tasks
                    AddButton.Click += new EventHandler(Tasks_AddButton_Click);
                    var searchButton = new ApplicationBarIconButton() 
                    { 
                        Text = "filter", 
                        IconUri = new Uri("/Images/appbar.feature.search.rest.png", UriKind.Relative) 
                    };
                    searchButton.Click += new EventHandler(Tasks_SearchButton_Click);                    
                    ApplicationBar.Buttons.Add(searchButton);
                    break;
                case 1: // lists
                    AddButton.Click += new EventHandler(Lists_AddButton_Click);
                    break;
                case 2: // tags
                    AddButton.Click += new EventHandler(Tags_AddButton_Click);
                    break;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.ViewModel.SyncWithService();
        }

        // Event handlers for search popup
        private void SearchPopup_SearchButton_Click(object sender, EventArgs e)
        {
            SearchTerm = SearchTextBox.Text;

            // reset the tasks collection and render the new list
            taskList.Tasks = FilterTasks(App.ViewModel.Tasks);
            TaskListHelper.RenderList(taskList);

            // close the popup
            SearchPopup.IsOpen = false;
        }

        private void SearchPopup_ClearButton_Click(object sender, EventArgs e)
        {
            SearchTerm = null;

            // reset the tasks collection and render the new list
            taskList.Tasks = FilterTasks(App.ViewModel.Tasks);
            TaskListHelper.RenderList(taskList);

            // close the popup
            SearchPopup.IsOpen = false;
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Settings");

            // Navigate to the settings page
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        // Event handlers for tags tab
        private void Tag_HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton button = (HyperlinkButton)e.OriginalSource;
            Guid tagID = (Guid)button.Tag;

            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to TaskList");

            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/TaskListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
        }

        private void Tags_AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to TagEditor");

            // Navigate to the ListEditor page
            NavigationService.Navigate(
                new Uri("/TagEditor.xaml?ID=new",
                UriKind.Relative));
        }

        private void TagsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (TagsListBox.SelectedIndex == -1)
                return;

            Tag tag = App.ViewModel.Tags[TagsListBox.SelectedIndex];

            // Trace the navigation and start a new timing
            TraceHelper.StartMessage("Navigating to TaskList");

            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/TaskListPage.xaml?type=Tag&ID=" + tag.ID.ToString(), UriKind.Relative));
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tag.ID.ToString(), UriKind.Relative));

            // Reset selected index to -1 (no selection)
            TagsListBox.SelectedIndex = -1;
        }

        // Event handlers for tasks tab
        private void Tasks_AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Task");

            // Navigate to the Task page
            NavigationService.Navigate(
                new Uri("/TaskPage.xaml?ID=new",
                UriKind.Relative));
        }

        /// <summary>
        /// Handle click event on Complete checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tasks_CompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)e.OriginalSource;
            Guid taskID = (Guid)cb.Tag;

            // get the task that was just updated, and ensure the Complete flag is in the correct state
            Task task = App.ViewModel.Tasks.Single<Task>(t => t.ID == taskID);

            // get a reference to the base list that this task belongs to
            TaskList tl = App.ViewModel.LoadList(task.TaskListID);

            // create a copy of that task
            Task taskCopy = new Task(task);

            // toggle the complete flag to reflect the checkbox click
            task.Complete = !task.Complete;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Task>() { taskCopy, task },
                    BodyTypeName = "Task",
                    ID = task.ID
                });

            // remove the task from the tasklist and ListBox (because it will now be complete)
            TaskListHelper.RemoveTask(taskList, task);

            // save the changes to local storage
            StorageHelper.WriteList(tl);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();
        }

        private void Tasks_SearchButton_Click(object sender, EventArgs e)
        {
            SearchPopup.IsOpen = true;
            SearchTextBox.Focus();
        }

        private void TasksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            // If selected index is -1 (no selection) do nothing
            if (listBox.SelectedIndex == -1)
                return;

            // get the task associated with this click
            Task task = null;

            // retrieve the current selection
            ListBoxItem item = listBox.SelectedItem as ListBoxItem;
            if (item != null)
                task = item.Tag as Task;

            // if there is no task, return without processing the event
            if (task == null)
                return;

            // trace page navigation
            TraceHelper.StartMessage("Main: Navigate to Task");

            // Navigate to the new page
            NavigationService.Navigate(
                new Uri(String.Format("/TaskPage.xaml?ID={0}&taskListID={1}", task.ID, task.TaskListID),
                UriKind.Relative));

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        #endregion 

        #region Helpers

        private ObservableCollection<Task> FilterTasks(ObservableCollection<Task> tasks)
        {
            ObservableCollection<Task> filteredTasks = new ObservableCollection<Task>();
            foreach (Task task in tasks)
            {
                // if the task is completed, don't list it
                if (task.Complete)
                    continue;
                
                // get the tasklist - if it's a template, this item doesn't qualify as a match
                TaskList taskList = App.ViewModel.TaskLists.Single(tl => tl.ID == task.TaskListID);
                if (taskList.Template == true)
                    continue;

                // if there is no search term present, add this task and continue
                if (searchTerm == null)
                {
                    filteredTasks.Add(task);
                    continue;
                }

                // search for the term in every non-null string field
                foreach (var pi in task.GetType().GetProperties())
                {
                    if (pi.PropertyType.Name == "String" && pi.CanWrite)
                    {
                        string stringVal = (string)pi.GetValue(task, null);
                        // perform case-insensitive comparison
                        if (stringVal != null && stringVal.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            filteredTasks.Add(task);
                            break;
                        }
                    }
                }
            }

            // return the filtered task collection
            return filteredTasks;
        }

        #endregion
    }
}