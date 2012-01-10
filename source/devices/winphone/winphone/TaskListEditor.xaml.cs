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
using TaskStoreClientEntities;
using Microsoft.Phone.Shell;
using TaskStoreWinPhoneUtilities;
using System.ComponentModel;

namespace TaskStoreWinPhone
{
    public partial class TaskListEditor : PhoneApplicationPage
    {
        private TaskList taskList;
        private TaskList taskListCopy;
        
        public TaskListEditor()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("ListEditor: constructor");

            ConnectedIconImage.DataContext = App.ViewModel;

            // enable tabbing
            this.IsTabStop = true;

            this.Loaded += new RoutedEventHandler(TaskListEditor_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(TaskListEditor_BackKeyPress);
        }

        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }
        
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new tasklist, delete just does the same thing as cancel
            if (taskList == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this list?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = taskList
                });

            // remove the task from the viewmodel
            App.ViewModel.TaskLists.Remove(taskList);
            App.ViewModel.Lists.Remove(taskList.ID);

            // save the changes to local storage
            StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);
            StorageHelper.DeleteList(taskList);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // Navigate back to the main page
            NavigationService.GoBack();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            int listTypeIndex = ListTypePicker.SelectedIndex;
            if (listTypeIndex < 0)
            {
                MessageBox.Show("list type must be set");
                return;
            }

            // set the appropriate list type ID
            taskListCopy.ListTypeID = App.ViewModel.ListTypes[listTypeIndex].ID;

            // get the name of the tag
            taskListCopy.Name = ListName.Text;

            // check for appropriate values
            if (taskListCopy.Name == "")
            {
                MessageBox.Show("list name cannot be empty");
                return;
            }

            // if this is a new list, create it
            if (taskList == null)
            {
                // enqueue the Web Request Record (with a new copy of the taskList)
                // need to create a copy because otherwise other tasks may be added to it
                // and we want the record to have exactly one operation in it (create the tasklist)
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = new TaskList(taskListCopy)
                    });

                // add the task to the local listType
                App.ViewModel.TaskLists.Add(taskListCopy);
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<TaskList>() { taskList, taskListCopy },
                        BodyTypeName = "TaskList",
                        ID = taskList.ID
                    });

                // save the changes to the existing tasklist (make a deep copy)
                taskList.Copy(taskListCopy, true);
            }

            // save the changes to local storage
            StorageHelper.WriteTaskLists(App.ViewModel.TaskLists);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // Navigate back to the main page
            NavigationService.GoBack();
        }

        void TaskListEditor_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void TaskListEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("ListEditor: Loaded");

            string taskListIDString = "";

            if (NavigationContext.QueryString.TryGetValue("ID", out taskListIDString))
            {
                if (taskListIDString == "new")
                {
                    // new tasklist
                    taskListCopy = new TaskList();
                    DataContext = taskListCopy;
                }
                else
                {
                    Guid taskListID = new Guid(taskListIDString);
                    taskList = App.ViewModel.TaskLists.Single<TaskList>(tl => tl.ID == taskListID);

                    // make a deep copy of the task for local binding
                    taskListCopy = new TaskList(taskList);
                    DataContext = taskListCopy;

                    // add the delete button to the ApplicationBar
                    var button = new ApplicationBarIconButton() { Text = "Delete", IconUri = new Uri("/Images/appbar.delete.rest.png", UriKind.Relative) };
                    button.Click += new EventHandler(DeleteButton_Click);

                    // insert after the save button but before the cancel button
                    ApplicationBar.Buttons.Add(button);
                }

                RenderListTypes();
            }
        }

        #endregion

        #region Helpers

        private void RenderListTypes()
        {
            ListTypePicker.ItemsSource = App.ViewModel.ListTypes;
            ListTypePicker.DisplayMemberPath = "Name";

            // set the selected index 
            if (taskListCopy.ListTypeID != null && taskListCopy.ListTypeID != Guid.Empty)
            {
                try
                {
                    ListType listType = App.ViewModel.ListTypes.Single(lt => lt.ID == taskListCopy.ListTypeID);
                    int index = App.ViewModel.ListTypes.IndexOf(listType);
                    ListTypePicker.SelectedIndex = index;
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}