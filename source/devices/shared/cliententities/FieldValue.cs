using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class FieldValue : ZaplifyEntity, INotifyPropertyChanged
    {
        public FieldValue() : base() { }

        public FieldValue(FieldValue fieldValue)
        {
            Copy(fieldValue);
        }

        public void Copy(FieldValue obj)
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

        private Guid fieldID;
        /// <summary>
        /// FieldID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid FieldID
        {
            get
            {
                return fieldID;
            }
            set
            {
                if (value != fieldID)
                {
                    fieldID = value;
                    NotifyPropertyChanged("FieldID");
                }
            }
        }

        private Guid itemID;
        /// <summary>
        /// ItemID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid ItemID
        {
            get
            {
                return itemID;
            }
            set
            {
                if (value != itemID)
                {
                    itemID = value;
                    NotifyPropertyChanged("ItemID");
                }
            }
        }

        private string val;
        /// <summary>
        /// Value property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Value
        {
            get
            {
                return val;
            }
            set
            {
                if (value != val)
                {
                    val = value;
                    NotifyPropertyChanged("Value");
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