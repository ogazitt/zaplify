using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.IO.IsolatedStorage;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Net;
using System.Linq;
using BuiltSteady.Zaplify.Devices.Utilities;
using System.Windows.Resources;
using System.Threading;
using Microsoft.Phone.Net.NetworkInformation;


namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            // retrieve the network type asynchronously since the property takes 20sec to retrieve
            //ThreadPool.QueueUserWorkItem(delegate { NetworkType = NetworkInterface.NetworkInterfaceType; });
            //NetworkInterfaceType type = new NetworkInterfaceList().Current.InterfaceType;
            //NetworkInterfaceSubType subtype = new NetworkInterfaceList().Current.InterfaceSubtype;
        }

        public bool retrievedConstants = false;

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public NetworkInterfaceType NetworkType { get; set; }

        #region Databound Properties

        private About about;
        /// <summary>
        /// About info for the app.  This comes from a resource file (About.xml) that is packaged
        /// with the app.
        /// </summary>
        /// <returns></returns>
        public About About
        {
            get
            {
                return about;
            }
            set
            {
                if (value != about)
                {
                    about = value;
                    NotifyPropertyChanged("About");
                }
            }
        }

        /// <summary>
        /// Databinding property for displaying whether we are connected or not
        /// </summary>
        public string ConnectedText { get { return LastNetworkOperationStatus == true ? "Connected" : "Not Connected"; } }

        public string ConnectedIcon { get { return LastNetworkOperationStatus == true ? "/Images/connected.true.png" : "/Images/connected.false.png"; } }

        private Constants constants;
        /// <summary>
        /// Constants for the application.  These have default values in the client app, but 
        /// these defaults are overridden by the service
        /// </summary>
        /// <returns></returns>
        public Constants Constants
        {
            get
            {
                return constants;
            }
            set
            {
                if (value != constants)
                {
                    constants = value;

                    // save the new Constants in isolated storage
                    StorageHelper.WriteConstants(constants);

                    // reset priority names and colors inside the Item static arrays
                    // these static arrays are the most convenient way to make databinding work
                    int i = 0;
                    foreach (var pri in constants.Priorities)
                    {
                        Item.PriorityNames[i] = pri.Name;
                        Item.PriorityColors[i++] = pri.Color;
                    }

                    // reset the ItemType static constants inside the ItemType type
                    try
                    {
                        ItemType.ToDo = constants.ItemTypes.Single(lt => lt.Name == "To Do List").ID;
                        ItemType.Shopping = constants.ItemTypes.Single(lt => lt.Name == "Shopping List").ID;
                        ItemType.Freeform = constants.ItemTypes.Single(lt => lt.Name == "Freeform List").ID;
                    }
                    catch (Exception)
                    {
                    }

                    NotifyPropertyChanged("Constants");
                }
            }
        }

        private Folder defaultFolder;
        /// <summary>
        /// Default item folder to add new items to
        /// </summary>
        /// <returns></returns>
        public Folder DefaultFolder
        {
            get
            {
                return defaultFolder;
            }
            set
            {
                if (value != defaultFolder)
                {
                    defaultFolder = value;

                    // never let the default folder be null
                    if (defaultFolder == null)
                    {
                        defaultFolder = folders[0];
                    }

                    // save the new default folder ID in isolated storage
                    StorageHelper.WriteDefaultFolderID(defaultFolder.ID);

                    NotifyPropertyChanged("DefaultFolder");
                }
            }
        }

        private bool lastNetworkOperationStatus;
        /// <summary>
        /// Status of last network operation (true == succeeded)
        /// </summary>
        /// <returns></returns>
        public bool LastNetworkOperationStatus
        {
            get
            {
                return lastNetworkOperationStatus;
            }
            set
            {
                if (value != lastNetworkOperationStatus)
                {
                    lastNetworkOperationStatus = value;
                    NotifyPropertyChanged("LastNetworkOperationStatus");
                    NotifyPropertyChanged("ConnectedText");
                    NotifyPropertyChanged("ConnectedIcon");
                }
            }
        }

        private Dictionary<Guid, Folder> folderDictionary;
        /// <summary>
        /// A dictionary of Folders
        /// </summary>
        public Dictionary<Guid, Folder> FolderDictionary
        {
            get
            {
                return folderDictionary;
            }
            set
            {
                if (value != folderDictionary)
                {
                    folderDictionary = value;
                    NotifyPropertyChanged("FolderDictionary");
                }
            }
        }

        private ObservableCollection<ItemType> itemTypes;
        /// <summary>
        /// A collection of List Types
        /// </summary>
        /// <returns></returns>
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

                    // save the new ItemTypes in isolated storage
                    StorageHelper.WriteItemTypes(itemTypes);

                    NotifyPropertyChanged("ItemTypes");
                }
            }
        }

        private Visibility networkOperationInProgress = Visibility.Collapsed;
        /// <summary>
        /// Whether a network operation is in progress (yes == Visible / no == Collapsed)
        /// </summary>
        /// <returns></returns>
        public Visibility NetworkOperationInProgress
        {
            get
            {
                return networkOperationInProgress;
            }
            set
            {
                if (value != networkOperationInProgress)
                {
                    networkOperationInProgress = value;
                    NotifyPropertyChanged("NetworkOperationInProgress");
                }
            }
        }

        private ObservableCollection<Tag> tags;
        /// <summary>
        /// A collection of Tags
        /// </summary>
        /// <returns></returns>
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

                    // save the new Tags in isolated storage
                    StorageHelper.WriteTags(tags);

                    NotifyPropertyChanged("Tags");
                }
            }
        }

        /// <summary>
        /// Items property for the MainViewModel, which is a collection of Item objects
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Item> Items
        {
            get
            {
                // create a concatenated folder of items. This will be used for items and tags views
                var newItems = new ObservableCollection<Item>();
                if (folders != null)
                {
                    foreach (Folder tl in folders)
                    {
                        if (tl.Items != null)
                            foreach (Item t in tl.Items)
                                newItems.Add(t);
                    }
                }
                return newItems;
            }
        }

        private ObservableCollection<Folder> folders;
        /// <summary>
        /// Folders property for the MainViewModel, which is a collection of Folder objects
        /// </summary>
        /// <returns></returns>
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

                    // do not allow a situation where there are no folders
                    if (folders == null || folders.Count == 0)
                    {
                        folders = new ObservableCollection<Folder>();
                        folders.Add(new Folder() { Name = "To Do", ItemTypeID = ItemType.ToDo });

                        // save the new folder collection
                        StorageHelper.WriteFolders(folders);

                        // enqueue the Web Request Record (with a new copy of the folder)
                        // need to create a copy because otherwise other items may be added to it
                        // and we want the record to have exactly one operation in it (create the folder)
                        RequestQueue.EnqueueRequestRecord(
                            new RequestQueue.RequestRecord()
                            {
                                ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                                Body = new Folder(folders[0])
                            });
                    }
                    else
                    {
                        // save the new folder collection
                        StorageHelper.WriteFolders(folders);
                    }

                    // try to find and refresh the default item folder
                    try
                    {
                        // try to obtain the default folder ID
                        Guid tlID;
                        if (defaultFolder != null)
                            tlID = defaultFolder.ID;
                        else
                            tlID = StorageHelper.ReadDefaultFolderID();
                        
                        // try to find the default folder by ID
                        var defaulttl = Folders.Single(tl => tl.ID == tlID);
                        if (defaulttl != null)
                            DefaultFolder = defaulttl;
                        else
                            DefaultFolder = Folders[0];
                    }
                    catch (Exception)
                    {
                        // just default to the first folder (which always exists)
                        DefaultFolder = Folders[0];
                    }

                    NotifyPropertyChanged("Folders");
                    NotifyPropertyChanged("Items");
                }
            }
        }

        private ObservableCollection<string> traceMessages;
        /// <summary>
        /// List of trace messages
        /// </summary>
        public ObservableCollection<string> TraceMessages
        {
            get
            {
                return traceMessages;
            }
            set
            {
                if (value != traceMessages)
                {
                    traceMessages = value;
                    NotifyPropertyChanged("TraceMessages");
                }
            }
        }

        private User user;
        /// <summary>
        /// User object corresponding to the authenticated user
        /// </summary>
        /// <returns></returns>
        public User User
        {
            get
            {
                return user;
            }
            set
            {
                if (value != user)
                {
                    user = value;

                    // save the new User credentiaions
                    StorageHelper.WriteUserCredentials(user);

                    NotifyPropertyChanged("User");
                }
            }
        }

        private ObservableCollection<ItemType> userItemTypes;
        /// <summary>
        /// A collection of User-defined List Types
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<ItemType> UserItemTypes
        {
            get
            {
                return userItemTypes;
            }
            set
            {
                if (value != userItemTypes)
                {
                    userItemTypes = value;

                    // reset the folder types collection to be the concatenation of the built-in and user-defined itemtypes
                    var itemtypes = new ObservableCollection<ItemType>();
                    foreach (ItemType l in Constants.ItemTypes)
                        itemtypes.Add(new ItemType(l));
                    foreach (ItemType l in userItemTypes)
                        itemtypes.Add(new ItemType(l));

                    // trigger setter for ItemTypes
                    ItemTypes = itemtypes;

                    NotifyPropertyChanged("UserItemTypes");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                //handler(this, new PropertyChangedEventArgs(propertyName));
                // do the below instead to avoid Invalid cross-thread access exception
                Deployment.Current.Dispatcher.BeginInvoke(() => { handler(this, new PropertyChangedEventArgs(propertyName)); });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get About data from the local resource
        /// </summary>
        public About GetAboutData()
        {
            // trace getting data
            TraceHelper.AddMessage("Get About Data");

            // get a stream to the about XML file 
            StreamResourceInfo aboutFile =
              Application.GetResourceStream(new Uri("/WinPhone;component/About.xml", UriKind.Relative));
            Stream stream = aboutFile.Stream;

            // deserialize the file
            DataContractSerializer dc = new DataContractSerializer(typeof(About));
            return (About) dc.ReadObject(stream);
        }

        /// <summary>
        /// Get Constants data from the Web Service
        /// </summary>
        public void GetConstants()
        {
            if (retrievedConstants == false)
            {
                // trace getting constants
                TraceHelper.AddMessage("Get Constants");

                WebServiceHelper.GetConstants(
                    User, 
                    new GetConstantsCallbackDelegate(GetConstantsCallback), 
                    new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressForGetConstantsCallback));
            }
        }

        /// <summary>
        /// Get User data from the Web Service
        /// </summary>
        public void GetUserData()
        {
            if (retrievedConstants == true)
            {
                // trace getting user data
                TraceHelper.AddMessage("Get User Data");

                WebServiceHelper.GetUser(
                    User, 
                    new GetUserDataCallbackDelegate(GetUserDataCallback),
                    new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
            }
        }

        /// <summary>
        /// Read application data from isolated storage
        /// </summary>
        public void LoadData()
        {
            // check if the data has already been loaded
            if (this.IsDataLoaded == true)
                return;

            // trace loading data
            TraceHelper.AddMessage("Load Data");

            // read the user credentials (can be null)
            this.User = StorageHelper.ReadUserCredentials();

            // read the folder types - and create them if they don't exist
            this.constants = StorageHelper.ReadConstants();
            if (this.constants == null)
            {
                this.Constants = InitializeConstants();
            }

            // read the folder types - and create them if they don't exist
            this.itemTypes = StorageHelper.ReadItemTypes();
            if (this.itemTypes == null)
            {
                this.ItemTypes = InitializeItemTypes();
            }

            // read the tags - and create them if they don't exist
            this.tags = StorageHelper.ReadTags();
            if (this.tags == null)
            {
                this.Tags = InitializeTags();
            }

            // create the FolderDictionary dictionary
            if (this.folderDictionary == null)
                this.FolderDictionary = new Dictionary<Guid, Folder>();

            // read the folders - and create it if it doesn't exist AND if the user credentials have never been set
            // note that this is the only instance where the property is assigned to 
            // we do this to initialize Items and DefaultFolder
            // we don't do it for other properties because assigning to the property also triggers a StorageHelper.Write call
            this.Folders = StorageHelper.ReadFolders();
            if (this.folders == null && (this.User == null || this.User.Synced == false))
            {
                // we don't want to create the "starter" data if we already have a sync relationship with the service
                this.Folders = InitializeFolders();
            }

            // create the tags collection (client-only property)
            if (folders != null)
            {
                foreach (Folder tl in folders)
                {
                    if (tl.Items != null)
                        foreach (Item t in tl.Items)
                            t.CreateTags(tags);
                }
            }

            this.IsDataLoaded = true;

            // trace finished loading data
            TraceHelper.AddMessage("Finished Load Data");
        }

        /// <summary>
        /// Read folder from isolated storage
        /// </summary>
        public Folder LoadFolder(Guid id)
        {
            Folder tl;
            if (this.FolderDictionary.TryGetValue(id, out tl))
                return tl;
            else
            {
                Folder folder = App.ViewModel.Folders.Single(l => l.ID == id);
                string name = String.Format("{0}-{1}", folder.Name, id.ToString());
                tl = StorageHelper.ReadFolder(name);
                if (tl != null)
                    this.FolderDictionary[id] = tl;
            }
            return tl;
        }

        /// <summary>
        /// Play the Request Queue
        /// </summary>
        public void PlayQueue()
        {
            // if user hasn't been set, we cannot sync with the service
            if (User == null)
                return;

            // peek at the first record 
            RequestQueue.RequestRecord record = RequestQueue.GetRequestRecord();
            // if the record is null, this means we've processed all the pending changes
            // in that case, retrieve the Service's (now authoritative) folder
            if (record == null)
            {
                // refresh the user data
                GetUserData();
                return;
            }

            // get type name for the record 
            string typename = record.BodyTypeName;

            // trace playing record
            TraceHelper.AddMessage(String.Format("Play Queue: {0} {1}", record.ReqType, typename));

            // invoke the appropriate web service call based on the record type
            switch (record.ReqType)
            {
                case RequestQueue.RequestRecord.RequestType.Delete:
                    switch (typename)
                    {
                        case "Tag":
                            WebServiceHelper.DeleteTag(
                                User, 
                                (Tag)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                        case "Item":
                            WebServiceHelper.DeleteItem(
                                User,
                                (Item)record.Body,
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));

                            break;
                        case "Folder":
                            WebServiceHelper.DeleteFolder(
                                User, 
                                (Folder)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                    }
                    break;
                case RequestQueue.RequestRecord.RequestType.Insert:
                    switch (typename)
                    {
                        case "Tag":
                            WebServiceHelper.CreateTag(
                                User, 
                                (Tag)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                        case "Item":
                            WebServiceHelper.CreateItem(
                                User, 
                                (Item)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                        case "Folder":
                            WebServiceHelper.CreateFolder(
                                User, 
                                (Folder)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                    }
                    break;
                case RequestQueue.RequestRecord.RequestType.Update:
                    switch (typename)
                    {
                        case "Tag":
                            WebServiceHelper.UpdateTag(
                                User, 
                                (List<Tag>)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                        case "Item":
                            WebServiceHelper.UpdateItem(
                                User, 
                                (List<Item>)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                        case "Folder":
                            WebServiceHelper.UpdateFolder(
                                User, 
                                (List<Folder>)record.Body, 
                                new PlayQueueCallbackDelegate(PlayQueueCallback),
                                new NetworkOperationInProgressCallbackDelegate(NetworkOperationInProgressCallback));
                            break;
                    }
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Main routine for performing a sync with the Service.  It will chain the following operations:
        ///     1.  Get Constants
        ///     2.  Play the record queue (which will daisy chain on itself)
        ///     3.  Retrieve the user data (itemtypes, folders, tags...)
        /// </summary>
        public void SyncWithService()
        {
            if (retrievedConstants == false)
            {
                GetConstants();
            }
            else
            {
                PlayQueue();
            }
        }

        #endregion

        #region Callbacks 

        public delegate void GetConstantsCallbackDelegate(Constants constants);
        private void GetConstantsCallback(Constants constants)
        {
            // trace callback
            TraceHelper.AddMessage(String.Format("Finished Get Constants: {0}", constants == null ? "null" : "success"));

            if (constants != null)
            {
                retrievedConstants = true;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    // no requests pending - we can use the Service constants as the authoritative ones
                    Constants = constants;

                    // reset priority names and colors inside the Item static arrays
                    // these static arrays are the most convenient way to make databinding work
                    int i = 0;
                    foreach (var pri in constants.Priorities)
                    {
                        Item.PriorityNames[i] = pri.Name;
                        Item.PriorityColors[i++] = pri.Color;
                    }

                    // Chain the PlayQueue call to drain the queue and retrieve the user data
                    PlayQueue();
                });
            }
        }

        public delegate void GetUserDataCallbackDelegate(User user);
        private void GetUserDataCallback(User user)
        {
            // trace callback
            TraceHelper.AddMessage(String.Format("Finished Get User Data: {0}", constants == null ? "null" : "success"));

            if (user != null)
            {
                // reset and save the user credentials
                user.Synced = true;
                User = user;

                // reset the user's folder types
                UserItemTypes = user.ItemTypes;

                // reset and save the user's tags
                Tags = user.Tags;

                // reset and save the user's folders
                Folders = user.Folders;

                // store the folders individually
                foreach (Folder tl in folders)
                {
                    // store the folder in the dictionary
                    FolderDictionary[tl.ID] = tl;

                    // create the tags collection (client-only property)
                    foreach (Item t in tl.Items)
                        t.CreateTags(tags);
                    
                    // save the folder in its own isolated storage file
                    StorageHelper.WriteFolder(tl);
                }
            }
        }

        public delegate void NetworkOperationInProgressCallbackDelegate(bool operationInProgress, bool? operationSuccessful);
        public void NetworkOperationInProgressCallback(bool operationInProgress, bool? operationSuccessful)
        {
            // signal whether the net operation is in progress or not
            NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);

            // if the operationSuccessful flag is null, no new data; otherwise, it signals the status of the last operation
            if (operationSuccessful != null)
                LastNetworkOperationStatus = (bool)operationSuccessful;
        }

        public void NetworkOperationInProgressForGetConstantsCallback(bool operationInProgress, bool? operationSuccessful)
        {
            // signal whether the net operation is in progress or not
            NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);
        }

        public delegate void PlayQueueCallbackDelegate(Object obj);
        private void PlayQueueCallback(object obj)
        {
            // trace callback
            TraceHelper.AddMessage(String.Format("Finished Play Queue: {0}", obj == null ? "null" : "success"));

            // dequeue the current record (which removes it from the queue)
            RequestQueue.RequestRecord record = RequestQueue.DequeueRequestRecord();

            // don't need to process the object since the folder will be refreshed at the end 
            // of the cycle anyway

            // since the operation was successful, continue to drain the queue
            PlayQueue();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Initialize application constants
        /// </summary>
        /// <returns></returns>
        private Constants InitializeConstants()
        {
            Constants constants = new Constants()
            {
                ActionTypes = new ObservableCollection<ActionType>(),
                Colors = new ObservableCollection<BuiltSteady.Zaplify.Devices.ClientEntities.Color>(),
                FieldTypes = new ObservableCollection<FieldType>(),
                Permissions = new ObservableCollection<Permission>(),
                Priorities = new ObservableCollection<Priority>()
            };

            // initialize actions
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 1, FieldName = "LinkedFolderID", DisplayName = "navigate", ActionName = "Navigate", SortOrder = 1 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 2, FieldName = "Due", DisplayName = "postpone", ActionName = "Postpone", SortOrder = 2 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 3, FieldName = "Due", DisplayName = "add reminder", ActionName = "AddToCalendar", SortOrder = 3 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 4, FieldName = "Location", DisplayName = "map", ActionName = "Map", SortOrder = 4 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 5, FieldName = "Phone", DisplayName = "call", ActionName = "Phone", SortOrder = 5 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 6, FieldName = "Phone", DisplayName = "text", ActionName = "TextMessage", SortOrder = 6 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 7, FieldName = "Website", DisplayName = "browse", ActionName = "Browse", SortOrder = 7 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 8, FieldName = "Email", DisplayName = "email", ActionName = "Email", SortOrder = 8 });

            // initialize colors
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 0, Name = "White" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 1, Name = "Blue" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 2, Name = "Brown" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 3, Name = "Green" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 4, Name = "Orange" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 5, Name = "Purple" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 6, Name = "Red" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 7, Name = "Yellow" });

            // initialize field types
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 1, Name = "Name", DisplayName = "Name", DisplayType = "String" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 2, Name = "Description", DisplayName = "Description", DisplayType = "String" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 3, Name = "PriorityID", DisplayName = "Priority", DisplayType = "Priority" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 4, Name = "Due", DisplayName = "Due", DisplayType = "Date" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 5, Name = "ItemTags", DisplayName = "Tags (separated by commas)", DisplayType = "TagList" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 6, Name = "Location", DisplayName = "Location", DisplayType = "Address" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 7, Name = "Phone", DisplayName = "Phone", DisplayType = "Phone" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 8, Name = "Website", DisplayName = "Website", DisplayType = "Website" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 9, Name = "Email", DisplayName = "Email", DisplayType = "Email" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 10, Name = "Complete", DisplayName = "Complete", DisplayType = "Boolean" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 11, Name = "Description", DisplayName = "Details", DisplayType = "TextBox" });
            constants.FieldTypes.Add(new FieldType() { FieldTypeID = 12, Name = "LinkedFolderID", DisplayName = "Link to another folder", DisplayType = "ListPointer" });

            // initialize permissions
            constants.Permissions.Add(new Permission() { PermissionID = 1, Name = "See" });
            constants.Permissions.Add(new Permission() { PermissionID = 2, Name = "Change" });
            constants.Permissions.Add(new Permission() { PermissionID = 3, Name = "Full" });


            // initialize priorities
            constants.Priorities.Add(new Priority() { PriorityID = 0, Name = "Low", Color = "Green" });
            constants.Priorities.Add(new Priority() { PriorityID = 1, Name = "Normal", Color = "White" });
            constants.Priorities.Add(new Priority() { PriorityID = 2, Name = "High", Color = "Red" });

            return constants;
        }

        /// <summary>
        /// Initialize default itemtypes 
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<ItemType> InitializeItemTypes()
        {
            ObservableCollection<ItemType> itemTypes = new ObservableCollection<ItemType>();

            ItemType itemType;

            // create the To Do folder type
            itemTypes.Add(itemType = new ItemType() { ID = ItemType.ToDo, Name = "To Do List", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("3F6F8964-FCCD-47C6-8595-FBB0D5CAB5C2"), FieldTypeID = 1 /* Name */, ItemTypeID = ItemType.ToDo, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("5B934DC3-983C-4F05-AA48-C26B43464BBF"), FieldTypeID = 2 /* Description */, ItemTypeID = ItemType.ToDo, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("8F96E751-417F-489E-8BE2-B9A2BABF05D1"), FieldTypeID = 3 /* PriorityID  */, ItemTypeID = ItemType.ToDo, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("5F33C018-F0ED-4C8D-AF96-5B5C4B78C843"), FieldTypeID = 4 /* Due */, ItemTypeID = ItemType.ToDo, IsPrimary = true, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("ea7a11ad-e842-40ea-8a50-987427e69845"), FieldTypeID = 5 /* Tags */, ItemTypeID = ItemType.ToDo, IsPrimary = true, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("F5391480-1675-4D5C-9F4B-0887227AFDA5"), FieldTypeID = 6 /* Location */, ItemTypeID = ItemType.ToDo, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("DA356E6E-A484-47A3-9C95-7618BCBB39EF"), FieldTypeID = 7 /* Phone */, ItemTypeID = ItemType.ToDo, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("82957B93-67D9-4E4A-A522-08D18B4B5A1F"), FieldTypeID = 8 /* Website */, ItemTypeID = ItemType.ToDo, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("261950F7-7FDA-4432-A280-D0373CC8CADF"), FieldTypeID = 9 /* Email */, ItemTypeID = ItemType.ToDo, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("1448b7e7-f876-46ec-8e5b-0b9a1de7ea74"), FieldTypeID = 12 /* LinkedFolderID */, ItemTypeID = ItemType.ToDo, IsPrimary = false, SortOrder = 10 });
            itemType.Fields.Add(new Field() { ID = new Guid("32EE3561-226A-4DAD-922A-9ED93099C457"), FieldTypeID = 10 /* Complete */, ItemTypeID = ItemType.ToDo, IsPrimary = false, SortOrder = 11 });

            // create the Shopping folder type
            itemTypes.Add(itemType = new ItemType() { ID = ItemType.Shopping, Name = "Shopping List", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("DEA2ECAD-1E53-4616-8EE9-C399D4223FFB"), FieldTypeID = 1 /* Name */, ItemTypeID = ItemType.Shopping, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("7E7EAEB4-562B-481C-9A38-AEE216B8B4A0"), FieldTypeID = 9 /* Complete */, ItemTypeID = ItemType.Shopping, IsPrimary = true, SortOrder = 2 });

            // create the Freeform folder type
            itemTypes.Add(itemType = new ItemType() { ID = ItemType.Freeform, Name = "Freeform List", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("1C01E1B0-C14A-4CE9-81B9-868A13AAE045"), FieldTypeID = 1 /* Name */, ItemTypeID = ItemType.Freeform, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("7FFD95DB-FE46-49B4-B5EE-2863938CD687"), FieldTypeID = 11 /* Details */, ItemTypeID = ItemType.Freeform, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("6B3E6603-3BAB-4994-A69C-DF0F4310FA95"), FieldTypeID = 3 /* PriorityID */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("2848AF68-26F7-4ABB-8B9E-1DA74EE4EC73"), FieldTypeID = 4 /* Due */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("9ebb9cba-277a-4462-b205-959520eb88c5"), FieldTypeID = 5 /* Tags */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("4054F093-3F7F-4894-A2C2-5924098DBB29"), FieldTypeID = 6 /* Location */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("8F0915DE-E77F-4B63-8B22-A4FF4AFC99FF"), FieldTypeID = 7 /* Phone  */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("9F9B9FDB-3403-4DCD-A139-A28487C1832C"), FieldTypeID = 8 /* Website */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("4E304CCA-561F-4CB3-889B-1F5D022C4364"), FieldTypeID = 9 /* Email */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("7715234d-a60e-4336-9af1-f05c36add1c8"), FieldTypeID = 12 /* LinkedFolderID */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 10 });
            itemType.Fields.Add(new Field() { ID = new Guid("FE0CFC57-0A1C-4E3E-ADD3-225E2C062DE0"), FieldTypeID = 10 /* Complete */, ItemTypeID = ItemType.Freeform, IsPrimary = false, SortOrder = 11 });

            return itemTypes;
        }

        /// <summary>
        /// Initialize default tags 
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<Tag> InitializeTags()
        {
            ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
           
            // no default tags - return empty collection
            return tags;
        }

        /// <summary>
        /// Initialize default folders
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<Folder> InitializeFolders()
        {
            ObservableCollection<Folder> folders = new ObservableCollection<Folder>();

            Folder folder;
            Item item;

            // create a To Do folder
            folders.Add(folder = new Folder() { Name = "To Do", ItemTypeID = ItemType.ToDo, Items = new ObservableCollection<Item>() });

            // add to the dictionary
            FolderDictionary.Add(folder.ID, folder);
            
            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = folder,
                    ID = folder.ID
                });

            // create the Welcome item
            folder.Items.Add(item = new Item() 
            { 
                Name = "Welcome to Zaplify!", 
                Description="Tap the browse button below to discover more about the Zaplify application.", 
                FolderID = folder.ID, 
                Due = DateTime.Today.Date,
                PriorityID = 0,
                Website = WebServiceHelper.BaseUrl + "/Home/WelcomeWP7" /*"/Content/Welcome.html"*/ 
            });

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = item,
                    ID = item.ID
                });

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // create a shopping folder
            folders.Add(folder = new Folder() { Name = "Shopping", ItemTypeID = ItemType.Shopping, Items = new ObservableCollection<Item>() });

            // add to the dictionary
            FolderDictionary.Add(folder.ID, folder);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);
            
            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = folder,
                    ID = folder.ID
                });

            return folders;
        }

        #endregion
    }
}