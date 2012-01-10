using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.ObjectModel;

namespace TaskStoreClientEntities
{
    [DataContract(Namespace = "")]
    public class About : INotifyPropertyChanged
    {
        public About() { }

        private string versionNumber;
        /// <summary>
        /// VersionNumber property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string VersionNumber
        {
            get
            {
                return versionNumber;
            }
            set
            {
                if (value != versionNumber)
                {
                    versionNumber = value;
                    NotifyPropertyChanged("VersionNumber");
                }
            }
        }

        private string developerInfo;
        /// <summary>
        /// DeveloperInfo property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string DeveloperInfo
        {
            get
            {
                return developerInfo;
            }
            set
            {
                if (value != developerInfo)
                {
                    developerInfo = value;
                    NotifyPropertyChanged("DeveloperInfo");
                }
            }
        }

        private string feedbackEmail;
        /// <summary>
        /// FeedbackEmail property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string FeedbackEmail
        {
            get
            {
                return feedbackEmail;
            }
            set
            {
                if (value != feedbackEmail)
                {
                    feedbackEmail = value;
                    NotifyPropertyChanged("FeedbackEmail");
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

        private ObservableCollection<Version> versions;
        /// <summary>
        /// Versions property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public ObservableCollection<Version> Versions
        {
            get
            {
                return versions;
            }
            set
            {
                if (value != versions)
                {
                    versions = value;
                    NotifyPropertyChanged("Versions");
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

    [DataContract(Namespace = "")]
    public class Version : INotifyPropertyChanged
    {
        public Version() { }

        private string number;
        /// <summary>
        /// Number property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Number
        {
            get
            {
                return number;
            }
            set
            {
                if (value != number)
                {
                    number = value;
                    NotifyPropertyChanged("Number");
                }
            }
        }

        private string details;
        /// <summary>
        /// Details property
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string Details
        {
            get
            {
                return details;
            }
            set
            {
                if (value != details)
                {
                    details = value;
                    NotifyPropertyChanged("Details");
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