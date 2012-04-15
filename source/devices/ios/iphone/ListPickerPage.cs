using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;
using MonoTouch.Dialog;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class ListPickerPage 
	{
        private UINavigationController controller;
        private Item valueList;
        private Item pickList;
        private string caption;
        private RootElement root;
        private PropertyInfo pi;
        private object container;
        private StringElement stringElement;
        private List<Item> currentList;
        private Section section;
        private ItemPage itemPage;
		
        public ListPickerPage(ItemPage itemPage, UINavigationController c, StringElement stringElement, PropertyInfo pi, object container, string caption, Item valueList, Item pickFromList)
        {
            // trace event
            TraceHelper.AddMessage("ListPicker: constructor");
            this.controller = c;
            this.itemPage = itemPage;
            this.stringElement = stringElement;
            this.pi = pi;
            this.container = container;
            this.caption = caption;
            this.valueList = valueList;
            this.pickList = pickFromList;
        }
     
        public void PushViewController()
        {
            // trace event
            TraceHelper.AddMessage("ListPicker: PushViewController");
            
            // if the sublist hasn't been created, do so now
            if (valueList.ID == Guid.Empty)
            {
                Guid id = Guid.NewGuid();
                valueList.ID = id;                
                foreach (var it in valueList.Items)
                    it.ParentID = id;

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = valueList
                        });

                // add the list to the folder
                Folder folder = App.ViewModel.Folders.Single(f => f.ID == valueList.FolderID);
                folder.Items.Add(valueList);

                // store the list's Guid in the item's property 
                pi.SetValue(container, id.ToString(), null);

                // save the item change, which will queue up the update item operation
                itemPage.SaveButton_Click(null, null);
            }
         
            // build the current picker list and render it
            currentList = BuildCurrentList(valueList, pickList);
            root = RenderPicker(valueList, pickList);            
            var dvc = new DialogViewController (root, true);
            controller.PushViewController (dvc, true);
        }
			        
        #region Event Handlers
       
        private void Checkbox_Click(object sender, EventArgs e)
        {
            var element = sender as Element;
            bool isChecked;
            var cie = element as CheckboxImageElement;
            if (cie == null)
            {
                var ce = element as CheckboxElement;
                if (ce == null)
                    return;
                else
                    isChecked = ce.Value;
            }
            else
                isChecked = cie.Value;
        
            int index = section.Elements.IndexOf(element);
            if (index < 0)
                return;
            
            // get the clicked item
            Item currentItem = currentList[index];
            Folder folder = App.ViewModel.Folders.Single(f => f.ID == currentItem.FolderID);
            
            if (isChecked)
            {
                // add the clicked item to the value list
                valueList.Items.Add(currentItem);
                folder.Items.Add(currentItem);
                StorageHelper.WriteFolder(folder);
                
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = currentItem
                        });
            }
            else
            {
                // remove the clicked item from the value list
                valueList.Items.Remove(currentItem);
                folder.Items.Remove(currentItem);
                StorageHelper.WriteFolder(folder);

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                            Body = currentItem
                        });
            }
            
            // re-render the comma-delimited list in the Element that was passed in
            RenderCommaList(stringElement, valueList);
            stringElement.GetImmediateRootElement().Reload(stringElement, UITableViewRowAnimation.None);
        }
        
        #endregion
        
        #region Helpers
 
        private List<Item> BuildCurrentList(Item values, Item pickerValues)
        {
            // create a list starting with the picked values
            List<Item> curr = new List<Item>();
            foreach (var item in values.Items)
                curr.Add(item);           
            
            // add all the valid picker values excluding the already selected values
            foreach (var item in pickerValues.Items)
            {
                try
                {
                    // try to match the current item against the list of currently selected items
                    values.Items.Single(it => it.ItemTypeID == SystemItemTypes.Contact && it.ID == item.ItemRef ||
                                              it.ItemTypeID == SystemItemTypes.Reference && it.ItemRef == item.ItemRef);
                }
                catch
                {
                    // an exception indicates this item wasn't found - therefore we need to add it
                    curr.Add(item);
                }
            }            
            return curr;
        }
        
        private void NavigateBack()
        {
            // since we're in the edit page, we need to pop twice
            controller.PopViewControllerAnimated(false);
            controller.PopViewControllerAnimated(true);
            root.Dispose();
        }
        
        private void RenderCommaList(StringElement stringElement, Item values)
        {
            // build a comma-delimited list of names to display in the control
            List<string> names = values.Items.Select(it => it.Name).ToList();
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
            stringElement.Value = sb.ToString();           
        }

        private RootElement RenderPicker(Item values, Item pickerValues)
        {
            // build a list of selections excluding the already selected values
            Item selections = new Item(pickerValues);
            selections.Items = new ObservableCollection<Item>();
            foreach (var item in pickerValues.Items)
            {
                try
                {
                    // try to match the current item against the list of currently selected items
                    values.Items.Single(it => it.ItemRef == item.ItemRef);
                }
                catch
                {
                    // an exception indicates this item wasn't found - therefore we need to add it
                    selections.Items.Add(item);
                }
            }
            
            // build a section with all the items in the current list 
            section = new Section();
            foreach (var item in currentList)
            {
                CheckboxElement ce = new CheckboxElement(item.Name);
                // set the value to true if the current item is in the values list
                ce.Value = values.Items.IndexOf(item) < 0 ? false : true;
                ce.Tapped += delegate { Checkbox_Click(ce, new EventArgs()); };
                section.Add(ce);
            }
            
            return new RootElement(caption) { section };
        }
        
        #endregion
    }
}

