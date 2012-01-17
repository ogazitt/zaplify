using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

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
        public static Guid ToDoItem = new Guid("14CDA248-4116-4E51-AC13-00096B43418C");
        public static Guid ShoppingItem = new Guid("1788A0C4-96E8-4B95-911A-75E1519D7259");
        public static Guid FreeformItem = new Guid("dc1c6243-e510-4297-9df8-75babd237fbe");
        //public static Guid ToDoItem = new Guid("00000000-0000-0000-0000-000000000001");
        //public static Guid ShoppingItem = new Guid("00000000-0000-0000-0000-000000000002");
        //public static Guid FreeformItem = new Guid("00000000-0000-0000-0000-000000000003");

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