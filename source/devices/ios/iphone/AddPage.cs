using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class AddPage : UINavigationController
	{
		private MultilineEntryElement Name;
		private CheckboxElement IsList;
		private RootElement ItemTypeRadio;
		private RootElement ListRadio;
		private List<Item> lists;
		
		public AddPage()
		{
			// trace event
            TraceHelper.AddMessage("Add: constructor");

			this.Title = NSBundle.MainBundle.LocalizedString ("Add", "Add");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/180-stickynote.png");
		}
		
		public override void ViewDidAppear (bool animated)
		{
			// trace event
            TraceHelper.AddMessage("Settings: ViewDidAppear");
			
			// initialize controls 
			var pushToTalkButton = new ButtonListElement() 
			{ 
				new Button() 
				{ 
					Background = "Images/redbutton.png", 
					Caption = "Hold to speak", 
					Clicked = PushToTalk_Click 
				}, 
			};
			pushToTalkButton.Margin = 0f;
			
			var addButton = new ButtonListElement() 
			{ 
				new Button() 
				{ 
					Background = "Images/darkgreybutton.png", 
					Caption = "Add", 
					Clicked = AddButton_Click 
				}, 
			};
			addButton.Margin = 0f;
			
			Name = new MultilineEntryElement("Name", "") { Lines = 3 };
			IsList = new CheckboxElement ("List?", false); 
			ItemTypeRadio = new RootElement ("Type", new RadioGroup (0))
			{
				new Section ()
				{
					from it in App.ViewModel.ItemTypes
						select (Element) new RadioElement (it.Name)
				}
			};
			ListRadio = new RootElement("List", new RadioGroup(0))				
			{
		        from f in App.ViewModel.Folders
			        orderby f.Name ascending
			        group f by f.Name into g
			        select new Section (g.Key) 
					{
			            new RadioElement(g.Key),
						from hs in g 
							from it in App.ViewModel.Items 
								where it.FolderID == hs.ID && it.IsList == true 
								orderby it.Name ascending
			               		select (Element) new RadioElement(String.Format("{0} : {1}", hs.Name, it.Name))
					}
			};
			
			// save the lists for processing in the AddButton handler
			lists = new List<Item>();
			foreach (Folder f in App.ViewModel.Folders)
			{
				lists.Add(new Item() { ID = Guid.Empty, FolderID = f.ID });
				lists.AddRange(App.ViewModel.Items.Where (it => it.FolderID == f.ID && it.IsList == true).OrderBy(it => it.Name));
			}

			// create the dialog
			var root = new RootElement("Add Item")
			{
				new Section()
				{
					Name,
					IsList,
					ItemTypeRadio,
					ListRadio,
				},
				new Section()
				{
					addButton,
				},
				new Section()
				{
					pushToTalkButton
				},
			};
			
			// create and push the dialog view onto the nav stack
			var dvc = new DialogViewController(root);
			dvc.NavigationItem.HidesBackButton = true;	
			dvc.Title = NSBundle.MainBundle.LocalizedString ("Add", "Add");
			this.PushViewController(dvc, false);
			
			base.ViewDidAppear (animated);
		}
		
		private void PushToTalk()
		{
			MessageBox.Show ("clicked");
		}
				
		private void PushToTalk_Click(object sender, EventArgs e)
		{
			MessageBox.Show ("Push to talk");
		}
		
		private void AddButton_Click(object sender, EventArgs e)
		{
            string name = Name.Value;
            // don't add empty items
            if (name == null || name == "")
                return;
			
			// get the values for all the controls
            int itemTypeIndex = ItemTypeRadio.RadioSelected;
            if (itemTypeIndex < 0)
            {
                MessageBox.Show("item type must be set");
                return;
			}
			int listIndex = ListRadio.RadioSelected;
            if (listIndex < 0)
            {
                MessageBox.Show("list must be set");
                return;
            }
            bool isChecked = IsList.Value;
			
			Item list = lists[listIndex];
			Folder folder = App.ViewModel.Folders.Single(f => f.ID == list.FolderID);

            // create the new item
            Item item = new Item()
            {
                Name = name,
                FolderID = folder.ID,
                ItemTypeID = App.ViewModel.ItemTypes[itemTypeIndex].ID,
                ParentID = list.ID,
                IsList = isChecked,
            };

            // hack: special-case processing for To Do item types
            // set the complete field to false 
            if (item.ItemTypeID == ItemType.ToDoItem)
                item.Complete = false;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = item
                });

            // add the new item to the list
            //ListHelper.AddItem(list, item);

            // add the item to the folder
            folder.Items.Add(item);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();		
			
			// refresh the view
			if (isChecked)
			{
				// we just added a new list - so refresh the whole view
				this.ViewDidAppear(true);
			}
			else
			{
				// just reset the name field
				Name.Value = "";
			}
		}
	}
}

