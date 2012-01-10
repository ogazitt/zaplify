using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class Field : TaskStoreEntity, INotifyPropertyChanged
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

        private Guid listTypeID;
        /// <summary>
        /// ListTypeID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid ListTypeID
        {
            get
            {
                return listTypeID;
            }
            set
            {
                if (value != listTypeID)
                {
                    listTypeID = value;
                    NotifyPropertyChanged("ListTypeID");
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