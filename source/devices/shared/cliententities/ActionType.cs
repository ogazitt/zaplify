using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class ActionType : INotifyPropertyChanged
    {
        public ActionType() { }

        public ActionType(ActionType action)
        {
            Copy(action);
        }

        public void Copy(ActionType obj)
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

        private int actionTypeID;
        /// <summary>
        /// ActionTypeID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int ActionTypeID
        {
            get
            {
                return actionTypeID;
            }
            set
            {
                if (value != actionTypeID)
                {
                    actionTypeID = value;
                    NotifyPropertyChanged("ActionTypeID");
                }
            }
        }

        private string fieldName;
        /// <summary>
        /// FieldName property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string FieldName
        {
            get
            {
                return fieldName;
            }
            set
            {
                if (value != fieldName)
                {
                    fieldName = value;
                    NotifyPropertyChanged("FieldName");
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

        private string actionName;
        /// <summary>
        /// ActionName property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string ActionName
        {
            get
            {
                return actionName;
            }
            set
            {
                if (value != actionName)
                {
                    actionName = value;
                    NotifyPropertyChanged("ActionName");
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