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
using System.Windows.Media.Imaging;
using Microsoft.Phone.Net.NetworkInformation;

namespace TaskStoreWinPhone
{
    public partial class ListPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private const int rendersize = 10;  // limit of elements to render immediately
        private bool constructorCalled = false;
        private TaskList taskList;
        private TaskListHelper TaskListHelper;
        private Tag tag;

        private SpeechHelper.SpeechState speechState;
        private string speechDebugString = null;
        private DateTime speechStart;

        // ViewSource for the TaskList collection for Import Template (used for filtering out non-template lists)
        public CollectionViewSource ImportTemplateViewSource { get; set; }

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
        public ListPage()
        {
            InitializeComponent();

            // trace data
            TraceHelper.AddMessage("List: constructor");

            // set some data context information
            ConnectedIconImage.DataContext = App.ViewModel;
            SpeechProgressBar.DataContext = App.ViewModel;

            // set some data context information for the speech UI
            SpeechPopup_SpeakButton.DataContext = this;
            SpeechPopup_CancelButton.DataContext = this;
            SpeechLabel.DataContext = this;

            ImportTemplateViewSource = new CollectionViewSource();
            ImportTemplateViewSource.Filter += new FilterEventHandler(ImportTemplate_Filter);

            // add some event handlers
            Loaded += new RoutedEventHandler(ListPage_Loaded);
            BackKeyPress += new EventHandler<CancelEventArgs>(ListPage_BackKeyPress);

            // set the constructor called flag
            constructorCalled = true;

            // trace data
            TraceHelper.AddMessage("Exiting List constructor");
        }

        // When page is navigated to set data context to selected item in listType
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // trace data
            TraceHelper.AddMessage("List: OnNavigatedTo");

            string IDString = "";
            string typeString = "";
            Guid id;

            if (NavigationContext.QueryString.TryGetValue("type", out typeString) == false)
            {
                // trace page navigation
                TraceHelper.StartMessage("List: Navigate back");

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
                        TraceHelper.StartMessage("List: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }

                    id = new Guid(IDString);

                    // get the tasklist and make it the datacontext
                    try
                    {
                        taskList = App.ViewModel.LoadList(id);

                        // if the load failed, this list has been deleted
                        if (taskList == null)
                        {
                            // the list isn't found - this can happen when the list we were just 
                            // editing was removed in TaskListEditor, which then goes back to TaskListPage.
                            // this will send us back to the MainPage which is appropriate.

                            // trace page navigation
                            TraceHelper.StartMessage("List: Navigate back");

                            // navigate back
                            NavigationService.GoBack();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // the list isn't found - this can happen when the list we were just 
                        // editing was removed in TaskListEditor, which then goes back to TaskListPage.
                        // this will send us back to the MainPage which is appropriate.

                        // trace page navigation
                        TraceHelper.StartMessage(String.Format("List: Navigate back (exception: {0})", ex.Message));

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }
                    break;
                case "Tag":
                    if (NavigationContext.QueryString.TryGetValue("ID", out IDString) == false)
                    {
                        // trace page navigation
                        TraceHelper.StartMessage("List: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }

                    id = new Guid(IDString);

                    // create a filter 
                    try
                    {
                        tag = App.ViewModel.Tags.Single(t => t.ID == id);
                        taskList = new TaskList() 
                        { 
                            ID = Guid.Empty, 
                            Name = String.Format("tasks with {0} tag", tag.Name), 
                            Tasks = App.ViewModel.Tasks.Where(t => t.TaskTags.Any(tg => tg.TagID == tag.ID)).ToObservableCollection()
                        };
                    }
                    catch (Exception)
                    {
                        // the tag isn't found - this can happen when the tag we were just 
                        // editing was removed in TagListEditor, which then goes back to TaskListPage.
                        // this will send us back to the MainPage which is appropriate.

                        // trace page navigation
                        TraceHelper.StartMessage("List: Navigate back");

                        // navigate back
                        NavigationService.GoBack();
                        return;
                    }
                    break;
                default:
                    // trace page navigation
                    TraceHelper.StartMessage("List: Navigate back");

                    // navigate back
                    NavigationService.GoBack();
                    return;
            }

            // set datacontext 
            DataContext = taskList;

            // create the TaskListHelper
            TaskListHelper = new TaskStoreWinPhone.TaskListHelper(
                taskList, 
                new RoutedEventHandler(CompleteCheckbox_Click), 
                new RoutedEventHandler(Tag_HyperlinkButton_Click));

            // store the current listbox and ordering
            PivotItem item = (PivotItem) PivotControl.Items[PivotControl.SelectedIndex];
            TaskListHelper.ListBox = (ListBox)((Grid)item.Content).Children[1];
            TaskListHelper.OrderBy = (string)item.Header;

            // trace data
            TraceHelper.AddMessage("Exiting List OnNavigatedTo");
        }

        #region Event Handlers

        private void AddButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("List: Navigate to Task");

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
            
            // reorder the task in the tasklist and the ListBox
            TaskListHelper.ReOrderTask(taskList, task);

            // save the changes to local storage
            StorageHelper.WriteList(taskList);

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

            // create a copy of the tasklist to foreach over.  this is because we can't delete
            // from the original collection while it's being enumerated.  the copy we make is shallow 
            // so as not to create brand new Task objects, but then we add all the task references to 
            // an new Tasks collection that won't interfere with the existing one.
            TaskList tl = new TaskList(taskList, false);
            tl.Tasks = new ObservableCollection<Task>();
            foreach (Task t in taskList.Tasks)
                tl.Tasks.Add(t);

            // remove any completed tasks from the original tasklist
            foreach (var task in tl.Tasks)
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

                    // remove the task from the original collection and from ListBox
                    TaskListHelper.RemoveTask(taskList, task);
                }
            }

            // save the changes to local storage
            StorageHelper.WriteList(taskList);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (taskList.ID != Guid.Empty)
            {
                // trace page navigation
                TraceHelper.StartMessage("List: Navigate to ListEditor");

                // Navigate to the TaskListEditor page
                NavigationService.Navigate(
                    new Uri(String.Format("/TaskListEditor.xaml?ID={0}", taskList.ID),
                    UriKind.Relative));
            }
            else
            {
                // trace page navigation
                TraceHelper.StartMessage("List: Navigate to TagEditor");

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
        }

        private void ImportTemplatePopup_ImportButton_Click(object sender, RoutedEventArgs e)
        {
            TaskList tl = ImportTemplatePopupTaskListPicker.SelectedItem as TaskList;
            if (tl == null)
                return;

            // add the tasks in the template to the existing list
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
                // note that we don't use TaskListHelper.AddTask() here because it is typically more efficient
                // to add all the tasks and then re-render the entire list.  This is because 
                // the typical use case is to import a template into an empty (or nearly empty) list.
                taskList.Tasks.Add(task);
            }

            // render the new list 
            TaskListHelper.RenderList(taskList);

            // save the changes to local storage
            StorageHelper.WriteList(taskList);

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
            TraceHelper.StartMessage("List: Navigate to Task");

            // Navigate to the new page
            NavigationService.Navigate(
                new Uri(String.Format("/TaskPage.xaml?ID={0}&taskListID={1}", task.ID, task.TaskListID),
                UriKind.Relative));

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        // handle ListPage events
        void ListPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("List: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void ListPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace page navigation
            TraceHelper.AddMessage("List: Loaded");

            // reset the constructor flag
            constructorCalled = false;

            // create the control tree and render the tasklist
            TaskListHelper.RenderList(taskList);

            // trace page navigation
            TraceHelper.AddMessage("Finished List Loaded");
        }

        // handle events associated with the Lists button
        private void ListsButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("List: Navigate to Main");

            // Navigate to the main page
            NavigationService.Navigate(
                new Uri("/MainPage.xaml?Tab=Lists", UriKind.Relative));
        }

        private void PivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // store the current listbox
            TaskListHelper.ListBox = (ListBox)((Grid)((PivotItem)PivotControl.SelectedItem).Content).Children[1];
            TaskListHelper.OrderBy = (string)((PivotItem)PivotControl.SelectedItem).Header;

            // the pivot control's selection changed event gets called during the initialization of a new
            // page.  since we do rendering in the Loaded event handler, we need to skip rendering here
            // so that we don't do it twice and slow down the loading of the page.
            if (constructorCalled == false)
            {
                TaskListHelper.RenderList(taskList);
            }
        }

        // handle events associated with the Quick Add Popup
        private void QuickAddButton_Click(object sender, EventArgs e)
        {
            // open the popup, disable list selection bug, and transfer focus to the popup text box
            QuickAddPopup.IsOpen = true;
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

            // add the new task
            TaskListHelper.AddTask(taskList, task);

            // save the changes to local storage
            StorageHelper.WriteList(taskList);

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
            // require a connection
            if (DeviceNetworkInformation.IsNetworkAvailable == false ||
                NetworkInterface.GetIsNetworkAvailable() == false)
            {
                MessageBox.Show("apologies - a network connection is required for this feature, and you appear to be disconnected :-(");
                return;
            }
            
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
                TraceHelper.StartMessage("List: Navigate to Settings");

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
            string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Connecting Socket");
            TraceHelper.AddMessage(traceString);
            speechDebugString += traceString + "\n";

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
            string traceString;

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
                    traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Stopping mic");
                    TraceHelper.AddMessage(traceString);
                    speechDebugString += traceString + "\n";

                    // stop listening and get the recognized text from the speech service
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
                    traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Initializing Request");
                    TraceHelper.AddMessage(traceString);
                    speechDebugString += traceString + "\n";

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
            string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, message);
            TraceHelper.AddMessage(traceString);
            speechDebugString += traceString + "\n";
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
                string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, textString);
                TraceHelper.AddMessage(traceString);
                speechDebugString += traceString + "\n";

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

        private void Tag_HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton button = (HyperlinkButton)e.OriginalSource;
            Guid tagID = (Guid)button.Tag;

            // trace page navigation
            TraceHelper.StartMessage("List: Navigate to Tag");

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/ListPage.xaml?type=Tag&ID=" + tagID.ToString(), UriKind.Relative));
        }

        #endregion

        #region Helpers

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