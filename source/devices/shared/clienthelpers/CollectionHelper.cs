using System;
using System.Net;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using BuiltSteady.Zaplify.Devices.ClientEntities;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
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
