using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class TaskTag : TaskStoreEntity, INotifyPropertyChanged
    {
        public TaskTag() : base() { }

        public TaskTag(TaskTag taskTag)
        {
            Copy(taskTag);
        }

        public void Copy(TaskTag obj)
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

        private Guid taskID;
        /// <summary>
        /// TaskID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid TaskID
        {
            get
            {
                return taskID;
            }
            set
            {
                if (value != taskID)
                {
                    taskID = value;
                    NotifyPropertyChanged("TaskID");
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