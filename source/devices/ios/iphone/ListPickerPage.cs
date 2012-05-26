using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using MonoTouch.AddressBookUI;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class ListPickerPage 
	{
        private UINavigationController controller;
        private DialogViewController dvc;
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
                // fix the pickList's ParentID's to this new list ID (otherwise they stay Guid.Empty)
                foreach (var i in pickList.Items)
                    i.ParentID = id;
                
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
                StorageHelper.WriteFolder(folder);

                // store the list's Guid in the item's property 
                pi.SetValue(container, id.ToString(), null);
    
                // save the item change, which will queue up the update item operation
                //itemPage.SaveButton_Click(null, null);
            }
         
            // build the current picker list and render it
            currentList = BuildCurrentList(valueList, pickList);
            root = RenderPicker(valueList, pickList);            
            dvc = new DialogViewController (root, true);
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
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
                if (!values.Items.Any(it => it.ItemTypeID == SystemItemTypes.Contact && it.ID == item.ItemRef ||
                                      it.ItemTypeID == SystemItemTypes.Reference && it.ItemRef == item.ItemRef))
                    curr.Add(item);
            }            
            return curr;
        }

        private void HandleAddedContact(Item contact)
        {
            // if this contact was found and already selected, nothing more to do (and don't need to refresh the display)
            if (valueList.Items.Any(i => i.ItemRef == contact.ID))
                return;

            // create a new itemref 
            var itemRef = new Item()
            {
                Name = contact.Name,
                FolderID = valueList.FolderID, 
                ItemTypeID = SystemItemTypes.Reference, 
                ParentID = valueList.ID, 
                ItemRef = contact.ID
            };

            // add the itemref to the folder
            Folder folder = App.ViewModel.Folders.FirstOrDefault(f => f.ID == valueList.FolderID);
            if (folder == null)
                return;
            folder.Items.Add(itemRef);

            // save the current state of the folder
            StorageHelper.WriteFolder(folder);

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = itemRef,
                });

            // add the new item reference to the selected value list
            valueList.Items.Add(itemRef);

            // build the current picker list and render it
            currentList = BuildCurrentList(valueList, pickList);
            root = RenderPicker(valueList, pickList);            
            dvc.Root = root;
            dvc.TableView.ReloadData();

            // re-render the comma-delimited list in the Element that was passed in
            RenderCommaList(stringElement, valueList);
            stringElement.GetImmediateRootElement().Reload(stringElement, UITableViewRowAnimation.None);
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
            
            return new RootElement(caption) 
            {
                new Section()
                {
                    new StringElement("Add contact", delegate {
                        var picker = new ABPeoplePickerNavigationController();
                        picker.SelectPerson += delegate(object sender, ABPeoplePickerSelectPersonEventArgs e) {
                            // process the contact - if it's not null, handle adding the new contact to the ListPicker
                            var contact = ContactPickerHelper.ProcessContact(e.Person);
                            if (contact != null)
                                HandleAddedContact(contact);
                            picker.DismissModalViewControllerAnimated(true);
                        };

                        picker.Cancelled += delegate {
                            picker.DismissModalViewControllerAnimated(true);
                        };

                        // present the contact picker
                        controller.PresentModalViewController(picker, true);
                    }),
                },
                section 
            };
        }

        #endregion
    }
}

