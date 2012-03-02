using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class Tag : ClientEntity, INotifyPropertyChanged
    {
        public Tag() : base() 
        {
            colorID = 0;       
        }

        public Tag(Tag tag)
        {
            Copy(tag);
        }

        public void Copy(Tag obj)
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

        private int colorID;
        [DataMember]
        public int ColorID
        {
            get
            {
                return colorID;
            }
            set
            {
                if (value != colorID)
                {
                    colorID = value;
                    NotifyPropertyChanged("ColorID");
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