using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class Field : ZaplifyEntity, INotifyPropertyChanged
    {
        public Field() : base() { }

        public Field(Field field)
        {
            Copy(field);
        }

        public void Copy(Field obj)
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

        private int fieldTypeID;
        /// <summary>
        /// FieldTypeID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int FieldTypeID
        {
            get
            {
                return fieldTypeID;
            }
            set
            {
                if (value != fieldTypeID)
                {
                    fieldTypeID = value;
                    NotifyPropertyChanged("FieldTypeID");
                }
            }
        }

        private string displayName;
        /// <summary>
        /// DisplayName property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                if (value != displayName)
                {
                    displayName = value;
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        private string displayType;
        /// <summary>
        /// DisplayType property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string DisplayType
        {
            get
            {
                return displayType;
            }
            set
            {
                if (value != displayType)
                {
                    displayType = value;
                    NotifyPropertyChanged("DisplayType");
                }
            }
        }

        private Guid itemTypeID;
        /// <summary>
        /// ItemTypeID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid ItemTypeID
        {
            get
            {
                return itemTypeID;
            }
            set
            {
                if (value != itemTypeID)
                {
                    itemTypeID = value;
                    NotifyPropertyChanged("ItemTypeID");
                }
            }
        }

        private bool isPrimary;
        /// <summary>
        /// IsPrimary property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public bool IsPrimary
        {
            get
            {
                return isPrimary;
            }
            set
            {
                if (value != isPrimary)
                {
                    isPrimary = value;
                    NotifyPropertyChanged("IsPrimary");
                }
            }
        }

        private int sortOrder;
        /// <summary>
        /// SortOrder property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int SortOrder
        {
            get
            {
                return sortOrder;
            }
            set
            {
                if (value != sortOrder)
                {
                    sortOrder = value;
                    NotifyPropertyChanged("SortOrder");
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}