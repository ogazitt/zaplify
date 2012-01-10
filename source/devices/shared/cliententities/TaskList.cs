using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class TaskList : TaskStoreEntity, INotifyPropertyChanged
    {
        public TaskList() : base()
        {
            Tasks = new ObservableCollection<Task>();
        }

        public TaskList(TaskList list)
        {
            Copy(list, true);
        }

        public TaskList(TaskList list, bool deepCopy)
        {
            Copy(list, deepCopy);
        }

        public void Copy(TaskList obj, bool deepCopy)
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

            if (deepCopy)
            {
                // reinitialize the Tasks collection
                this.tasks = new ObservableCollection<Task>();
                foreach (Task t in obj.tasks)
                {
                    this.tasks.Add(new Task(t));
                }
            }
            else
            {
                this.tasks = new ObservableCollection<Task>();
            }

            NotifyPropertyChanged("Tasks");
        }

        public override string ToString()
        {
            return this.Name;
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

        private Guid listTypeID;
        /// <summary>
        /// ListType property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid ListTypeID
        {
            get
            {
                return listTypeID;
            }
            set
            {
                if (value != listTypeID)
                {
                    listTypeID = value;
                    NotifyPropertyChanged("ListTypeID");
                }
            }
        }

        private bool template;
        /// <summary>
        /// Template property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public bool Template
        {
            get
            {
                return template;
            }
            set
            {
                if (value != template)
                {
                    template = value;
                    NotifyPropertyChanged("Template");
                }
            }
        }

        private ObservableCollection<Task> tasks;
        /// <summary>
        /// Tasks collection property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<Task> Tasks
        {
            get
            {
                return tasks;
            }
            set
            {
                if (value != tasks)
                {
                    tasks = value;
                    NotifyPropertyChanged("Tasks");
                }
            }
        }

        static string FirstDueText = "next item due ";
        /// <summary>
        /// Returns the earliest date a task is due in this tasklist
        /// This property is used solely for databinding
        /// </summary>
        public string FirstDue
        {
            get
            {
                if (tasks == null)
                    return null;
                DateTime dt = DateTime.MinValue;
                foreach (var task in tasks)
                {
                    if (task.Complete == false && task.Due != null)
                    {
                        if (dt == DateTime.MinValue)
                        {
                            dt = (DateTime)task.Due;
                        }
                        else
                        {
                            if (task.Due < dt)
                                dt = (DateTime)task.Due;
                        }
                    }
                }
                if (dt > DateTime.MinValue)
                    return String.Format("{0}{1}", FirstDueText, dt.ToString("MMMM dd, yyyy"));
                else
                    return null;
            }
        }

        public string FirstDueColor
        {
            get
            {
                if (FirstDue == null)
                    return "White";

                string fdstr = FirstDue.Substring(FirstDueText.Length);
                DateTime dt = Convert.ToDateTime(fdstr);
                if (dt.Date < DateTime.Today.Date)
                    return "Red";
                if (dt.Date == DateTime.Today.Date)
                    return "Yellow";
                return "White";
            }
        }

        public int IncompleteCount
        {
            get
            {
                int i = 0;
                foreach (var t in Tasks)
                {
                    if (t.Complete == false)
                        i++;
                }
                return i;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}