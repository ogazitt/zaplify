using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class Action : INotifyPropertyChanged
    {
        public Action() { }

        public Action(Action action)
        {
            Copy(action);
        }

        public void Copy(Action obj)
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

        private int actionID;
        /// <summary>
        /// ActionID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int ActionID
        {
            get
            {
                return actionID;
            }
            set
            {
                if (value != actionID)
                {
                    actionID = value;
                    NotifyPropertyChanged("ActionID");
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

        private string actionType;
        /// <summary>
        /// ActionType property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string ActionType
        {
            get
            {
                return actionType;
            }
            set
            {
                if (value != actionType)
                {
                    actionType = value;
                    NotifyPropertyChanged("ActionType");
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