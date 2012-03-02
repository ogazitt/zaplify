using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class ItemTag : ClientEntity, INotifyPropertyChanged
    {
        public ItemTag() : base() { }

        public ItemTag(ItemTag itemTag)
        {
            Copy(itemTag);
        }

        public void Copy(ItemTag obj)
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

        private Guid tagID;
        /// <summary>
        /// TagID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid TagID
        {
            get
            {
                return tagID;
            }
            set
            {
                if (value != tagID)
                {
                    tagID = value;
                    NotifyPropertyChanged("TagID");
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