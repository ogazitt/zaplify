using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.WinPhone.Controls
{
    public class ItemRefListPicker : ListPicker
    {
        public ItemRefListPicker(Folder folder, Item currentList, Guid itemTypeID, PropertyInfo pi, object container)
        {
            this.ExpansionMode = ExpansionMode.FullScreenOnly;
            this.SelectionMode = SelectionMode.Multiple;
            this.SummaryForSelectedItemsDelegate = (list) => { return CreateCommaDelimitedList(list); };

            // create a list of ItemRefs to all the items in the user's Item collection that match the item type passed in
            var allRefs = App.ViewModel.Items.
                Where(it => it.ItemTypeID == itemTypeID && it.IsList == false).
                Select(it => new Item() { Name = it.Name, FolderID = currentList.FolderID, ItemTypeID = SystemItemTypes.Reference, ParentID = currentList.ID, ItemRef = it.ID }).
                ToList();

            this.ItemsSource = allRefs;
            this.DisplayMemberPath = "Name";
            this.SetValue(ListPicker.SelectedItemsProperty, new List<Item>());

            // replace all ItemRefs which are already in the selected list 
            foreach (var itemRef in currentList.Items.ToList())
            {
                bool found = false;
                for (var i = 0; i < allRefs.Count; i++)
                    if (allRefs[i].ItemRef == itemRef.ItemRef)
                    {
                        found = true;
                        allRefs[i] = itemRef;
                        this.SelectedItems.Add(itemRef);
                        break;
                    }
                // remove item refs that no longer point to a valid contact
                if (found == false)
                {
                    currentList.Items.Remove(itemRef);
                    folder.Items.Remove(itemRef);
                    StorageHelper.WriteFolder(folder);

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = itemRef
                        });
                }
            }

            this.SelectionChanged += new SelectionChangedEventHandler((o, ea) =>
            {
                // if the list doesn't yet exist, create it now
                if (ea.AddedItems.Count > 0 && currentList.ID == Guid.Empty)
                {
                    Guid id = Guid.NewGuid();
                    currentList.ID = id;
                    // fix the pickList's ParentID's to this new list ID (otherwise they stay Guid.Empty)
                    foreach (var i in allRefs)
                        i.ParentID = id;

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = currentList
                        });

                    // add the list to the folder
                    folder.Items.Add(currentList);
                    StorageHelper.WriteFolder(folder);

                    // store the list's Guid in the item's property 
                    pi.SetValue(container, id.ToString(), null);
                }

                // add all the newly added items
                foreach (var added in ea.AddedItems)
                {
                    Item addedItem = added as Item;
                    if (addedItem == null)
                        continue;
                    currentList.Items.Add(addedItem);
                    folder.Items.Add(addedItem);
                    StorageHelper.WriteFolder(folder);

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = addedItem
                        });
                }

                // remove all the newly removed items
                foreach (var removed in ea.RemovedItems)
                {
                    Item removedItem = removed as Item;
                    if (removedItem == null)
                        continue;
                    currentList.Items.Remove(removedItem);
                    folder.Items.Remove(removedItem);
                    StorageHelper.WriteFolder(folder);

                    // enqueue the Web Request Record
                    RequestQueue.EnqueueRequestRecord(RequestQueue.UserQueue,
                        new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = removedItem
                        });
                }
            });
        }

        private string CreateCommaDelimitedList(IList ilist)
        {
            if (ilist == null)
                return null;
            IList<Item> list = new List<Item>();
            foreach (var i in ilist)
                list.Add((Item)i);

            // build a comma-delimited list of names to display in a control
            List<string> names = list.Select(it => it.Name).ToList();
            StringBuilder sb = new StringBuilder();
            bool comma = false;
            foreach (var name in names)
            {
                if (comma)
                    sb.Append(", ");
                else
                    comma = true;
                sb.Append(name);
            }
            return sb.ToString();
        }
    }
}
