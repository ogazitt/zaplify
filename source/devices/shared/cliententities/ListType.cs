using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class ListType : TaskStoreEntity, INotifyPropertyChanged
    {
        public ListType() : base() { }

        public ListType(ListType listType)
        {
            Copy(listType);
        }

        public void Copy(ListType obj)
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

        public static Guid ToDo = new Guid("14CDA248-4116-4E51-AC13-00096B43418C");
        public static Guid Shopping = new Guid("1788A0C4-96E8-4B95-911A-75E1519D7259");
        public static Guid Freeform = new Guid("dc1c6243-e510-4297-9df8-75babd237fbe");

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