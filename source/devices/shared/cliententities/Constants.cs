using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    public class Constants
    {
        public Constants() 
        {
        }

        public Constants(Constants constants)
        {
            Copy(constants);
        }

        public void Copy(Constants obj)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                if (pi.CanWrite)
                {
                    var val = pi.GetValue(obj, null);
                    pi.SetValue(this, val, null);
                }
            }
        }

        private ObservableCollection<ActionType> actionTypes;
        /// <summary>
        /// ActionTypes collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<ActionType> ActionTypes
        {
            get
            {
                return actionTypes;
            }
            set
            {
                if (value != actionTypes)
                {
                    actionTypes = value;
                    NotifyPropertyChanged("ActionTypes");
                }
            }
        }

        private ObservableCollection<Color> colors;
        /// <summary>
        /// Colors collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<Color> Colors
        {
            get
            {
                return colors;
            }
            set
            {
                if (value != colors)
                {
                    colors = value;
                    NotifyPropertyChanged("Colors");
                }
            }
        }

        private ObservableCollection<FieldType> fieldTypes;
        /// <summary>
        /// Field Types collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<FieldType> FieldTypes
        {
            get
            {
                return fieldTypes;
            }
            set
            {
                if (value != fieldTypes)
                {
                    fieldTypes = value;
                    NotifyPropertyChanged("FieldTypes");
                }
            }
        }

        private ObservableCollection<ItemType> itemTypes;
        /// <summary>
        /// List Types collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<ItemType> ItemTypes
        {
            get
            {
                return itemTypes;
            }
            set
            {
                if (value != itemTypes)
                {
                    itemTypes = value;
                    NotifyPropertyChanged("ItemTypes");
                }
            }
        }

        private ObservableCollection<Permission> permissions;
        /// <summary>
        /// Permissions collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<Permission> Permissions
        {
            get
            {
                return permissions;
            }
            set
            {
                if (value != permissions)
                {
                    permissions = value;
                    NotifyPropertyChanged("Permissions");
                }
            }
        }

        private ObservableCollection<Priority> priorities;
        /// <summary>
        /// Priorities collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<Priority> Priorities
        {
            get
            {
                return priorities;
            }
            set
            {
                if (value != priorities)
                {
                    priorities = value;
                    NotifyPropertyChanged("Priorities");
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