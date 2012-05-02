using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;

using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class ItemType : ClientEntity, INotifyPropertyChanged
    {
        public ItemType() : base() { }

        public ItemType(ItemType itemType)
        {
            Copy(itemType);
        }

        public void Copy(ItemType obj)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                var val = pi.GetValue(obj, null);
                pi.SetValue(this, val, null);
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

        // static dictionary of current item types
        public static Dictionary<Guid, ItemType> ItemTypes { get; set; }
        public static void CreateDictionary(ObservableCollection<ItemType> itemTypes)
        {
            if (ItemTypes == null)
                ItemTypes = new Dictionary<Guid,ItemType>();
            ItemTypes.Clear();
            foreach (var it in itemTypes)
                ItemTypes.Add(it.ID, it);
        }

        public static string ItemTypeName(Guid itemTypeID)
        {
            if (ItemTypes != null)
            {
                ItemType itemType = null;
                if (ItemTypes.TryGetValue(itemTypeID, out itemType))
                    return itemType.Name;
            }

            // last resort
            if (itemTypeID == SystemItemTypes.Task)
                return "Task";
            if (itemTypeID == SystemItemTypes.Location)
                return "Location";
            if (itemTypeID == SystemItemTypes.Contact)
                return "Contact";
            if (itemTypeID == SystemItemTypes.ListItem)
                return "ListItem";
            if (itemTypeID == SystemItemTypes.ShoppingItem)
                return "ShoppingItem";
            if (itemTypeID == SystemItemTypes.Reference)
                return "Reference";
            if (itemTypeID == SystemItemTypes.NameValue)
                return "NameValue";
            return null;
        }

        private Guid id;
        [DataMember]
        public override Guid ID
        {
            get
            {
                return id;
            }
            set
            {
                if (value != id)
                {
                    id = value;
                    NotifyPropertyChanged("ID");
                }
            }
        }

        private string name;
        [DataMember]
        public override string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (value != name)
                {
                    name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private List<Field> fields;
        [DataMember]
        public List<Field> Fields
        {
            get
            {
                return fields;
            }
            set
            {
                if (value != fields)
                {
                    fields = value;
                    NotifyPropertyChanged("Fields");
                }
            }
        }

        private string icon;
        [DataMember]
        public string Icon
        {
            get
            {
                return icon;
            }
            set
            {
                if (value != icon)
                {
                    icon = value;
                    NotifyPropertyChanged("Icon");
                }
            }
        }

        private Guid? userID;
        [DataMember]
        public Guid? UserID
        {
            get
            {
                return userID;
            }
            set
            {
                if (value != userID)
                {
                    userID = value;
                    NotifyPropertyChanged("UserID");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool HasField(string fieldName)
        {
            foreach (Field f in this.Fields)
            {
                if (f.Name.Equals(fieldName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}