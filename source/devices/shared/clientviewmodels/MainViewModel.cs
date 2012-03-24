using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Shared.Entities;

#if IOS
namespace System.Windows
{
    public enum Visibility { Visible = 0, Collapsed = 1 };
}
#endif

namespace BuiltSteady.Zaplify.Devices.ClientViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
        }

        public bool retrievedConstants = false;

        public bool IsDataLoaded
        {
            get;
            private set;
        }

#region // Databound Properties

        private About about;
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


        // Databinding property for displaying whether we are connected or not
        public string ConnectedText 
        { 
            get { return LastNetworkOperationStatus == true ? "Connected" : "Not Connected"; } 
        }

        public string ConnectedIcon 
        { 
            get { return LastNetworkOperationStatus == true ? "/Images/connected.true.png" : "/Images/connected.false.png"; } 
        }

        private Constants constants;
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

                    NotifyPropertyChanged("Constants");
                }
            }
        }

        private Folder defaultFolder;
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

        private ObservableCollection<Folder> folders;
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
                        folders.Add(new Folder() { Name = "Activities", ItemTypeID = SystemItemTypes.Task });

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

        private ObservableCollection<ItemType> itemTypes;
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

                    // (re)build the static ItemTypes dictionary
                    ItemType.CreateDictionary(itemTypes);

                    NotifyPropertyChanged("ItemTypes");
                }
            }
        }

        private Visibility networkOperationInProgress = Visibility.Collapsed;
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

        private ObservableCollection<string> traceMessages;
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
        public User User
        {
            get
            {
                return user;
            }
            set
            {   // the server does NOT send password back, so it will likely be null
                bool changed = false;
                if (value != user)
                {
                    changed = true;
                    if (user != null && value != null)
                    {
                        if (value.Name == user.Name && value.Email == user.Email && value.Password == null)
                        {   // for comparison purposes, a null password is NOT considered a change
                            changed = false;
                        }
                        if (value.Password == null)
                        {   // preserve local client password, as password is NOT sent back from server
                            value.Password = user.Password;
                        }
                    }
                }

                if (changed)
                {   // store changes locally and notify
                    user = value;
                    StorageHelper.WriteUserCredentials(user);
                    NotifyPropertyChanged("User");
                }
            }
        }

        private ObservableCollection<ItemType> userItemTypes;
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

                    // reset the item types collection to be the concatenation of the built-in and user-defined itemtypes
                    var itemtypes = new ObservableCollection<ItemType>();
                    foreach (ItemType l in Constants.ItemTypes)
                        itemtypes.Add(new ItemType(l));
                    foreach (ItemType l in userItemTypes)
                        itemtypes.Add(new ItemType(l));

                    // trigger setter for ItemTypes
                    ItemTypes = itemtypes;

                    NotifyPropertyChanged("ItemTypes");
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
#if IOS
                handler(this, new PropertyChangedEventArgs(propertyName));
#else
                // do the below instead to avoid Invalid cross-thread access exception
                Deployment.Current.Dispatcher.BeginInvoke(() => { handler(this, new PropertyChangedEventArgs(propertyName)); });
#endif
            }
        }
        
        /// <summary>
        /// The SyncComplete event fires when a SyncWithService cycle is complete
        /// The EventArgs is typed SyncCompleteEventArgs and the SyncCompleteArg
        /// in this class is populated with the SyncCompleteArg on this ViewModel
        /// </summary>
        public delegate void SyncCompleteEventHandler(object sender, SyncCompleteEventArgs ea);
        public class SyncCompleteEventArgs
        {
            public SyncCompleteEventArgs(object obj)
            {
                SyncCompleteArg = obj;
            }
            
            public object SyncCompleteArg { get; set; }
        }
        public event SyncCompleteEventHandler SyncComplete;
        public object SyncCompleteArg { get; set; }

#endregion

#region // Public Methods

        public About GetAboutData()
        {
            // trace getting data
            TraceHelper.AddMessage("Get About Data");

            // get a stream to the about XML file 
            Stream stream = AppResourcesHelper.GetResourceStream("About.xml");

            // deserialize the file
            DataContractSerializer dc = new DataContractSerializer(typeof(About));
            return (About) dc.ReadObject(stream);
        }

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

            // read the item types - and create them if they don't exist
            this.itemTypes = StorageHelper.ReadItemTypes();
            if (this.itemTypes == null)
            {
                this.ItemTypes = InitializeItemTypes();
            }
            else
            {
                // initialize the static ItemTypes dictionary
                ItemType.CreateDictionary(itemTypes);
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
            if (this.folders == null)
			{
				if (this.User == null || this.User.Synced == false)
	            {
	                // we don't want to create the "starter" data if we already have a sync relationship with the service
	                this.Folders = InitializeFolders();
	            }
				else
					this.folders = new ObservableCollection<Folder>();
			}

            // load the contents of each folder 
            List<Guid> guidList = Folders.Select(f => f.ID).ToList<Guid>();
            foreach (Guid guid in guidList)
                LoadFolder(guid);

            // create the tags and values collections (client-only properties)
            if (folders != null)
            {
                foreach (Folder f in folders)
                {
                    if (f.Items != null)
                        foreach (Item i in f.Items)
                            i.CreateTags(tags);
                }
            }

            this.IsDataLoaded = true;

            // trace finished loading data
            TraceHelper.AddMessage("Finished Load Data");
        }

        public Folder LoadFolder(Guid id)
        {
            Folder f;
            if (this.FolderDictionary.TryGetValue(id, out f))
                return f;
            else
            {
                Folder folder = Folders.Single(l => l.ID == id);
                string name = String.Format("{0}-{1}", folder.Name, id.ToString());
                f = StorageHelper.ReadFolder(name);
                if (f != null)
                {
                    this.FolderDictionary[id] = f;
                    int index = Folders.IndexOf(folder);
                    Folders[index] = f;
                    //NotifyPropertyChanged("Folders");
                    //NotifyPropertyChanged("Items");
                }
            }
            return f;
        }

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
        /// Recursively remove item and all possible sublists of an item
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(Item item)
        {
            try
            {
                var subitems = Items.Where(i => i.ParentID == item.ID).ToList();
                foreach (var it in subitems)
                    RemoveItem(it);
                Folder folder = Folders.Single(f => f.ID == item.ID);
                folder.Items.Remove(item);
                StorageHelper.WriteFolder(folder);                
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage("RemoveItem: exception; ex: " + ex.Message);
            }
        }

        // Main routine for performing a sync with the Service.  It will chain the following operations:
        //     1.  Get Constants
        //     2.  Play the record queue (which will daisy chain on itself)
        //     3.  Retrieve the user data (itemtypes, folders, tags...)
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

#region // Callbacks 

        public delegate void GetConstantsCallbackDelegate(Constants constants);
        private void GetConstantsCallback(Constants constants)
        {
            // trace callback
            TraceHelper.AddMessage(String.Format("Finished Get Constants: {0}", constants == null ? "null" : "success"));

            if (constants != null)
            {
                retrievedConstants = true;
				
#if !IOS
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
#endif
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

                    // initialize the static ItemTypes dictionary
                    ItemType.CreateDictionary(constants.ItemTypes);

                    // Chain the PlayQueue call to drain the queue and retrieve the user data
                    PlayQueue();
#if !IOS
                });
#endif
            }
            else
            {
                // refresh cycle interrupted - still need to signal the SyncComplete event if it was set
                if (SyncComplete != null)
                    SyncComplete(this, new SyncCompleteEventArgs(SyncCompleteArg));
                
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

                // reset the user's item types (which will also add the user's ItemTypes to the system ItemType list)
                UserItemTypes = user.ItemTypes;

                // reset and save the user's tags
                Tags = user.Tags;

                // reset and save the user's folders
                Folders = user.Folders;

                // remove existing folders
                foreach (Folder f in FolderDictionary.Values)
                    StorageHelper.DeleteFolder(f);
                FolderDictionary.Clear();

                // store the folders individually
                foreach (Folder f in folders)
                {                    
                    // store the folder in the dictionary
                    FolderDictionary[f.ID] = f;

                    // create the tags collection (client-only property)
                    foreach (Item i in f.Items)
                        i.CreateTags(tags);

                    // save the folder in its own isolated storage file
                    StorageHelper.WriteFolder(f);
                }
            }
            
            // invoke the SyncComplete event handler if it was set
            if (SyncComplete != null)
                SyncComplete(this, new SyncCompleteEventArgs(SyncCompleteArg));
        }

        public delegate void NetworkOperationInProgressCallbackDelegate(bool operationInProgress, OperationStatus status);
        public void NetworkOperationInProgressCallback(bool operationInProgress, OperationStatus status)
        {
            // signal whether the net operation is in progress or not
            NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);

            if (status != OperationStatus.Started)
            {
                LastNetworkOperationStatus = (status == OperationStatus.Success);
                
                // check for network operation failure
                if (LastNetworkOperationStatus == false)
                {
                    // refresh cycle interrupted - still need to signal the SyncComplete event if it was set
                    if (SyncComplete != null)
                        SyncComplete(this, new SyncCompleteEventArgs(SyncCompleteArg));                    
                }
            }
            
            if (status == OperationStatus.Retry)
            {   // allows failed network operations to try again
                this.SyncWithService();
            }
        }

        public void NetworkOperationInProgressForGetConstantsCallback(bool operationInProgress, OperationStatus status)
        {
            // signal whether the net operation is in progress or not
            NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);
        }

        public delegate void PlayQueueCallbackDelegate(Object obj);
        private void PlayQueueCallback(object obj)
        {
            // trace callback
            TraceHelper.AddMessage(String.Format("Play Queue Callback: {0}", obj == null ? "null" : "success"));
   
            // if the operation was successful, continue the refresh cycle
            if (obj != null)
            {
                // dequeue the current record (which removes it from the queue)
                RequestQueue.RequestRecord record = RequestQueue.DequeueRequestRecord();
    			
    			// parse out request record info and trace the details 
                string typename;
                string reqtype;
                string id;
                string name;
                RequestQueue.RetrieveRequestInfo(record, out typename, out reqtype, out id, out name);
    			TraceHelper.AddMessage(String.Format("Request details: {0} {1} {2} (id {3})", reqtype, typename, name, id));
    			
                // don't need to process the object since the folder will be refreshed at the end 
                // of the cycle anyway
    
                // since the operation was successful, continue to drain the queue
                PlayQueue();
            }
            else
            {
                // refresh cycle interrupted - still need to signal the SyncComplete event if it was set
                if (SyncComplete != null)
                    SyncComplete(this, new SyncCompleteEventArgs(SyncCompleteArg));
                
            }
        }

#endregion

#region // Helpers

        private Constants InitializeConstants()
        {
            Constants constants = new Constants()
            {
                ActionTypes = new ObservableCollection<ActionType>(),
                Colors = new ObservableCollection<BuiltSteady.Zaplify.Devices.ClientEntities.Color>(),
                Permissions = new ObservableCollection<Permission>(),
                Priorities = new ObservableCollection<Priority>()
            };

            // initialize actions
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 1, FieldName = FieldNames.DueDate, DisplayName = "postpone", ActionName = ActionNames.Postpone, SortOrder = 1 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 2, FieldName = FieldNames.ReminderDate, DisplayName = "add reminder", ActionName = ActionNames.AddToCalendar, SortOrder = 2 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 3, FieldName = FieldNames.Address, DisplayName = "map", ActionName = ActionNames.Map, SortOrder = 3 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 4, FieldName = FieldNames.Phone, DisplayName = "call", ActionName = ActionNames.Call, SortOrder = 4 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 5, FieldName = FieldNames.HomePhone, DisplayName = "call", ActionName = ActionNames.Call, SortOrder = 5 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 6, FieldName = FieldNames.WorkPhone, DisplayName = "call", ActionName = ActionNames.Call, SortOrder = 6 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 7, FieldName = FieldNames.Phone, DisplayName = "text", ActionName = ActionNames.TextMessage, SortOrder = 7 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 8, FieldName = FieldNames.WebLink, DisplayName = "browse", ActionName = ActionNames.Browse, SortOrder = 8 });
            constants.ActionTypes.Add(new ActionType() { ActionTypeID = 9, FieldName = FieldNames.Email, DisplayName = "email", ActionName = ActionNames.SendEmail, SortOrder = 9 });

            // initialize colors
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 0, Name = "White" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 1, Name = "Blue" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 2, Name = "Brown" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 3, Name = "Green" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 4, Name = "Orange" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 5, Name = "Purple" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 6, Name = "Red" });
            constants.Colors.Add(new BuiltSteady.Zaplify.Devices.ClientEntities.Color() { ColorID = 7, Name = "Yellow" });

            // initialize permissions
            constants.Permissions.Add(new Permission() { PermissionID = 1, Name = "View" });
            constants.Permissions.Add(new Permission() { PermissionID = 2, Name = "Modify" });
            constants.Permissions.Add(new Permission() { PermissionID = 3, Name = "Full" });

            // initialize priorities
            constants.Priorities.Add(new Priority() { PriorityID = 0, Name = "Low", Color = "Green" });
            constants.Priorities.Add(new Priority() { PriorityID = 1, Name = "Normal", Color = "White" });
            constants.Priorities.Add(new Priority() { PriorityID = 2, Name = "High", Color = "Red" });

            return constants;
        }

        private ObservableCollection<ItemType> InitializeItemTypes()
        {
            ObservableCollection<ItemType> itemTypes = new ObservableCollection<ItemType>();

            ItemType itemType;

            // create the Task
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Task, Name = "Task", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000011"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000012"), FieldType = FieldTypes.Integer, Name = FieldNames.Priority, DisplayName = "Priority", DisplayType = DisplayTypes.Priority, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000013"), FieldType = FieldTypes.DateTime, Name = FieldNames.DueDate, DisplayName = "Due", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000014"), FieldType = FieldTypes.DateTime, Name = FieldNames.ReminderDate, DisplayName = "Reminder", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Task, IsPrimary = true, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000015"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Details", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000016"), FieldType = FieldTypes.Url, Name = FieldNames.WebLink, DisplayName = "Website", DisplayType = DisplayTypes.Link, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000017"), FieldType = FieldTypes.ItemID, Name = FieldNames.Locations, DisplayName = "Location", DisplayType = DisplayTypes.LocationList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000018"), FieldType = FieldTypes.ItemID, Name = FieldNames.Contacts, DisplayName = "For", DisplayType = DisplayTypes.ContactList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 8 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000019"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", DisplayType = DisplayTypes.TagList, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 9 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-00000000001A"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.Task, IsPrimary = false, SortOrder = 10 });

           // create Location
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Location, Name = "Location", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000021"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000022"), FieldType = FieldTypes.Address, Name = FieldNames.Address, DisplayName = "Address", DisplayType = DisplayTypes.Address, ItemTypeID = SystemItemTypes.Location, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000023"), FieldType = FieldTypes.Phone, Name = FieldNames.Phone, DisplayName = "Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000024"), FieldType = FieldTypes.Url, Name = FieldNames.WebLink, DisplayName = "Website", DisplayType = DisplayTypes.Link, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000025"), FieldType = FieldTypes.Email, Name = FieldNames.Email, DisplayName = "Email", DisplayType = DisplayTypes.Email, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000026"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Description", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000027"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", DisplayType = DisplayTypes.TagList, ItemTypeID = SystemItemTypes.Location, IsPrimary = false, SortOrder = 7 });

            // create Contact
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.Contact, Name = "Contact", Icon = "contact.png", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000031"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000032"), FieldType = FieldTypes.Email, Name = FieldNames.Email, DisplayName = "Email", DisplayType = DisplayTypes.Email, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000033"), FieldType = FieldTypes.Phone, Name = FieldNames.Phone, DisplayName = "Mobile Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000034"), FieldType = FieldTypes.Phone, Name = FieldNames.HomePhone, DisplayName = "Home Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000035"), FieldType = FieldTypes.Phone, Name = FieldNames.WorkPhone, DisplayName = "Work Phone", DisplayType = DisplayTypes.Phone, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 5 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000036"), FieldType = FieldTypes.ItemID, Name = FieldNames.Locations, DisplayName = "Address", DisplayType = DisplayTypes.Address, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 6 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000037"), FieldType = FieldTypes.DateTime, Name = FieldNames.Birthday, DisplayName = "Birthday", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 7 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000038"), FieldType = FieldTypes.TagIDs, Name = FieldNames.ItemTags, DisplayName = "Tags", DisplayType = DisplayTypes.DatePicker, ItemTypeID = SystemItemTypes.Contact, IsPrimary = false, SortOrder = 8 });

            // create ListItem
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ListItem, Name = "ListItem", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000041"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000042"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000043"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Notes", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = false, SortOrder = 3 });

            // create ShoppingItem
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ShoppingItem, Name = "ShoppingItem", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000051"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000052"), FieldType = FieldTypes.Boolean, Name = FieldNames.Complete, DisplayName = "Complete", DisplayType = DisplayTypes.Checkbox, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 2 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000053"), FieldType = FieldTypes.String, Name = FieldNames.Amount, DisplayName = "Quantity", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = true, SortOrder = 3 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000054"), FieldType = FieldTypes.Currency, Name = FieldNames.Cost, DisplayName = "Price", DisplayType = DisplayTypes.Currency, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 4 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000055"), FieldType = FieldTypes.String, Name = FieldNames.Description, DisplayName = "Notes", DisplayType = DisplayTypes.TextArea, ItemTypeID = SystemItemTypes.ShoppingItem, IsPrimary = false, SortOrder = 5 });

            // create Reference
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ListItem, Name = "ListItem", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000061"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000062"), FieldType = FieldTypes.ItemID, Name = FieldNames.ItemRef, DisplayName = "Reference", DisplayType = DisplayTypes.Reference, ItemTypeID = SystemItemTypes.Reference, IsPrimary = true, SortOrder = 2 });

            // create NameValue
            itemTypes.Add(itemType = new ItemType() { ID = SystemItemTypes.ListItem, Name = "ListItem", Fields = new List<Field>() });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000071"), FieldType = FieldTypes.String, Name = FieldNames.Name, DisplayName = "Name", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.ListItem, IsPrimary = true, SortOrder = 1 });
            itemType.Fields.Add(new Field() { ID = new Guid("00000000-0000-0000-0000-000000000072"), FieldType = FieldTypes.String, Name = FieldNames.Value, DisplayName = "Value", DisplayType = DisplayTypes.Text, ItemTypeID = SystemItemTypes.NameValue, IsPrimary = true, SortOrder = 2 });

            return itemTypes;
        }

        private ObservableCollection<Tag> InitializeTags()
        {
            ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
           
            // no default tags - return empty collection
            return tags;
        }

        private ObservableCollection<Folder> InitializeFolders()
        {
            ObservableCollection<Folder> folders = new ObservableCollection<Folder>();

            Folder folder;
            Item item;

            // create the Activities folder
            folders.Add(folder = new Folder() { Name = "Activities", Items = new ObservableCollection<Item>(), ItemTypeID = SystemItemTypes.Task, SortOrder=1000 });
            FolderDictionary.Add(folder.ID, folder);
            
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord() { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = folder, ID = folder.ID });

            // create the Tasks list
            folder.Items.Add(item = new Item()
            {
                Name = "Tasks",
                FolderID = folder.ID,
                IsList = true,
                ItemTypeID = SystemItemTypes.Task,
                SortOrder = 1000,
                ParentID = Guid.Empty
            });

            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord() { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = item, ID = item.ID });

            // create the Learn Zaplify task
            folder.Items.Add(item = new Item() 
            { 
                Name = "Learn about Zaplify!", 
                FolderID = folder.ID, 
                ItemTypeID = SystemItemTypes.Task,
                SortOrder = 2000,
                ParentID = item.ID,             // add to Tasks
                Complete = false,
                Due = DateTime.Today.Date,
                Priority = 0,
                Description = "Tap the browse button below to discover more about Zaplify.",
            });
            StorageHelper.WriteFolder(folder);

            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord() { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = item, ID = item.ID });


            // create the People folder
            folders.Add(folder = new Folder() { Name = "People", Items = new ObservableCollection<Item>(), ItemTypeID = SystemItemTypes.Contact, SortOrder = 2000 });
            FolderDictionary.Add(folder.ID, folder);
            StorageHelper.WriteFolder(folder);

            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord() { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = folder, ID = folder.ID });

            // create the Places folder
            folders.Add(folder = new Folder() { Name = "Places", Items = new ObservableCollection<Item>(), ItemTypeID = SystemItemTypes.Location, SortOrder = 3000 });
            FolderDictionary.Add(folder.ID, folder);
            StorageHelper.WriteFolder(folder);

            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord() { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = folder, ID = folder.ID });

            // create the Lists folder
            folders.Add(folder = new Folder() { Name = "Lists", Items = new ObservableCollection<Item>(), ItemTypeID = SystemItemTypes.ListItem, SortOrder = 4000 });
            FolderDictionary.Add(folder.ID, folder);

            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord() { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = folder, ID = folder.ID });

            // create the Groceries list
            folder.Items.Add(item = new Item()
            {
                Name = "Groceries",
                FolderID = folder.ID,
                IsList = true,
                ItemTypeID = SystemItemTypes.ShoppingItem,
                SortOrder = 3000,
                ParentID = Guid.Empty
            });
            StorageHelper.WriteFolder(folder);

            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                { ReqType = RequestQueue.RequestRecord.RequestType.Insert, Body = item, ID = folder.ID });

            // save changes to local storage
            StorageHelper.WriteFolders(folders);
            
            return folders;
        }

#endregion
    }
}