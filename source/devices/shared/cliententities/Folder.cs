using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class Folder : ZaplifyEntity, INotifyPropertyChanged
    {
        public Folder() : base()
        {
            Items = new ObservableCollection<Item>();
        }

        public Folder(Folder folder)
        {
            Copy(folder, true);
        }

        public Folder(Folder folder, bool deepCopy)
        {
            Copy(folder, deepCopy);
        }

        public void Copy(Folder obj, bool deepCopy)
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
                // reinitialize the Items collection
                this.items = new ObservableCollection<Item>();
                foreach (Item t in obj.items)
                {
                    this.items.Add(new Item(t));
                }
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

        private ObservableCollection<Item> items;
        /// <summary>
        /// Items collection property
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

        static string FirstDueText = "next item due ";
        /// <summary>
        /// Returns the earliest date a item is due in this folder
        /// This property is used solely for databinding
        /// </summary>
        public string FirstDue
        {
            get
            {
                if (items == null)
                    return null;
                DateTime dt = DateTime.MinValue;
                foreach (var item in items)
                {
                    if (item.Complete != true && item.Due != null)
                    {
                        if (dt == DateTime.MinValue)
                        {
                            dt = (DateTime)item.Due;
                        }
                        else
                        {
                            if (item.Due < dt)
                                dt = (DateTime)item.Due;
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
                foreach (var item in Items)
                {
                    if (item.Complete == false)
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