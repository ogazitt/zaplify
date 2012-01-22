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

namespace BuiltSteady.Zaplify.Devices.Utilities
{
    public static class CollectionHelper
    {
        public static ObservableCollection<Item> ToObservableCollection(this IEnumerable<Item> coll)
        {
            ObservableCollection<Item> ret = new ObservableCollection<Item>();
            foreach (var o in coll)
                ret.Add(o);

            return ret;
        }

        public static ObservableCollection<Folder> ToObservableCollection(this IEnumerable<Folder> coll)
        {
            ObservableCollection<Folder> ret = new ObservableCollection<Folder>();
            foreach (var o in coll)
                ret.Add(o);

            return ret;
        }
    }
}
