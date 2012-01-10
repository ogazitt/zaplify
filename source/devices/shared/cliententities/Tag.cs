using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class Tag : TaskStoreEntity, INotifyPropertyChanged
    {
        public Tag() : base() 
        {
            // default the tag to White
            color = "White";
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

        private string color;
        /// <summary>
        /// Color property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Color
        {
            get
            {
                return color;
            }
            set
            {
                if (value != color)
                {
                    color = value;
                    NotifyPropertyChanged("Color");
                }
            }
        }

        public static List<string> Colors = new List<string>()
        {
            "White",
            "Blue",
            "Brown",
            "Green",
            "Orange",
            "Purple",
            "Red",
            "Yellow",
        };

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