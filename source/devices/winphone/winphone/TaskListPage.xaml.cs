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
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using TaskStoreClientEntities;
using System.Collections.ObjectModel;
using TaskStoreWinPhoneUtilities;
using System.Windows.Data;
using System.ComponentModel;
using System.Threading;
using Microsoft.Xna.Framework.Audio;

namespace TaskStoreWinPhone
{
    public partial class TaskListPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private TaskList taskList;
        public TaskList TaskList
        {
            get
            {
                return taskList;
            }
            set
            {
                if (value != taskList)
                {
                    taskList = value;
                    NotifyPropertyChanged("TaskList");
                }
            }
        }
        private Tag tag;
        private bool disableListBoxSelectionChanged = true;
        private SpeechHelper.SpeechState speechState;
        
        private string speechDebugString = null;
        private DateTime speechStart;

        // ViewSource for the TaskList collection for Import Template (used for filtering out non-template lists)
        public CollectionViewSource ImportTemplateViewSource { get; set; }

        // property that controls the sort order for the ListBoxes
        public List<CollectionViewSource> OrderedSource { get; set; }

        private Visibility networkOperationInProgress = Visibility.Collapsed;
        /// <summary>
        /// Whether a network operation is in progress (yes == Visible / no == Collapsed)
        /// </summary>
        /// <returns></returns>
        public Visibility NetworkOperationInProgress
        {
            get
            {
                return networkOperationInProgress;
            }
            set
            {
                if (value != networkOperationInProgress)
                {
                    networkOperationInProgress = value;
                    NotifyPropertyChanged("NetworkOperationInProgress");
                }
            }
        }

        private bool speechButtonEnabled = false;
        /// <summary>
        /// Speech button enabled
        /// </summary>
        /// <returns></returns>
        public bool SpeechButtonEnabled
        {
            get
            {
                return speechButtonEnabled;
            }
            set
            {
                if (value != speechButtonEnabled)
                {
                    speechButtonEnabled = value;
                    NotifyPropertyChanged("SpeechButtonEnabled");
                }
            }
        }

        private string speechButtonText = "done";
        /// <summary>
        /// Speech button text
        /// </summary>
        /// <returns></returns>
        public string SpeechButtonText
        {
            get
            {
                return speechButtonText;
            }
            set
            {
                if (value != speechButtonText)
                {
                    speechButtonText = value;
                    NotifyPropertyChanged("SpeechButtonText");
                }
            }
        }

        private string speechCancelButtonText = "cancel";
        /// <summary>
        /// Speech cancel button text
        /// </summary>
        /// <returns></returns>
        public string SpeechCancelButtonText
        {
            get
            {
                return speechCancelButtonText;
            }
            set
            {
                if (value != speechCancelButtonText)
                {
                    speechCancelButtonText = value;
                    NotifyPropertyChanged("SpeechCancelButtonText");
                }
            }
        }

        private string speechLabelText = "initializing...";
        /// <summary>
        /// Speech button text
        /// </summary>
        /// <returns></returns>
        public string SpeechLabelText
        {
            get
            {
                return speechLabelText;
            }
            set
            {
                if (value != speechLabelText)
                {
                    speechLabelText = value;
                    NotifyPropertyChanged("SpeechLabelText");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                //handler(this, new PropertyChangedEventArgs(propertyName));
                // do the below instead to avoid Invalid cross-thread access exception
                Deployment.Current.Dispatcher.BeginInvoke(() => { handler(this, new PropertyChangedEventArgs(propertyName)); });
            }
        }

        // Constructor
        public TaskListPage()
        {
            InitializeComponent();

            // trace data
            TraceHelper.AddMessage("TaskList: constructor");

            // set some data context information
            ConnectedIconImage.DataContext = App.ViewModel;
            SpeechProgressBar.DataContext = App.ViewModel;

            // set some data context information for the speech UI
            SpeechPopup_SpeakButton.DataContext = this;
            SpeechPopup_CancelButton.DataContext = this;
            SpeechLabel.DataContext = this;

            OrderedSource = new List<CollectionViewSource>();

            // add the name viewsource (which is a sorted view of the tasklist by name)
            var nameViewSource = new CollectionViewSource();
            using (nameViewSource.DeferRefresh())
            {
                nameViewSource.SortDescriptions.Add(new SortDescription("Complete", ListSortDirection.Ascending));
                nameViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                OrderedSource.Add(nameViewSource);
                ByNameListBox.DataContext = this;
            }
            // add the due viewsource (which is a sorted view of the tasklist by due date)
            var dueViewSource = new CollectionViewSource();
            using (dueViewSource.DeferRefresh())
            {
                dueViewSource.SortDescriptions.Add(new SortDescription("Complete", ListSortDirection.Ascending));
                dueViewSource.SortDescriptions.Add(new SortDescription("DueSort", ListSortDirection.Ascending));
                dueViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                OrderedSource.Add(dueViewSource);
                ByDueListBox.DataContext = this;
            }
            // add the priority viewsource (which is a sorted view of the tasklist by priority)
            var priViewSource = new CollectionViewSource();
            using (priViewSource.DeferRefresh())
            {
                priViewSource.SortDescriptions.Add(new SortDescription("Complete", ListSortDirection.Ascending));
                priViewSource.SortDescriptions.Add(new SortDescription("PriorityIDSort", ListSortDirection.Descending));
                priViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                OrderedSource.Add(priViewSource);
                ByPriListBox.DataContext = this;
            }
            ImportTemplateViewSource = new CollectionViewSource();
            ImportTemplateViewSource.Filter += new FilterEventHandler(ImportTemplate_Filter);

            Loaded += new RoutedEventHandler(TaskListPage_Loaded);
            BackKeyPress += new EventHandler<CancelEventArgs>(TaskListPage_BackKeyPress);

            // trace data
            TraceHelper.AddMessage("Exiting TaskList constructor");
        }

        // When page is navigated to set data context to selected item in listType
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace data
            TraceHelper.AddMessage("TaskList: OnNavigatedTo");

            string IDString = "";
            string typeString = "";
            Guid id;

            if (NavigationContext.QueryString.TryGetValue("type", out typeString) == false)
            {
                // trace page navigation
                TraceHelper.StartMessage("TaskList: Navigate back");

                // navigate back
                NavigationService.GoBack();
                return;
            }

            switch (typeString)
            {
                case "TaskList":
                    if (NavigationContext.QueryString.TryGetValue("ID", out IDString) == false)
                    {
                        // trace page navigation
                        TraceHelper.StartMessage("TaskList: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }

                    id = new Guid(IDString);

                    // get the tasklist and make it the datacontext
                    try
                    {
                        taskList = App.ViewModel.TaskLists.Single(tl => tl.ID == id);
                        SetContext(taskList);
                    }
                    catch (Exception ex)
                    {
                        // the list isn't found - this can happen when the list we were just 
                        // editing was removed in TaskListEditor, which then goes back to TaskListPage.
                        // this will send us back to the MainPage which is appropriate.

                        // trace page navigation
                        TraceHelper.StartMessage(String.Format("TaskList: Navigate back (exception: {0})", ex.Message));

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }
                    break;
                case "Tag":
                    if (NavigationContext.QueryString.TryGetValue("ID", out IDString) == false)
                    {
                        // trace page navigation
                        TraceHelper.StartMessage("TaskList: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }

                    id = new Guid(IDString);

                    // create a filter 
                    try
                    {
                        tag = App.ViewModel.Tags.Single(t => t.ID == id);
                        taskList = new TaskList() { ID = Guid.Empty, Name = String.Format("tasks with {0} tag", tag.Name), Tasks = App.ViewModel.Tasks };
                        SetContext(taskList);
                        foreach (var source in OrderedSource)
                            source.Filter += new FilterEventHandler(Tag_Filter);
                    }
                    catch (Exception)
                    {
                        // the tag isn't found - this can happen when the tag we were just 
                        // editing was removed in TagListEditor, which then goes back to TaskListPage.
                        // this will send us back to the MainPage which is appropriate.

                        // trace page navigation
                        TraceHelper.StartMessage("TaskList: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }
                    break;
                default:
                    // trace page navigation
                    TraceHelper.StartMessage("TaskList: Navigate back");

                    // navigate back
                    NavigationService.GoBack();
                    return;
            }

            // workaround for the CollectionViewSource wrappers that are used for the different ListBox sorts
            // setting SelectionMode to Multiple removes the issue where the SelectionChanged event handler gets
            // invoked every time the list is changed (which triggers a re-sort).  The SelectionMode gets reset back
            // to Single when the SelectionChanged events handler gets called (for a valid reason - i.e. user action)
            SetSelectionMode(SelectionMode.Multiple);

            // enable the listbox
            disableListBoxSelectionChanged = false;

            // trace data
            TraceHelper.AddMessage("Exiting TaskList OnNavigatedTo");
        }

        #region Event Handlers

        private void AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("TaskList: Navigate to Task");

            // Navigate to the new page
            NavigationService.Navigate(
                new Uri(String.Format("/TaskPage.xaml?ID={0}&taskListID={1}", "new", taskList.ID),
                UriKind.Relative));
        }

        /// <summary>
        /// Handle click event on Complete checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            // trace data
            TraceHelper.StartMessage("CompleteCheckbox Click");

            CheckBox cb = (CheckBox)e.OriginalSource;
            Guid taskID = (Guid)cb.Tag;

            // get the task that was just updated, and ensure the Complete flag is in the correct state
            Task task = taskList.Tasks.Single<Task>(t => t.ID == taskID);

            // create a copy of that task
            Task taskCopy = new Task(task);

            // toggle the Complete flag on the task copy to reflect the original state 
            taskCopy.Complete = !taskCopy.Complete;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Task>() { taskCopy, task },
                    BodyTypeName = "Task",
                    ID = taskCopy.ID
                });

            // trigger databinding by creating a new taskList and binding to it
            SetContext(null);

            // save the changes to local storage
            StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);

            // trigger a databinding refresh for tasks
            App.ViewModel.NotifyPropertyChanged("Tasks");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace data
            TraceHelper.AddMessage("Finished CompleteCheckbox Click");
        }

        private void DeleteCompletedMenuItem_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("delete all completed tasks in this list?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // we will be building a new tasklist of only the non-completed items
            TaskList newTaskList = new TaskList(taskList);
            newTaskList.Tasks.Clear();
            
            foreach (var task in taskList.Tasks)
            {
                if (task.Complete == true)
                {
                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = task
                        });
                }
                else
                {
                    // add the non-completed task to the new list
                    newTaskList.Tasks.Add(task);
                }
            }

            // replace the main tasklist and databind to the new list
            SetContext(newTaskList);

            // save the changes to local storage
            StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);

            // trigger a databinding refresh for tasks
            App.ViewModel.NotifyPropertyChanged("Tasks");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (taskList.ID != Guid.Empty)
            {
                // trace page navigation
                TraceHelper.StartMessage("TaskList: Navigate to ListEditor");

                // Navigate to the TaskListEditor page
                NavigationService.Navigate(
                    new Uri(String.Format("/TaskListEditor.xaml?ID={0}", taskList.ID),
                    UriKind.Relative));
            }
            else
            {
                // trace page navigation
                TraceHelper.StartMessage("TaskList: Navigate to TagEditor");

                // Navigate to the TagEditor page
                NavigationService.Navigate(
                    new Uri(String.Format("/TagEditor.xaml?ID={0}", tag.ID),
                    UriKind.Relative));
            }
        }

        // handle events associated with Import Template 

        private void ImportTemplate_Filter(object sender, FilterEventArgs e)
        {
            TaskList tl = e.Item as TaskList;
            e.Accepted = tl.Template;
        }

        private void ImportTemplateMenuItem_Click(object sender, EventArgs e)
        {
            // set the collection source for the import template list picker
            ImportTemplateViewSource.Source = App.ViewModel.TaskLists;
            ImportTemplatePopupTaskListPicker.DataContext = this;

            // open the popup, disable list selection bug
            ImportTemplatePopup.IsOpen = true;
            SetSelectionMode(SelectionMode.Multiple);
        }

        private void ImportTemplatePopup_ImportButton_Click(object sender, RoutedEventArgs e)
        {
            TaskList tl = ImportTemplatePopupTaskListPicker.SelectedItem as TaskList;
            if (tl == null)
                return;

            // add the task to a new tasklist reference (to trigger databinding)
            var newTaskList = new TaskList(taskList);

            foreach (Task t in tl.Tasks)
            {
                DateTime now = DateTime.UtcNow;

                // create the new task
                Task task = new Task(t) { ID = Guid.NewGuid(), TaskListID = taskList.ID, Created = now, LastModified = now };
                // recreate the tasktags (they must be unique)
                if (task.TaskTags != null && task.TaskTags.Count > 0)
                {
                    foreach (var tt in task.TaskTags)
                    {
                        tt.ID = Guid.NewGuid();
                    }
                }

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = task
                    });

                // add the task to the local collection
                newTaskList.Tasks.Add(task);
            }

            // replace the main tasklist and databind to the new list
            SetContext(newTaskList);

            // save the changes to local storage
            StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);

            // trigger a databinding refresh for tasks
            App.ViewModel.NotifyPropertyChanged("Tasks");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // close the popup 
            ImportTemplatePopup.IsOpen = false;
        }

        private void ImportTemplatePopup_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // close the popup 
            ImportTemplatePopup.IsOpen = false;
        }

        // Handle selection changed on ListBox
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // if listbox is disabled (because we've navigated off this page), return without handling this event.
            // this condition can happen when we've already navigated off this page, and an async network operation
            // creates a new Tasks collection, which is databound to this ListBox.  Since we have a ViewSourceCollection as 
            // the databinding source, and a sort is applied, the Selection will be changed automatically and trigger this
            // event even though we're not on the page anymore.
            if (disableListBoxSelectionChanged == true)
                return;

            ListBox listBox = (ListBox)sender;
            // If selected index is -1 (no selection) do nothing
            if (listBox.SelectedIndex == -1)
                return;

            // get the task associated with this click
            Task task = null;

            if (listBox.SelectionMode == SelectionMode.Multiple)
            {
                var selItems = listBox.SelectedItems;
                var c = selItems.Count;
                // the last task in the SelectedItems collection is the one we want (the previous one(s) were generated by re-binding 
                // events and are spurious)
                foreach (var item in selItems)
                {
                    task = (Task)item;
                }

                // reset the selection mode to single (it's now safe to do so)
                // note: this will recursively reinvoke the SelectionChanged event handler
                SetSelectionMode(SelectionMode.Single);  
            }
            else
            {
                // this is a single selection mode - just retrieve the current selection
                task = (Task)listBox.SelectedItem;
            }

            // if there is no task, return without processing the event
            if (task == null)
                return;

            // trace page navigation
            TraceHelper.StartMessage("TaskList: Navigate to Task");

            // Navigate to the new page
            NavigationService.Navigate(
                new Uri(String.Format("/TaskPage.xaml?ID={0}&taskListID={1}", task.ID, task.TaskListID),
                UriKind.Relative));

            // we need to disable the selection changed event so that we don't get cascading calls to Navigate which will
            // cause a navigation exception
            disableListBoxSelectionChanged = true;

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        // handle events associated with the Lists button
        private void ListsButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("TaskList: Navigate to Main");

            // Navigate to the main page
            NavigationService.Navigate(
                new Uri("/MainPage.xaml?Tab=Lists", UriKind.Relative));
        }

        private void PivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderedSource[PivotControl.SelectedIndex].Source == null)
            {
                // set the source for the current tab (doing all three is too expensive)
                OrderedSource[PivotControl.SelectedIndex].Source = taskList.Tasks;
            }
        }

        // handle events associated with the Quick Add Popup
        private void QuickAddButton_Click(object sender, EventArgs e)
        {
            // open the popup, disable list selection bug, and transfer focus to the popup text box
            QuickAddPopup.IsOpen = true;
            SetSelectionMode(SelectionMode.Multiple);
            PopupTextBox.Focus();
        }

        private void QuickAddPopup_AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = PopupTextBox.Text;
            // don't add empty items
            if (name == null || name == "")
                return;

            // create the new task
            Task task = new Task() { Name = name, TaskListID = taskList.ID, LastModified = DateTime.UtcNow };

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = task
                });

            // add the task to a new tasklist reference (to trigger databinding)
            var newTaskList = new TaskList(taskList);
            newTaskList.Tasks.Add(task);

            // replace the main tasklist and databind to the new list
            SetContext(newTaskList);

            // save the changes to local storage
            StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);

            // trigger a databinding refresh for tasks
            App.ViewModel.NotifyPropertyChanged("Tasks");

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // clear the textbox and focus back to it
            PopupTextBox.Text = "";
            PopupTextBox.Focus();
        }

        private void QuickAddPopup_DoneButton_Click(object sender, RoutedEventArgs e)
        {
            // close the popup 
            QuickAddPopup.IsOpen = false;
        }

        // handle events associated with the Speech Popup
        private void SpeechButton_Click(object sender, RoutedEventArgs e)
        {
            // require an account
            if (App.ViewModel.User == null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "the speech feature requires an account.  create a free account now?",
                    "create account?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;

                // trace page navigation
                TraceHelper.StartMessage("TaskList: Navigate to Settings");

                // Navigate to the settings page
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
                return;
            }

            // set the UI state to initializing state
            speechState = SpeechHelper.SpeechState.Initializing;
            SpeechSetUIState(speechState);

            // store debug / timing info
            speechStart = DateTime.Now;
            speechDebugString = "";

            // store debug / timing info
            TimeSpan ts = DateTime.Now - speechStart;
            string stateString = SpeechHelper.SpeechStateString(speechState);
            speechDebugString += String.Format("New state: {0}; Time: {1}; Message: {2}\n", stateString, ts.TotalSeconds, "Connecting Socket");

            // initialize the connection to the speech service
            SpeechHelper.Start(
                App.ViewModel.User,
                new SpeechHelper.SpeechStateCallbackDelegate(SpeechPopup_SpeechStateCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));

            // open the popup
            SpeechPopup.IsOpen = true;
        }

        private void SpeechPopup_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            switch (speechState)
            {
                case SpeechHelper.SpeechState.Initializing:
                case SpeechHelper.SpeechState.Listening:
                case SpeechHelper.SpeechState.Recognizing:
                    // user tapped the cancel button

                    // cancel the current operation / close the socket to the service
                    SpeechHelper.CancelStreamed(
                        new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));

                    // reset the text in the textbox
                    PopupTextBox.Text = "";
                    break;
                case SpeechHelper.SpeechState.Finished:
                    // user tapped the OK button

                    // set the text in the popup textbox
                    PopupTextBox.Text = SpeechLabelText.Trim('\'');
                    break;
            }
 
            SpeechPopup_Close();
        }

        private void SpeechPopup_Close()
        {
            // close the popup 
            SpeechPopup.IsOpen = false;
        }

        private void SpeechPopup_NetworkOperationInProgressCallBack(bool operationInProgress, bool? operationSuccessful)
        {
            // call the MainViewModel's routine to make sure global network status is reset
            App.ViewModel.NetworkOperationInProgressCallback(operationInProgress, operationSuccessful);

            // signal whether the net operation is in progress or not
            NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);

            // if the operationSuccessful flag is null, no new data; otherwise, it signals the status of the last operation
            if (operationSuccessful != null)
            {
                if ((bool)operationSuccessful == false)
                {
                    // the server wasn't reachable
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show("apologies - cannot reach the speech service at this time.");
                        SpeechPopup_Close();
                    });
                }
            }
        }

        private void SpeechPopup_SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan ts;
            string stateString;

            switch (speechState)
            {
                case SpeechHelper.SpeechState.Initializing:
                    // can't happen since the button isn't enabled
#if DEBUG
                    MessageBox.Show("Invalid state SpeechState.Initializing reached");
#endif
                    break;
                case SpeechHelper.SpeechState.Listening:
                    // done button tapped

                    // set the UI state to recognizing state
                    speechState = SpeechHelper.SpeechState.Recognizing;
                    SpeechSetUIState(speechState);

                    // store debug / timing info
                    ts = DateTime.Now - speechStart;
                    stateString = SpeechHelper.SpeechStateString(speechState);
                    speechDebugString += String.Format("New state: {0}; Time: {1}; Message: {2}\n", stateString, ts.TotalSeconds, "Stopping mic");

                    // stop listening and get the recognized text from the speech service
                    //SpeechHelper.Stop(
                    //    App.ViewModel.User,
                    //    new SpeechHelper.SpeechToTextCallbackDelegate(SpeechPopup_SpeechToTextCallback),
                    //    new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));
                    SpeechHelper.Stop(new SpeechHelper.SpeechToTextCallbackDelegate(SpeechPopup_SpeechToTextCallback)); 
                    break;
                case SpeechHelper.SpeechState.Recognizing:
                    // can't happen since the button isn't enabled
#if DEBUG
                    MessageBox.Show("Invalid state SpeechState.Initializing reached");
#endif
                    break;
                case SpeechHelper.SpeechState.Finished:
                    // "try again" button tapped

                    // set the UI state to initializing state
                    speechState = SpeechHelper.SpeechState.Initializing;
                    SpeechSetUIState(speechState);

                    // store debug / timing info
                    speechStart = DateTime.Now;
                    speechDebugString = "";

                    // store debug / timing info
                    ts = DateTime.Now - speechStart;
                    stateString = SpeechHelper.SpeechStateString(speechState);
                    speechDebugString += String.Format("New state: {0}; Time: {1}; Message: {2}\n", stateString, ts.TotalSeconds, "Initializing Request");

                    // initialize the connection to the speech service
                    SpeechHelper.Start(
                        App.ViewModel.User,
                        new SpeechHelper.SpeechStateCallbackDelegate(SpeechPopup_SpeechStateCallback),
                        new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));
                    break;
            }
        }

        private void SpeechPopup_SpeechStateCallback(SpeechHelper.SpeechState state, string message)
        {
            speechState = state;
            SpeechSetUIState(speechState);

            // store debug / timing info
            TimeSpan ts = DateTime.Now - speechStart;
            string stateString = SpeechHelper.SpeechStateString(state);
            speechDebugString += String.Format("New state: {0}; Time: {1}; Message: {2}\n", stateString, ts.TotalSeconds, message);
        }

        private void SpeechPopup_SpeechToTextCallback(string textString)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // set the UI state to finished state
                speechState = SpeechHelper.SpeechState.Finished;
                SpeechSetUIState(speechState);

                // store debug / timing info
                TimeSpan ts = DateTime.Now - speechStart;
                string stateString = SpeechHelper.SpeechStateString(speechState);
                speechDebugString += String.Format("New state: {0}; Time: {1}; Message: {2}\n", stateString, ts.TotalSeconds, textString);

                // strip any timing / debug info 
                textString = textString == null ? "" : textString;
                string[] words = textString.Split(' ');
                if (words[words.Length - 1] == "seconds")
                {
                    textString = "";
                    // strip off last two words - "a.b seconds"
                    for (int i = 0; i < words.Length - 2; i++)
                    {
                        textString += words[i];
                        textString += " ";
                    }
                    textString = textString.Trim();
                }

                // set the speech label text as well as the popup text
                SpeechLabelText = textString == null ? "recognition failed" : String.Format("'{0}'", textString);
                PopupTextBox.Text = textString;

#if DEBUG && KILL
                MessageBox.Show(speechDebugString);
#endif
            });
        }

        // event handlers related to tags
        private void Tag_Filter(object sender, FilterEventArgs e)
        {
            Task task = e.Item as Task;
            try
            {
                TaskTag taskTag = task.TaskTags.Single(tt => tt.TagID == tag.ID);
                if (taskTag != null)
                    e.Accepted = true;
                else
                    e.Accepted = false;
            }
            catch (Exception)
            {
                e.Accepted = false;
            }
        }

        private void Tag_HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton button = (HyperlinkButton)e.OriginalSource;
            Guid tagID = (Guid)button.Tag;

            // trace page navigation
            TraceHelper.StartMessage("TaskList: Navigate to Tag");

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/TaskListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
        }

        void TaskListPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("TaskList: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void TaskListPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace page navigation
            TraceHelper.AddMessage("TaskList: Loaded");
        }

        #endregion

        #region Helpers

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

        private void SetContext(TaskList newTaskList)
        {
            // dispatch all this work on the UI thread in order to stop the app from blocking
            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
                // if a new copy wasn't passed, create a new one now (to facilitate databinding)
                if (newTaskList == null)
                    newTaskList = new TaskList(taskList);

                // find the current tasklist's index and replace the entry in the viewmodel's TaskLists collection
                // this call will return -1 if the tasklist's index is not found, because we are filtering by tag or doing a search
                // in that case, we don't need to replace the list
                int index = IndexOf(App.ViewModel.TaskLists, taskList);
                if (index >= 0)
                {
                    // replace the existing list with the new reference
                    App.ViewModel.TaskLists[index] = newTaskList;
                }

                // replace the top-level reference
                taskList = newTaskList;

                // reset the contexts for databinding
                DataContext = newTaskList;
                OrderedSource[0].Source = null;
                OrderedSource[1].Source = null;
                OrderedSource[2].Source = null;

                // set the source for the current tab (doing all three is too expensive)
                OrderedSource[PivotControl.SelectedIndex].Source = taskList.Tasks;
            //});
        }

        /// <summary>
        /// Set the selection mode for all three list boxes
        /// </summary>
        /// <param name="selectionMode"></param>
        private void SetSelectionMode(SelectionMode selectionMode)
        {
            // workaround for the CollectionViewSource wrappers that are used for the different ListBox sorts
            // setting SelectionMode to Multiple removes the issue where the SelectionChanged event handler gets
            // invoked every time the list is changed (which triggers a resort).  The SelectionMode gets reset back
            // to Single when the SelectionChanged events handler gets called (for a valid reason - i.e. user action)

            // mitigate the recursive behavior of setting the SelectionMode re-invoking the SelectionChanged event handler
            bool disableState = disableListBoxSelectionChanged;
            disableListBoxSelectionChanged = true;

            ByNameListBox.SelectionMode = selectionMode;
            ByDueListBox.SelectionMode = selectionMode;
            ByPriListBox.SelectionMode = selectionMode;

            // reset the disable state
            disableListBoxSelectionChanged = disableState;
        }

        /// <summary>
        /// Set the UI based on the current state of the speech state machine
        /// </summary>
        /// <param name="state"></param>
        private void SpeechSetUIState(SpeechHelper.SpeechState state)
        {
            switch (state)
            {
                case SpeechHelper.SpeechState.Initializing:
                    SpeechLabelText = "initializing...";
                    SpeechButtonText = "done";
                    SpeechButtonEnabled = false;
                    SpeechCancelButtonText = "cancel";
                    break;
                case SpeechHelper.SpeechState.Listening:
                    SpeechLabelText = "listening...";
                    SpeechButtonText = "done";
                    SpeechButtonEnabled = true;
                    SpeechCancelButtonText = "cancel";
                    break;
                case SpeechHelper.SpeechState.Recognizing:
                    SpeechLabelText = "recognizing...";
                    SpeechButtonText = "try again";
                    SpeechButtonEnabled = false;
                    SpeechCancelButtonText = "cancel";
                    break;
                case SpeechHelper.SpeechState.Finished:
                    SpeechLabelText = "";
                    SpeechButtonText = "try again";
                    SpeechButtonEnabled = true;
                    SpeechCancelButtonText = "ok";
                    break;
            }
        }

        #endregion
    }
}