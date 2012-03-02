using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class Color : INotifyPropertyChanged
    {
        public Color() { }

        public Color(Color color)
        {
            Copy(color);
        }

        public void Copy(Color obj)
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

        public override string ToString()
        {
            return this.Name;
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

        private string name;
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