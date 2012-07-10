using System;
using System.Drawing;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
    public class ParentListPickerElement : ThemedRootElement 
    {    
        List<Item> lists;

        public ParentListPickerElement(string caption, Item list) : base(caption, new RadioGroup(null, 0)) 
        {
            lists = new List<Item>();
            foreach (var f in App.ViewModel.Folders.OrderBy(f => f.SortOrder))
            {
                lists.Add(new Item() { Name = f.Name, FolderID = f.ID, ID = Guid.Empty });
                var s = new Section() { new RadioElement(f.Name, f.Name) };
                // get all the lists in this folder except for the current list (if passed in)
                var folderlists = f.Items.
                    Where(li => li.IsList == true && li.ItemTypeID != SystemItemTypes.Reference && (list == null || li.ID != list.ID)).
                    OrderBy(li => li.Name).ToList();
                foreach (var l in folderlists)
                    lists.Add(l);
                var radioButtons = folderlists.Select(li => (Element) new RadioElement("        " + li.Name, f.Name)).ToList();
                s.AddAll(radioButtons);
                this.Add(s);
            };

            Item thisList = null;
            Guid listID = list != null && list.ParentID != null ? (Guid) list.ParentID : Guid.Empty;
            if (list != null && lists.Any(li => li.FolderID == list.FolderID && li.ID == listID))
                thisList = lists.First(li => li.FolderID == list.FolderID && li.ID == listID);

            this.RadioSelected = thisList != null ? Math.Max(lists.IndexOf(thisList, 0), 0) : 0;
        }

        public Guid SelectedFolderID 
        {
            get 
            {
                return lists[RadioSelected].FolderID;
            }
        }

        public Guid? SelectedParentID 
        {
            get 
            {
                var selected = lists[RadioSelected];
                return selected.ID == Guid.Empty ? (Guid?)null : selected.ID;
            }
        }
    }
}
