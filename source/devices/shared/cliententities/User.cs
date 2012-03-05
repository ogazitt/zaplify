using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract(Namespace = "")]
    public class User : ClientEntity, INotifyPropertyChanged
    {
        public User() : base() { }

        public User(User obj) 
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

        [DataMember]
        public override string Name
        {
            get
            {
                return Email;
            }
            set
            {
                Email = value;
            }
        }

        private string password;
        [DataMember]
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                if (value != password)
                {
                    password = value;
                    NotifyPropertyChanged("Password");
                }
            }
        }

        private string email;
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

        private ObservableCollection<Folder> folders;
        [DataMember]
        public ObservableCollection<Folder> Folders
        {
            get
            {
                return folders;
            }
            set
            {
                if (value != folders)
                {
                    folders = value;
                    NotifyPropertyChanged("Folders");
                }
            }
        }

        private ObservableCollection<ItemType> itemTypes;
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

        private ObservableCollection<Tag> tags;
        [DataMember]
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

        // local property
        public bool Synced { get; set; }

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