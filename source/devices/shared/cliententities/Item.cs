using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class Item : ZaplifyEntity, INotifyPropertyChanged
    {
        public Item() : base()
        {
            created = DateTime.UtcNow;
            lastModified = DateTime.UtcNow;
            itemTags = new ObservableCollection<ItemTag>();
            tags = new ObservableCollection<Tag>();
        }

        public Item(Item item)
        {
            Copy(item);
        }

        public void Copy(Item obj)
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

            if (itemTags != null)
            {
                foreach (var itemTag in itemTags)
                {
                    try
                    {
                        var foundTag = tagList.Single<Tag>(t => t.ID == itemTag.TagID);
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

        private ObservableCollection<FieldValue> fieldValues;
        /// <summary>
        /// FieldValues collection
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<FieldValue> FieldValues
        {
            get
            {
                return fieldValues;
            }
            set
            {
                if (value != fieldValues)
                {
                    fieldValues = value;
                    NotifyPropertyChanged("FieldValues");
                }
            }
        }

        private Guid folderID;
        /// <summary>
        /// FolderID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid FolderID
        {
            get
            {
                return folderID;
            }
            set
            {
                if (value != folderID)
                {
                    folderID = value;
                    NotifyPropertyChanged("FolderID");
                }
            }
        }

        private ObservableCollection<Item> items;
        /// <summary>
        /// Items collection
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<Item> Items
        {
            get
            {
                return items;
            }
            set
            {
                if (value != items)
                {
                    items = value;
                    NotifyPropertyChanged("Items");
                }
            }
        }

        private ObservableCollection<ItemTag> itemTags;
        /// <summary>
        /// ItemTags collection
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<ItemTag> ItemTags
        {
            get
            {
                return itemTags;
            }
            set
            {
                if (value != itemTags)
                {
                    itemTags = value;
                    NotifyPropertyChanged("ItemTags");
                }
            }
        }

        private Guid itemTypeID;
        /// <summary>
        /// ItemType property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid ItemTypeID
        {
            get
            {
                return itemTypeID;
            }
            set
            {
                if (value != itemTypeID)
                {
                    itemTypeID = value;
                    NotifyPropertyChanged("ItemTypeID");
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

        private Guid? linkedFolderID;
        /// <summary>
        /// LinkedFolderID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid? LinkedFolderID
        {
            get
            {
                return linkedFolderID;
            }
            set
            {
                if (value != linkedFolderID)
                {
                    linkedFolderID = value;
                    NotifyPropertyChanged("LinkedFolderID");
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

        private Guid parentID;
        /// <summary>
        /// ParentID property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Guid ParentID
        {
            get
            {
                return parentID;
            }
            set
            {
                if (value != parentID)
                {
                    parentID = value;
                    NotifyPropertyChanged("ParentID");
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
                
                // if the item is already completed, no need to alert past-due items
                if (complete == true)
                    return "#ffa0a0a0";

                // return red for past-due items, yellow for items due today, gray for items due in future
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

        // boolean property for LinkedFolderID
        public bool LinkedFolderIDBool { get { return linkedFolderID == null ? false : true; } }

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

        // tags collection to databind to (ItemTags is the authoritative source)
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