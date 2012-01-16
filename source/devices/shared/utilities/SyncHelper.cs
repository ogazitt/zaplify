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
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Collections.ObjectModel;

namespace BuiltSteady.Zaplify.Devices.Utilities
{
    public class SyncHelper
    {
        public static ObservableCollection<Folder> ResolveFolders(ObservableCollection<Folder> localFolders, List<Folder> remoteFolders)
        {
            if (remoteFolders == null)
                return localFolders;

            // create a new collection and copy the remote folders into it as a starting point
            ObservableCollection<Folder> newFolders = new ObservableCollection<Folder>();
            foreach (Folder tl in remoteFolders)
                newFolders.Add(new Folder(tl));

            // merge any of the local folders as approriate
            foreach (Folder localFolder in localFolders)
            {
                bool foundFolder = false;
                foreach (Folder remoteFolder in newFolders)
                {
                    if (localFolder.ID == remoteFolder.ID)
                    {
                        ResolveItems(localFolder, remoteFolder);
                        foundFolder = true;
                        break;
                    }
                }
                // if didn't find the local item itemType in the remote data set, copy it over
                if (foundFolder == false)
                {
                    newFolders.Add(localFolder);
                }
            }

            return newFolders;
        }


        /// <summary>
        /// Resolve Item conflicts between a local and remote Folder
        /// </summary>
        /// <param name="localFolder">Local item itemType</param>
        /// <param name="remoteFolder">Item itemType retrieved from the data service</param>
        private static void ResolveItems(Folder localFolder, Folder remoteFolder)
        {
            foreach (Item localItem in localFolder.Items)
            {
                bool foundItem = false;
                foreach (Item remoteItem in remoteFolder.Items)
                {
                    if (localItem.ID == remoteItem.ID)
                    {
                        foundItem = true;
                        break;
                    }
                }
                if (foundItem == false)
                {
                    remoteFolder.Items.Add(localItem);
                }
            }
        }
    }
}
