using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class FieldType : INotifyPropertyChanged
    {
        public FieldType() { }

        public FieldType(FieldType fieldType)
        {
            Copy(fieldType);
        }

        public void Copy(FieldType obj)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in obj.GetType().GetProperties())
            {
                // get the value of the property
                var val = pi.GetValue(obj, null);
                pi.SetValue(this, val, null);
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

        private string name;
        /// <summary>
        /// Name property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Name
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