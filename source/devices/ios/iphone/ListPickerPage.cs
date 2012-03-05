using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Text;

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
		
        public ListPickerPage(UINavigationController c, StringElement stringElement, PropertyInfo pi, object container, string caption, Item valueList, Item pickFromList)
        {
            // trace event
            TraceHelper.AddMessage("ListPicker: constructor");
            controller = c;
            this.caption = caption;
            this.valueList = valueList;
            this.pickList = pickFromList;
            this.pi = pi;
            this.container = container;
            this.stringElement = stringElement;
        }
     
        public void PushViewController()
        {
            // trace event
            TraceHelper.AddMessage("ListPicker: PushViewController");
         
            // build the current picker list and render it
            currentList = BuildCurrentList(valueList, pickList);
            root = RenderPicker(valueList, pickList);            
            var dvc = new DialogViewController (root, true);
         
            // push the "view item" view onto the nav stack
            controller.PushViewController (dvc, true);
        }
			        
        #region Event Handlers

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("ListPicker: Navigate back");

            // Navigate back to the list page
            NavigateBack();
        }
       
        private void Checkbox_Click(object sender, EventArgs e)
        {
            var booleanImage = sender as BooleanImageElement;
            if (booleanImage == null)
                return;
            
            bool isChecked = booleanImage.Value;
            int index = section.Elements.IndexOf(booleanImage);
            if (index < 0)
                return;
            
            // get the clicked item
            Item currentItem = currentList[index];
            
            if (isChecked)
            {
                // add the clicked item to the value list
                valueList.Items.Add(currentItem);
            }
            else
            {
                // remove the clicked item from the value list
                valueList.Items.Remove(currentItem);
            }
            
            // re-render the comma-delimited list in the Element that was passed in
            RenderCommaList(stringElement, valueList);
            stringElement.GetImmediateRootElement().Reload(stringElement, UITableViewRowAnimation.None);
        }
        
        private void SaveButton_Click(object sender, EventArgs e)
        {
            /*
            // update the LastModified timestamp
            itemCopy.LastModified = DateTime.UtcNow;
                     
            // remove any NEW FieldValues (i.e. ones which didn't exist on the original item) 
            // which contain null Values (we don't want to burden the item with
            // extraneous null fields)
            List<FieldValue> fieldValues = new List<FieldValue>(itemCopy.FieldValues);
            foreach (var fv in fieldValues)
                if (fv.Value == null && (thisItem == null || thisItem.GetFieldValue(fv.FieldID, false) == null))
                    itemCopy.FieldValues.Remove(fv);                       

            // if this is a new item, create it
            if (thisItem == null)
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                        {
                            ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                            Body = itemCopy
                        });

                // add the item to the local itemType
                folder.Items.Add(itemCopy);
                thisItem = itemCopy;
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { thisItem, itemCopy },
                        BodyTypeName = "Item",
                        ID = thisItem.ID
                    });

                // save the changes to the existing item
                int index = IndexOf(folder, thisItem);
                if (index < 0)
                    return; 
                folder.Items[index] = itemCopy;
                thisItem = itemCopy;
            }
            
            // save the changes to local storage
            //StorageHelper.WriteFolders(App.ViewModel.Folders);
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // signal the folder that the FirstDue property needs to be recomputed
            folder.NotifyPropertyChanged("FirstDue");
             */
            // trace page navigation
            TraceHelper.StartMessage("ListPicker: Navigate back");

            // Navigate back to the list page
            NavigateBack();
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
                if (values.Items.IndexOf(item) < 0)
                    curr.Add(item);   
            
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
            UIImage checkedImage = UIImage.FromFile("Images/checkbox.on.png");
            UIImage uncheckedImage = UIImage.FromFile("Images/checkbox.off.png");
            
            // build a list of selections excluding the already selected values
            Item selections = new Item(pickerValues);
            selections.Items = new ObservableCollection<Item>();
            foreach (var item in pickerValues.Items)
                if (values.Items.IndexOf(item) < 0)
                    selections.Items.Add(item);
            
            // build a section with all the selected items
            section = new Section();
            foreach (var item in values.Items)
            {
                BooleanImageElement bie = new BooleanImageElement(item.Name, true, checkedImage, uncheckedImage);
                bie.Tapped += delegate { Checkbox_Click(bie, new EventArgs()); };
                section.Add(bie);
            }
            
            // add elements for all the unselected items
            foreach (var item in selections.Items)
            {
                BooleanImageElement bie = new BooleanImageElement(item.Name, false, checkedImage, uncheckedImage);
                bie.Tapped += delegate { Checkbox_Click(bie, new EventArgs()); };
                section.Add(bie);
            }
            return new RootElement(caption) { section };
        }
        
        #endregion
    }
}

