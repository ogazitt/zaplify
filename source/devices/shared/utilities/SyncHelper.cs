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
using System.Collections.Generic;
using TaskStoreClientEntities;
using System.Collections.ObjectModel;

namespace TaskStoreWinPhoneUtilities
{
    public class SyncHelper
    {
        public static ObservableCollection<TaskList> ResolveTaskLists(ObservableCollection<TaskList> localTaskLists, List<TaskList> remoteTaskLists)
        {
            if (remoteTaskLists == null)
                return localTaskLists;

            // create a new collection and copy the remote tasklists into it as a starting point
            ObservableCollection<TaskList> newTaskLists = new ObservableCollection<TaskList>();
            foreach (TaskList tl in remoteTaskLists)
                newTaskLists.Add(new TaskList(tl));

            // merge any of the local tasklists as approriate
            foreach (TaskList localTaskList in localTaskLists)
            {
                bool foundTaskList = false;
                foreach (TaskList remoteTaskList in newTaskLists)
                {
                    if (localTaskList.ID == remoteTaskList.ID)
                    {
                        ResolveTasks(localTaskList, remoteTaskList);
                        foundTaskList = true;
                        break;
                    }
                }
                // if didn't find the local task listType in the remote data set, copy it over
                if (foundTaskList == false)
                {
                    newTaskLists.Add(localTaskList);
                }
            }

            return newTaskLists;
        }


        /// <summary>
        /// Resolve Task conflicts between a local and remote TaskList
        /// </summary>
        /// <param name="localTaskList">Local task listType</param>
        /// <param name="remoteTaskList">Task listType retrieved from the data service</param>
        private static void ResolveTasks(TaskList localTaskList, TaskList remoteTaskList)
        {
            foreach (Task localTask in localTaskList.Tasks)
            {
                bool foundTask = false;
                foreach (Task remoteTask in remoteTaskList.Tasks)
                {
                    if (localTask.ID == remoteTask.ID)
                    {
                        foundTask = true;
                        break;
                    }
                }
                if (foundTask == false)
                {
                    remoteTaskList.Tasks.Add(localTask);
                }
            }
        }
    }
}
