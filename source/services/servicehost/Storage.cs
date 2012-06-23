namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
    using System.Collections.Generic;
    using System.Collections;

    // ****************************************************************************
    // static class for getting storage contexts
    // ****************************************************************************
    public static class Storage
    {
#if !DEBUG
        private static UserStorageContext staticUserContext;
#endif

        public static SuggestionsStorageContext NewSuggestionsContext
        {
            get { return new SuggestionsStorageContext(); }
        }

        public static UserStorageContext NewUserContext
        {
            get { return new UserStorageContext(); }
        }

        public static UserStorageContext StaticUserContext
        {   // use a static context to access static data (serving values out of EF cache)
            get
            {
#if DEBUG
                // if in a debug build, always go to the database
                return new UserStorageContext();
#else
                if (staticUserContext == null)
                {
                    staticUserContext = new UserStorageContext();
                }
                return staticUserContext;
#endif
            }
        }
    }

    // ****************************************************************************
    // storage context for the Suggestions database
    // ****************************************************************************
    public class SuggestionsStorageContext : DbContext
    {
        public SuggestionsStorageContext() : base(HostEnvironment.SuggestionsConnection) { }
        public SuggestionsStorageContext(string connection) : base(connection) { }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder) { }

        public DbSet<Intent> Intents { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<WorkflowType> WorkflowTypes { get; set; }
        public DbSet<DatabaseVersion> Versions { get; set; }

        // check if schema version in Suggestion database and WorkflowConstants match
        public bool CheckSchemaVersion()
        {   
            var match = Versions.Any(v => v.VersionType == DatabaseVersion.Schema && v.VersionString == WorkflowConstants.SchemaVersion);
            if (match == false)
            {
                TraceLog.TraceError(String.Format("Suggestions database schema version {0} not found", WorkflowConstants.SchemaVersion));
            }
            return match;
        }

        // update constants in Suggestion database to current version defined in WorkflowConstants
        public bool VersionConstants(string me)
        {   
            try
            {
                bool updateDB = false;
                if (Versions.Any(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == WorkflowConstants.ConstantsVersion) == false)
                {   // no database - create and lock the new version entry
                    TraceLog.TraceInfo(String.Format("Suggestions database version {0} not found", WorkflowConstants.ConstantsVersion));

                    // remove any existing database version (there should never be more than one)
                    foreach (var existingVersion in Versions.Where(v => v.VersionType == DatabaseVersion.Constants).ToList())
                    {
                        Versions.Remove(existingVersion);
                    }
                    SaveChanges();
                    
                    // create the new version entry
                    DatabaseVersion ver = new DatabaseVersion()
                    {
                        VersionType = DatabaseVersion.Constants,
                        VersionString = WorkflowConstants.ConstantsVersion,
                        Status = me
                    };
                    Versions.Add(ver);
                    SaveChanges();
                    updateDB = true;
                }
                else
                {
                    var dbVersion = Versions.Single(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == WorkflowConstants.ConstantsVersion);
                    if (dbVersion.Status == DatabaseVersion.Corrupted)
                    {   // try to update the database again - take a lock
                        TraceLog.TraceInfo("Suggestions database corrupted");
                        dbVersion.Status = me;
                        SaveChanges();
                        updateDB = true;
                    }
                }
                if (updateDB == false)
                {
                    TraceLog.TraceInfo(String.Format("Suggestions database version {0} is up to date", WorkflowConstants.ConstantsVersion));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Could not find database version", ex);
                return false;
            }

            // update the default database values
            DatabaseVersion version = null;
            SuggestionsStorageContext versionContext = Storage.NewSuggestionsContext;
            try
            {   // verify that this unit of execution owns the update lock for the database version
                version = versionContext.Versions.Single(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == WorkflowConstants.ConstantsVersion);
                if (version.Status != me)  
                    return true;

                TraceLog.TraceInfo(String.Format("{0} updating Suggestions database to version {1}", me, WorkflowConstants.ConstantsVersion));

                // remove existing intents 
                foreach (var entity in Intents.ToList()) { Intents.Remove(entity); }
                var intents = WorkflowConstants.DefaultIntents();
                if (intents == null)
                {
                    TraceLog.TraceError("Could not find or load intents");
                    version.Status = DatabaseVersion.Corrupted;
                    versionContext.SaveChanges();
                    return false;
                }
                // add current intents
                foreach (var entity in intents) { Intents.Add(entity); }
                SaveChanges();
                TraceLog.TraceInfo("Replaced intents");

                // remove existing workflow types
                foreach (var entity in WorkflowTypes.ToList()) { WorkflowTypes.Remove(entity); }
                var workflowTypes = WorkflowConstants.DefaultWorkflowTypes();
                if (workflowTypes == null)
                {
                    TraceLog.TraceError("Could not find or load workflow definitions");
                    version.Status = DatabaseVersion.Corrupted;
                    versionContext.SaveChanges();
                    return false;
                }
                // add current workflow types
                foreach (var entity in workflowTypes) { WorkflowTypes.Add(entity); }
                SaveChanges();
                TraceLog.TraceInfo("Replaced workflow types");

                // save the new version number
                version.Status = DatabaseVersion.OK;
                versionContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("VersionConstants failed", ex);
                // mark the version as corrupted
                version.Status = DatabaseVersion.Corrupted;
                versionContext.SaveChanges();
                return false;
            }
        }
    }

    // ****************************************************************************
    // storage context for the Users database
    // ****************************************************************************
    public class UserStorageContext : DbContext
    {
        public UserStorageContext() : base(HostEnvironment.UserDataConnection) { }
        public UserStorageContext(string connection) : base(connection) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) { }

        // constants shared tables
        public DbSet<ActionType> ActionTypes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Priority> Priorities { get; set; }
        public DbSet<DatabaseVersion> Versions { get; set; }

        // user data tables
        public DbSet<Field> Fields { get; set; }
        public DbSet<FieldValue> FieldValues { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<FolderUser> FolderUsers { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemTag> ItemTags { get; set; }
        public DbSet<ItemType> ItemTypes { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<User> Users { get; set; }

        // add an operation to the Operations table
        public Operation CreateOperation(User user, string opType, int? code, object body, object oldBody)
        {
            Operation operation = null;
            try
            {   // add the operation to the Operations table
                string name;
                Type bodyType = body.GetType();
                Guid id = (Guid)bodyType.GetProperty("ID").GetValue(body, null);
                if (body is Suggestion)
                {   // Suggestion does not have a Name property, use GroupDisplayName property
                    name = (string)bodyType.GetProperty("GroupDisplayName").GetValue(body, null);
                }
                else
                {
                    name = (string)bodyType.GetProperty("Name").GetValue(body, null);
                }

                operation = new Operation()
                {
                    ID = Guid.NewGuid(),
                    UserID = user.ID,
                    Username = user.Name,
                    EntityID = id,
                    EntityName = name,
                    EntityType = bodyType.Name,
                    OperationType = opType,
                    StatusCode = (int?)code,
                    Body = JsonSerializer.Serialize(body),
                    OldBody = JsonSerializer.Serialize(oldBody),
                    Timestamp = DateTime.Now
                };
                Operations.Add(operation);
                if (SaveChanges() < 1)
                {   // log failure to record operation
                    TraceLog.TraceError("Failed to record operation: " + opType);
                }
            }
            catch (Exception ex)
            {   // log failure to record operation
                TraceLog.TraceException("Failed to record operation", ex);
            }

            return operation;
        }
        
        // get User by ID (optionally include UserCredentials)
        public User GetUser(Guid id, bool includeCredentials = false)
        {
            try
            {
                if (includeCredentials)
                    return Users.Include("UserCredentials").Single(u => u.ID == id);
                else
                    return Users.Single(u => u.ID == id);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("User not found for ID: ", id), ex);
            }
            return null;
        }

        // get Item by ID (including FieldValues)
        public Item GetItem(User user, Guid itemID)
        {
            if (Items.Any(i => i.UserID == user.ID && i.ID == itemID))
            {
                return Items.Include("FieldValues").Single<Item>(i => i.UserID == user.ID && i.ID == itemID);
            }
            return null;
        }

        // get or create a Folder by name for given user
        public Folder GetOrCreateFolder(User user, string name, Guid itemTypeID)
        {
            try
            {   // get the folder by name for user
                if (Folders.Any(f => f.UserID == user.ID && f.Name == name))
                {
                    return Folders.Single(f => f.UserID == user.ID && f.Name == name);
                }
                else
                {   // create the folder with given name and itemTypeID for user
                    var folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = user.ID, PermissionID = BuiltSteady.Zaplify.Shared.Entities.Permissions.Full };
                    var folder = new Folder()
                    {
                        ID = folderUser.FolderID,
                        SortOrder = 0,
                        Name = name,
                        UserID = user.ID,
                        ItemTypeID = itemTypeID,
                        Items = new List<Item>(),
                        FolderUsers = new List<FolderUser>() { folderUser }
                    };
                    Folders.Add(folder);
                    SaveChanges();
                    TraceLog.TraceInfo(string.Format("Created folder named '{0}' for user '{1}'", name, user.Name));
                    return folder;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(string.Format("Could not find or create folder named '{0}' for user '{1}'", name, user.Name), ex);
                return null;
            }
        }

        // get or create a List by name in given folder for given user
        public Item GetOrCreateList(User user, Folder folder, string name, Guid? itemTypeID = null)
        {
            if (itemTypeID == null) { itemTypeID = SystemItemTypes.NameValue; }
            try
            {   // get the list with given name in given folder
                if (Items.Any(i => i.UserID == user.ID && i.FolderID == folder.ID && i.Name == name))
                {
                    return Items.Include("FieldValues").Single(i => i.UserID == user.ID && i.FolderID == folder.ID && i.Name == name);
                }
                else
                {   // create new list with given name in given folder
                    DateTime now = DateTime.UtcNow;
                    var list = new Item()
                    {
                        ID = Guid.NewGuid(),
                        Name = name,
                        FolderID = folder.ID,
                        UserID = user.ID,
                        IsList = true,
                        ItemTypeID = itemTypeID.Value,
                        ParentID = null,
                        Created = now,
                        LastModified = now
                    };
                    Items.Add(list);
                    SaveChanges();
                    TraceLog.TraceInfo(string.Format("Created list named '{0}' in folder '{1}' for user '{2}'", name, folder.Name, user.Name));
                    return list;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(string.Format("Could not find or create list named '{0}' in folder '{1}' for user '{2}'", name, folder.Name, user.Name), ex);
                return null;
            }
        }

        // get or create a List by value in given folder for given user
        public Item GetOrCreateListByValue(User user, Folder folder, string value, string name, Guid? itemTypeID = null)
        {
            if (itemTypeID == null) { itemTypeID = SystemItemTypes.NameValue; }
            try
            {   // get the list with given value in given folder
                if (Items.Any(i => i.UserID == user.ID && i.FolderID == folder.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.Value && fv.Value == value)))
                {
                    return Items.Single(i => i.UserID == user.ID && i.FolderID == folder.ID && 
                        i.FieldValues.Any(fv => fv.FieldName == FieldNames.Value && fv.Value == value));
                }
                else
                {   // create new list with given value and name in given folder
                    DateTime now = DateTime.UtcNow;
                    var list = new Item()
                    {
                        ID = Guid.NewGuid(),
                        Name = name,
                        FolderID = folder.ID,
                        UserID = user.ID,
                        IsList = true,
                        ItemTypeID = itemTypeID.Value,
                        ParentID = null,
                        Created = now,
                        LastModified = now,
                        FieldValues = new List<FieldValue>()
                    };
                    list.GetFieldValue(FieldNames.Value, true).Value = value;
                    Items.Add(list);
                    SaveChanges();
                    TraceLog.TraceInfo(string.Format("Created list by value '{0}' in folder '{1}' for user '{2}'", value, folder.Name, user.Name));
                    return list;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(string.Format("Could not find or create list by value '{0}' in folder '{1}' for user '{2}'", value, folder.Name, user.Name), ex);
                return null;
            }
        }

        // check if schema version in User database and EntityConstants match
        public bool CheckSchemaVersion()
        {
            var match = Versions.Any(v => v.VersionType == DatabaseVersion.Schema && v.VersionString == UserConstants.SchemaVersion);
            if (match == false)
            {
                TraceLog.TraceError(String.Format("User database schema version {0} not found", UserConstants.SchemaVersion));
            }
            return match;
        }

        // update constants in User database to current version defined in EntityConstants
        public bool VersionConstants(string me)
        {
            try
            {
                bool updateDB = false;
                if (Versions.Any(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == UserConstants.ConstantsVersion) == false)
                {   // no database - create and lock the new version entry
                    TraceLog.TraceInfo(String.Format("User database version {0} not found", UserConstants.ConstantsVersion));

                    // remove an existing database version (there should never be more than one)
                    foreach (var existingVersion in Versions.Where(v => v.VersionType == DatabaseVersion.Constants).ToList())
                    {
                        Versions.Remove(existingVersion);
                    }
                    SaveChanges();

                    // create the new version entry
                    DatabaseVersion ver = new DatabaseVersion()
                    {
                        VersionType = DatabaseVersion.Constants,
                        VersionString = UserConstants.ConstantsVersion,
                        Status = me
                    };
                    Versions.Add(ver);
                    SaveChanges();
                    updateDB = true;
                }
                else
                {
                    var dbVersion = Versions.Single(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == UserConstants.ConstantsVersion);
                    if (dbVersion.Status == DatabaseVersion.Corrupted)
                    {   // try to update the database again - take a lock
                        TraceLog.TraceInfo("User database corrupted");
                        dbVersion.Status = me;
                        SaveChanges();
                        updateDB = true;
                    }
                }
                if (updateDB == false)
                {
                    TraceLog.TraceInfo(String.Format("User database version {0} is up to date", UserConstants.ConstantsVersion));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Could not find database version", ex);
                return false;
            }

            // update the default database values
            DatabaseVersion version = null;
            UserStorageContext versionContext = Storage.NewUserContext;
            try
            {   // verify that this unit of execution owns the update lock for the database version
                version = versionContext.Versions.Single(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == UserConstants.ConstantsVersion);
                if (version.Status != me)
                    return true;

                TraceLog.TraceInfo(String.Format("{0} updating User datatbase to version {1}", me, UserConstants.ConstantsVersion));

                // update existing action types, add new action types
                foreach (var entity in UserConstants.DefaultActionTypes())
                {
                    if (ActionTypes.Any(e => e.ActionTypeID == entity.ActionTypeID))
                        ActionTypes.Single(e => e.ActionTypeID == entity.ActionTypeID).Copy(entity);
                    else
                        ActionTypes.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("Replaced action types");

                // update existing colors, add new colors
                foreach (var entity in UserConstants.DefaultColors())
                {
                    if (Colors.Any(e => e.ColorID == entity.ColorID))
                        Colors.Single(e => e.ColorID == entity.ColorID).Copy(entity);
                    else
                        Colors.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("Replaced colors");

                // update existing permissions, add new permissions
                foreach (var entity in UserConstants.DefaultPermissions())
                {
                    if (Permissions.Any(e => e.PermissionID == entity.PermissionID))
                        Permissions.Single(e => e.PermissionID == entity.PermissionID).Copy(entity);
                    else
                        Permissions.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("Replaced permissions");

                // update existing priorities, add new priorities
                foreach (var entity in UserConstants.DefaultPriorities())
                {
                    if (Priorities.Any(e => e.PriorityID == entity.PriorityID))
                        Priorities.Single(e => e.PriorityID == entity.PriorityID).Copy(entity);
                    else
                        Priorities.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("Replaced priorities");

                // update existing or add new built-in users
                foreach (var user in UserConstants.DefaultUsers())
                {
                    if (Users.Any(u => u.ID == user.ID))
                    {
                        var existing = Users.Single(u => u.ID == user.ID);
                        existing.Name = user.Name;
                        existing.Email = user.Email;
                        existing.CreateDate = user.CreateDate;
                    }
                    else
                        Users.Add(user);
                }
                SaveChanges();
                TraceLog.TraceInfo("Replaced users");

                // update existing or add new built-in itemtypes and fields
                foreach (var itemType in UserConstants.DefaultItemTypes())
                {
                    if (ItemTypes.Any(it => it.ID == itemType.ID))
                    {
                        var existing = ItemTypes.Include("Fields").Single(it => it.ID == itemType.ID);
                        existing.Copy(itemType);
                        if (itemType.Fields == null)
                            continue;
                        foreach (var field in itemType.Fields)
                        {
                            if (existing.Fields.Any(f => f.ID == field.ID))
                            {
                                var existingField = existing.Fields.Single(f => f.ID == field.ID);
                                existingField.Copy(field);
                            }
                            else
                                existing.Fields.Add(field);
                        }
                    }
                    else
                        ItemTypes.Add(itemType);
                }
                SaveChanges();
                TraceLog.TraceInfo("Replaced item types and fields");

                // save the new version number
                version.Status = DatabaseVersion.OK;
                versionContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("VersionConstants failed", ex);

                // mark the version as corrupted
                version.Status = DatabaseVersion.Corrupted;
                versionContext.SaveChanges();
                return false;
            }
        }


        // ****************************************************************************
        // nested accessor for UserFolder
        // ****************************************************************************
        private ServerUserFolder userFolder;
        public ServerUserFolder UserFolder
        {
            get
            {
                if (userFolder == null) { userFolder = new ServerUserFolder(this); }
                return userFolder;
            }
        }

        public class ServerUserFolder
        {
            UserStorageContext storage;
            public ServerUserFolder(UserStorageContext storage)
            {
                this.storage = storage;
            }

            // get or create the UserFolder for given user
            public Folder Get(User user)
            {
                return storage.GetOrCreateFolder(user, SystemEntities.User, SystemItemTypes.System);
            }

            // get or create the EntityRefs list in the UserFolder for given user
            public Item GetEntityRefsList(User user)
            {
                Folder userFolder = Get(user);
                if (userFolder != null)
                {
                    return storage.GetOrCreateList(user, userFolder, SystemEntities.EntityRefs);
                }
                return null;
            }

            // get or create a list for an ItemType in the UserFolder for given user
            public Item GetListForItemType(User user, Guid itemTypeID)
            {
                Folder userFolder = Get(user);
                if (userFolder != null)
                {
                    return storage.GetOrCreateListByValue(user, userFolder, itemTypeID.ToString(), SystemItemTypes.Names[itemTypeID]);
                }
                return null;
            }

            // get or create an reference to the given entity in the UserFolder EntityRefs list
            public Item GetEntityRef(User user, ServerEntity entity)
            {
                Item entityRefsList = GetEntityRefsList(user);
                if (entityRefsList == null)
                    return null;

                var entityID = entity.ID.ToString();
                try
                {   // get existing reference to given entity
                    if (storage.Items.Include("FieldValues").Any(i => i.UserID == user.ID && i.FolderID == entityRefsList.FolderID && i.ParentID == entityRefsList.ID &&
                        i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == entityID)))
                    {
                        return storage.Items.Include("FieldValues").Single(i => i.UserID == user.ID && i.FolderID == entityRefsList.FolderID && i.ParentID == entityRefsList.ID &&
                            i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == entityID));
                    }
                    else
                    {   // create new reference to given entity 
                        DateTime now = DateTime.UtcNow;
                        var entityRefItemID = Guid.NewGuid();
                        var entityRefItem = new Item()
                        {
                            ID = entityRefItemID,
                            Name = entity.Name,
                            FolderID = entityRefsList.FolderID,
                            UserID = user.ID,
                            ItemTypeID = SystemItemTypes.Reference,
                            ParentID = entityRefsList.ID,
                            Created = now,
                            LastModified = now,
                            FieldValues = new List<FieldValue>()
                            {
                                new FieldValue() { ItemID = entityRefItemID, FieldName = FieldNames.EntityRef, Value = entityID },
                                new FieldValue() { ItemID = entityRefItemID, FieldName = FieldNames.EntityType, Value = entity.GetType().Name },
                            }
                        };
                        storage.Items.Add(entityRefItem);
                        storage.SaveChanges();
                        TraceLog.TraceInfo(String.Format("Created entity ref item {0} for user {1}", entity.Name, user.Name));
                        return entityRefItem;
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException(String.Format("Created entity ref item {0} for user {1}", entity.Name, user.Name), ex);
                    return null;
                }
            }
        }

        // ****************************************************************************
        // nested accessor for ClientFolder
        // ****************************************************************************
        private ClientUserFolder clientFolder;
        public ClientUserFolder ClientFolder
        {
            get
            {
                if (clientFolder == null) { clientFolder = new ClientUserFolder(this); }
                return clientFolder;
            }
        }

        public class ClientUserFolder
        {
            UserStorageContext storage;
            public ClientUserFolder(UserStorageContext storage)
            {
                this.storage = storage;
            }

            // get or create the ClientFolder for given user
            public Folder Get(User user)
            {
                return storage.GetOrCreateFolder(user, SystemEntities.ClientSettings, SystemItemTypes.NameValue);
            }

            // get or create the UserProfile list in the ClientSettings for given user
            public Item GetUserProfile(User user)
            {
                Folder clientFolder = Get(user);
                if (clientFolder != null)
                {
                    return storage.GetOrCreateList(user, clientFolder, SystemEntities.UserProfile);
                }
                return null;
            }
        }
    }
}