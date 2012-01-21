using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class ItemType : ZaplifyEntity, INotifyPropertyChanged
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

        // built-in items
        public static Guid ToDoItem = new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid ShoppingItem = new Guid("00000000-0000-0000-0000-000000000002");
        public static Guid FreeformItem = new Guid("00000000-0000-0000-0000-000000000003");
        public static Guid Contact = new Guid("00000000-0000-0000-0000-000000000004");
        public static List<Guid> BuiltInTypes = new List<Guid>() { ToDoItem, ShoppingItem, FreeformItem, Contact };

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
            if (itemTypeID == ToDoItem)
                return "ToDo";
            if (itemTypeID == ShoppingItem)
                return "Shopping Item";
            if (itemTypeID == FreeformItem)
                return "Freeform Item";
            return null;
        }

        private Guid id;
        /// <summary>
        /// ID property
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Name property
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Fields collection property
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Icon property
        /// </summary>
        /// <returns></returns>
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}