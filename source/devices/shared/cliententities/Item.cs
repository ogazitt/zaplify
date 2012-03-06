using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class Item : ClientEntity, INotifyPropertyChanged
    {
        public Item() : base()
        {
            DateTime now = DateTime.UtcNow;
            created = now;
            lastModified = now;
            fieldValues = new ObservableCollection<FieldValue>();
            items = new ObservableCollection<Item>();
            itemTags = new ObservableCollection<ItemTag>();
            tags = new ObservableCollection<Tag>();
        }

        public Item(Item item)
        {
            Copy(item, true);
        }

        public Item(Item item, bool deepCopy)
        {
            Copy(item, deepCopy);
        }

        public void Copy(Item obj, bool deepCopy)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                // only copy [DataMember] properties (which also must be writable)
                object[] attr = pi.GetCustomAttributes(false);
                if (attr != null && attr.Length > 0 && attr[0].GetType() == typeof(DataMemberAttribute) && 
                    pi.CanWrite)
                {
                    var val = pi.GetValue(obj, null);
                    pi.SetValue(this, val, null);
                }
            }

            if (deepCopy)
            {
                // reinitialize the Items collection
                this.items = new ObservableCollection<Item>();
                if (obj.items != null)
                    foreach (Item i in obj.items)
                        this.items.Add(new Item(i));

                // reinitialize the FieldValues collection
                this.fieldValues = new ObservableCollection<FieldValue>();
                if (obj.fieldValues != null)
                    foreach (FieldValue fv in obj.fieldValues)
                        this.fieldValues.Add(new FieldValue(fv));
            }
            else
            {
                this.items = new ObservableCollection<Item>();
            }

            NotifyPropertyChanged("Items");
        }

        public override string ToString()
        {
            return this.Name;
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

        private float sortOrder;
        [DataMember]
        public float SortOrder
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

        private DateTime created;
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

        private ObservableCollection<FieldValue> fieldValues;
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

        private bool isList;
        [DataMember]
        public bool IsList
        {
            get
            {
                return isList;
            }
            set
            {
                if (value != isList)
                {
                    isList = value;
                    NotifyPropertyChanged("IsList");
                }
            }
        }

        private ObservableCollection<ItemTag> itemTags;
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

        private string name;
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

        private Guid? parentID;
        [DataMember]
        public Guid? ParentID
        {
            get
            {
                return parentID;
            }
            set
            {
                if (value != parentID)
                {
                    if (value == Guid.Empty)
                        parentID = null;
                    else
                        parentID = value;
                    NotifyPropertyChanged("ParentID");
                }
            }
        }

        private Guid userID;
        [DataMember]
        public Guid UserID
        {
            get
            {
                return userID;
            }
            set
            {
                if (value != userID)
                {
                    userID = value;
                    NotifyPropertyChanged("UserID");
                }
            }
        }

        #region DataBinding Properties

        // dictionary to hold all the aliases to FieldValues
        private Dictionary<string, FieldValue> fieldValueDict = new Dictionary<string, FieldValue>();


        // get FieldValue of this Item by FieldName
        // if create == true, a new FieldValue for this FieldName will be created if none exists
        public FieldValue GetFieldValue(string fieldName, bool create)
        {
            // try to find the current item's itemtype (this should succeed)
            ItemType it;
            if (ItemType.ItemTypes.TryGetValue(this.ItemTypeID, out it) == false)
                return null;
            try
            {   // try to find the fieldName among the "supported" fields of the itemtype 
                // this may fail if this itemtype doesn't support this field name
                Field field = it.Fields.Single(f => f.Name == fieldName);

                // delegate to GetFieldValue(Guid fieldID)
                return GetFieldValue(field, create);
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        // get FieldValue of this Item by ID
        // if create == true, a new FieldValue with this ID will be created if none exists
        public FieldValue GetFieldValue(Guid fieldID, bool create)
        {
            // try to find the current item's itemtype (this should succeed)
            ItemType it;
            if (ItemType.ItemTypes.TryGetValue(this.ItemTypeID, out it) == false)
                return null;
            try
            {
                // try to find the fieldID among the "supported" fields of the itemtype 
                // this may fail if this itemtype doesn't support this field name
                Field field = it.Fields.Single(f => f.ID == fieldID);
                
                // delegate to GetFieldValue(Guid fieldID)
                return GetFieldValue(field, create);
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        // get FieldValue of this Item for the given Field
        // if create == true, a new FieldValue with for this Field will be created if none exists
        public FieldValue GetFieldValue(Field field, bool create)
        {
            FieldValue fieldValue = null;

            // initialize the dictionary if necessary
            if (fieldValueDict == null)
                fieldValueDict = new Dictionary<string, FieldValue>();

            // try to get the FieldValue out of the dictionary (if it was already retrieved)
            if (fieldValueDict.TryGetValue(field.Name, out fieldValue) == true)
                return fieldValue;

            // get the fieldvalue associated with this field
            // this may fail if this item doesn't have this field set yet
            try
            {
                fieldValue = fieldValues.Single(fv => fv.FieldID == field.ID);
                
                // store the new FieldValue in the dictionary
                fieldValueDict[field.Name] = fieldValue;
            }
            catch (Exception)
            {
                // if the caller wishes to create a new FieldValue, do so now
                if (create)
                {
                    fieldValue = new FieldValue()
                    {
                        FieldID = field.ID,
                        ItemID = this.ID,
                        Value = FieldTypes.DefaultValue(field.FieldType)
                    };

                    // store the new FieldValue in the dictionary
                    fieldValueDict[field.Name] = fieldValue;

                    if (fieldValues == null)
                        fieldValues = new ObservableCollection<FieldValue>();

                    // add the new FieldValue in the FieldValues collection
                    fieldValues.Add(fieldValue);
                }
            }

            return fieldValue;
        }

        // local-only properties used for databinding

        public bool? Complete
        {
            get
            {
                FieldValue fv = GetFieldValue("Complete", false);
                if (fv != null)
                    return (bool?)Convert.ToBoolean(fv.Value);
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("Complete", true);
                if (fv != null)
                {
                    fv.Value = (value == null) ? null : ((bool)value).ToString();
                    NotifyPropertyChanged("Complete");
                }
            }
        }

        public string Description
        {
            get
            {
                FieldValue fv = GetFieldValue("Description", false);
                if (fv != null)
                    return fv.Value;
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("Description", true);
                if (fv != null)
                {
                    fv.Value = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        public DateTime? Due
        {
            get
            {
                return DueDate == null ? null : (DateTime?) Convert.ToDateTime(DueDate);
            }
            set
            {
                DueDate = (value == null) ? null : ((DateTime)value).ToString("yyyy/MM/dd");
                NotifyPropertyChanged("DueDate");
                NotifyPropertyChanged("Due");
                NotifyPropertyChanged("DueDisplay");
                NotifyPropertyChanged("DueDisplayColor");
                NotifyPropertyChanged("DueSort");
            }
        }

        public string DueDate
        {
            get
            {
                FieldValue fv = GetFieldValue("DueDate", false);
                if (fv != null)
                    return fv.Value;
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("DueDate", true);
                if (fv != null)
                {
                    fv.Value = value;
                    NotifyPropertyChanged("DueDate");
                    NotifyPropertyChanged("Due");
                    NotifyPropertyChanged("DueDisplay");
                    NotifyPropertyChanged("DueDisplayColor");
                    NotifyPropertyChanged("DueSort");
                }
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
                if (Complete == true)
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

        public string Email
        {
            get
            {
                FieldValue fv = GetFieldValue("Email", false);
                if (fv != null)
                    return fv.Value;
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("Email", true);
                if (fv != null)
                {
                    fv.Value = value;
                    NotifyPropertyChanged("Email");
                }
            }
        }
        
        private ObservableCollection<Item> items;
        /// <summary>
        /// Items collection
        /// </summary>
        /// <returns></returns>
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

        // boolean property for LinkedFolderID
        //public bool LinkedFolderIDBool { get { return linkedFolderID == null ? false : true; } }

        public string Location
        {
            get
            {
                FieldValue fv = GetFieldValue("Location", false);
                if (fv != null)
                    return fv.Value;
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("Location", true);
                if (fv != null)
                {
                    fv.Value = value;
                    NotifyPropertyChanged("Location");
                }
            }
        }

        // display color property for Name
        public string NameDisplayColor { get { return Complete == true ? "Gray" : "White"; } }

        public int? Priority
        {
            get
            {
                FieldValue fv = GetFieldValue("Priority", false);
                if (fv != null)
                    return (int?)Convert.ToInt32(fv.Value);
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("Priority", true);
                if (fv != null)
                {
                    fv.Value = (value == null) ? null : ((int)value).ToString();
                    NotifyPropertyChanged("Priority");
                }
            }
        }

        public string Phone
        {
            get
            {
                FieldValue fv = GetFieldValue("Phone", false);
                if (fv != null)
                    return fv.Value;
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("Phone", true);
                if (fv != null)
                {
                    fv.Value = value;
                    NotifyPropertyChanged("Phone");
                }
            }
        }

        // display image for Priority
        public string PriorityIcon
        {
            get
            {
                if (Priority == null)
                    return "/Images/priority.none.png";
                string priString = PriorityNames[(int)Priority];
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

        // sort property for Priority
        public int PrioritySort { get { return Priority == null ? 1 : (int)Priority; } }

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

        public string Website
        {
            get
            {
                FieldValue fv = GetFieldValue("WebLink", false);
                if (fv != null)
                    return fv.Value;
                else
                    return null;
            }
            set
            {
                FieldValue fv = GetFieldValue("WebLink", true);
                if (fv != null)
                {
                    fv.Value = value;
                    NotifyPropertyChanged("Website");
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