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

namespace TaskStoreWinPhoneUtilities
{
    public static class CollectionHelper
    {
        public static ObservableCollection<Task> ToObservableCollection(this IEnumerable<Task> coll)
        {
            ObservableCollection<Task> ret = new ObservableCollection<Task>();
            foreach (var o in coll)
                ret.Add(o);

            return ret;
        }

        public static ObservableCollection<TaskList> ToObservableCollection(this IEnumerable<TaskList> coll)
        {
            ObservableCollection<TaskList> ret = new ObservableCollection<TaskList>();
            foreach (var o in coll)
                ret.Add(o);

            return ret;
        }
    }
}
