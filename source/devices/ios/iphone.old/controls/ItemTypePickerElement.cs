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
    public class ItemTypePickerElement : ThemedRootElement 
    {    
        List<ItemType> itemTypes;

        public ItemTypePickerElement(string caption, Guid itemTypeID) : base(caption, new RadioGroup(null, 0)) 
        {
            itemTypes = App.ViewModel.ItemTypes.Where(i => i.UserID != SystemUsers.System).OrderBy(i => i.Name).ToList();
            ItemType thisItemType = itemTypes.FirstOrDefault(i => i.ID == itemTypeID);
            int selectedIndex = Math.Max(itemTypes.IndexOf(thisItemType), 0);
            var itemTypeSection = new Section();
            itemTypeSection.AddAll(from it in itemTypes select (Element) new RadioEventElement(it.Name));
            foreach (var e in itemTypeSection)
            {
                RadioEventElement ree = (RadioEventElement)e;
                ree.OnSelected += (sender, ea) =>
                {
                    var selected = OnSelected;
                    if (selected != null)
                        selected(this, EventArgs.Empty);
                };
            }
            this.Add(itemTypeSection);
            this.RadioSelected = selectedIndex;
        }

        public event EventHandler<EventArgs> OnSelected;

        public Guid SelectedItemType
        {
            get 
            {
                return itemTypes[RadioSelected].ID;
            }
        }
    }
}
