using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class Task : TaskStoreEntity, INotifyPropertyChanged
    {
        public Task() : base()
        {
            created = DateTime.UtcNow;
            lastModified = DateTime.UtcNow;
            taskTags = new ObservableCollection<TaskTag>();
            tags = new ObservableCollection<Tag>();
        }

        public Task(Task task)
        {
            Copy(task);
        }

        public void Copy(Task obj)
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

        public void CreateTags(ObservableCollection<Tag> tagList)
        {
            var newTags = new ObservableCollection<Tag>();

            if (taskTags != null)
            {
                foreach (var taskTag in taskTags)
                {
                    try
                    {
                        var foundTag = tagList.Single<Tag>(t => t.ID == taskTag.TagID);
                        if (foundTag != null)
                        {
                            newTags.Add(foundTag);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            // store the tag collection (which will invoke setter and trigger databinding)
            //Tags = newTags;
            // don't trigger databinding
            tags = newTags;
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

        private bool complete;
        /// <summary>
        /// Complete property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public bool Complete
        {
            get
            {
                return complete;
            }
            set
            {
                if (value != complete)
                {
                    complete = value;
                    NotifyPropertyChanged("Complete");
                    NotifyPropertyChanged("NameDisplayColor");
                }
            }
        }

        private DateTime created;
        /// <summary>
        /// Created property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public DateTime Created
        {
            get
            {
                return created;
            }
            set
            {
                if (value != created)
                {
                    created = value;
                    NotifyPropertyChanged("Created");
                }
            }
        }

        private string description;
        /// <summary>
        /// Description property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                if (value != description)
                {
                    description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        private string dueDate;
        /// <summary>
        /// DueDate property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string DueDate
        {
            get
            {
                return dueDate;
            }
            set
            {
                if (value != dueDate)
                {
                    dueDate = value;
                    NotifyPropertyChanged("DueDate");
                    NotifyPropertyChanged("Due");
                    NotifyPropertyChanged("DueDisplay");
                    NotifyPropertyChanged("DueDisplayColor");
                    NotifyPropertyChanged("DueSort");
                }
            }
        }

        private string email;
        /// <summary>
        /// Email property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                if (value != email)
                {
                    email = value;
                    NotifyPropertyChanged("Email");
                }
            }
        }

        private DateTime lastModified;
        /// <summary>
        /// LastModified property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public DateTime LastModified
        {
            get
            {
                return lastModified;
            }
            set
            {
                if (value != lastModified)
                {
                    lastModified = value;
                    NotifyPropertyChanged("LastModified");
                }
            }
        }

        private Guid? linkedTaskListID;
        /// <summary>
        /// LinkedTaskListID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid? LinkedTaskListID
        {
            get
            {
                return linkedTaskListID;
            }
            set
            {
                if (value != linkedTaskListID)
                {
                    linkedTaskListID = value;
                    NotifyPropertyChanged("LinkedTaskListID");
                }
            }
        }

        private string location;
        /// <summary>
        /// Location property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Location
        {
            get
            {
                return location;
            }
            set
            {
                if (value != location)
                {
                    location = value;
                    NotifyPropertyChanged("Location");
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

        private string phone;
        /// <summary>
        /// Phone property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Phone
        {
            get
            {
                return phone;
            }
            set
            {
                if (value != phone)
                {
                    phone = value;
                    NotifyPropertyChanged("Phone");
                }
            }
        }

        private int? priorityId;
        /// <summary>
        /// PriorityID property (0 is low, 1 is regular, 2 is high)
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public int? PriorityID
        {
            get
            {
                return priorityId;
            }
            set
            {
                if (value != priorityId)
                {
                    priorityId = value;
                    NotifyPropertyChanged("PriorityID");
                    NotifyPropertyChanged("PriorityIDIcon");
                    NotifyPropertyChanged("PriorityIDSort");
                }
            }
        }

        private Guid taskListID;
        /// <summary>
        /// TaskListID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid TaskListID
        {
            get
            {
                return taskListID;
            }
            set
            {
                if (value != taskListID)
                {
                    taskListID = value;
                    NotifyPropertyChanged("TaskListID");
                }
            }
        }

        private ObservableCollection<TaskTag> taskTags;
        /// <summary>
        /// TaskTags collection
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<TaskTag> TaskTags
        {
            get
            {
                return taskTags;
            }
            set
            {
                if (value != taskTags)
                {
                    taskTags = value;
                    NotifyPropertyChanged("TaskTags");
                }
            }
        }

        private string website;
        /// <summary>
        /// Website property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Website
        {
            get
            {
                return website;
            }
            set
            {
                if (value != website)
                {
                    website = value;
                    NotifyPropertyChanged("Website");
                }
            }
        }

        #region DataBinding Properties

        // local-only properties used for databinding

        public DateTime? Due
        {
            get
            {
                return dueDate == null ? null : (DateTime?) Convert.ToDateTime(dueDate);
            }
            set
            {
                dueDate = (value == null) ? null : ((DateTime)value).ToString("yyyy/MM/dd");
                NotifyPropertyChanged("DueDate");
                NotifyPropertyChanged("Due");
                NotifyPropertyChanged("DueDisplay");
                NotifyPropertyChanged("DueDisplayColor");
                NotifyPropertyChanged("DueSort");
            }
        }

        // display property for Due
        public string DueDisplay { get { return Due == null ? null : String.Format("{0}", ((DateTime)Due).ToString("d")); } }

        // color property for Due
        public string DueDisplayColor
        {
            get
            {
                if (Due == null)
                    return null;
                
                // if the task is already completed, no need to alert past-due tasks
                if (complete == true)
                    return "#ffa0a0a0";

                // return red for past-due tasks, yellow for tasks due today, gray for tasks due in future
                DateTime dueDatePart = ((DateTime)Due).Date;
                if (dueDatePart < DateTime.Today.Date)
                    return "Red";
                if (dueDatePart == DateTime.Today.Date)
                    return "Yellow";
                return "#ffa0a0a0";
            }
        }

        // sort property for Due
        public DateTime DueSort { get { return Due == null ? DateTime.MaxValue : (DateTime)Due; } }

        // boolean property for LinkedTaskListID
        public bool LinkedTaskListIDBool { get { return linkedTaskListID == null ? false : true; } }

        // display color property for Name
        public string NameDisplayColor { get { return complete == true ? "Gray" : "White"; } }

        // display image for PriorityID
        public string PriorityIDIcon
        {
            get
            {
                if (priorityId == null)
                    return "/Images/priority.none.png";
                string priString = PriorityNames[(int)priorityId];
                switch (priString)
                {
                    case "Low":
                        return "/Images/priority.low.png";
                    case "Normal":
                        return "/Images/priority.none.png";
                    case "High":
                        return "/Images/priority.high.png";
                    default:
                        return "/Images/priority.none.png";
                }
            }
        }

        // sort property for PriorityID
        public int PriorityIDSort { get { return priorityId == null ? 1 : (int)priorityId; } }

        // hardcode some names and colors for priority values
        public static string[] PriorityNames = new string[] { "Low", "Normal", "High" };
        public static string[] PriorityColors = new string[] { "Green", "White", "Red" };

        // tags collection to databind to (TaskTags is the authoritative source)
        private ObservableCollection<Tag> tags;
        public ObservableCollection<Tag> Tags
        {
            get
            {
                return tags;
            }
            set
            {
                if (value != tags)
                {
                    tags = value;
                    NotifyPropertyChanged("Tags");
                }
            }
        }       

        #endregion

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