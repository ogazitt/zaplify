namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
    using System.Collections.Generic;
    using System.Collections;

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

    // DbContext for the suggestions DB
    public class SuggestionsStorageContext : DbContext
    {
        public SuggestionsStorageContext() : base(HostEnvironment.SuggestionsConnection) { }
        public SuggestionsStorageContext(string connection) : base(connection) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public DbSet<Intent> Intents { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<WorkflowType> WorkflowTypes { get; set; }
        public DbSet<DatabaseVersion> Versions { get; set; }


        /// <summary>
        /// Check whether the schema version in the database is what the compiled code expects
        /// </summary>
        /// <returns>true if the schema versions match, false if the database is out of sync (fatal error)</returns>
        public bool CheckSchemaVersion()
        {
            var current = Versions.Any(v => v.VersionType == DatabaseVersion.Schema && v.VersionString == WorkflowConstants.SchemaVersion);
            if (current == false)
                TraceLog.TraceError(String.Format("SuggestionsStorageContext.CheckSchemaVersion: Suggestions database schema version {0} not found", WorkflowConstants.SchemaVersion));
            return current;
        }

        /// <summary>
        /// Initialize or update the database version if necessary
        /// </summary>
        /// <param name="me">Identity of the current unit of execution</param>
        /// <returns>true for success, false if database is broken</returns>
        public bool VersionConstants(string me)
        {
            try
            {
                bool updateDB = false;
                if (Versions.Any(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == WorkflowConstants.ConstantsVersion) == false)
                {
                    // no database - create and lock the new version entry
                    TraceLog.TraceInfo(String.Format("SuggestionsStorageContext.VersionConstants: Suggestions database version {0} not found", WorkflowConstants.ConstantsVersion));

                    // remove any existing database version (there should never be more than one...)
                    foreach (var existingVersion in Versions.Where(v => v.VersionType == DatabaseVersion.Constants).ToList())
                        Versions.Remove(existingVersion);
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
                    {
                        // try to update the database again - take a lock
                        TraceLog.TraceInfo("SuggestionsStorageContext.VersionConstants: Suggestions database corrupted");
                        dbVersion.Status = me;
                        SaveChanges();
                        updateDB = true;
                    }
                }
                if (updateDB == false)
                {
                    TraceLog.TraceInfo(String.Format("SuggestionsStorageContext.VersionConstants: Suggestions database version {0} is up to date",
                        WorkflowConstants.ConstantsVersion));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("SuggestionsStorageContext.VersionConstants: could not find database version", ex);
                return false;
            }

            // update the default database values
            SuggestionsStorageContext versionContext = Storage.NewSuggestionsContext;
            DatabaseVersion version = versionContext.Versions.Single(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == WorkflowConstants.ConstantsVersion);
            try
            {
                // verify that this unit of execution owns the update lock for the database version
                if (version.Status != me)  // someone else is update the database
                    return true;

                TraceLog.TraceInfo(String.Format("SuggestionsStorageContext.VersionConstants: {0} updating Suggestions datatbase to version {1}",
                    me, WorkflowConstants.ConstantsVersion));

                // replace intents 
                foreach (var entity in Intents.ToList())
                    Intents.Remove(entity);
                var intents = WorkflowConstants.DefaultIntents();
                if (intents == null)
                {
                    TraceLog.TraceError("SuggestionsStorageContext.VersionConstants: could not find or load intents");
                    version.Status = DatabaseVersion.Corrupted;
                    versionContext.SaveChanges();
                    return false;
                }
                foreach (var entity in intents)
                    Intents.Add(entity);
                SaveChanges();
                TraceLog.TraceInfo("SuggestionsStorageContext.VersionConstants: replaced intents");

                // replace workflow types
                foreach (var entity in WorkflowTypes.ToList())
                    WorkflowTypes.Remove(entity);
                var workflowTypes = WorkflowConstants.DefaultWorkflowTypes();
                if (workflowTypes == null)
                {
                    TraceLog.TraceError("SuggestionsStorageContext.VersionConstants: could not find or load workflow definitions");
                    version.Status = DatabaseVersion.Corrupted;
                    versionContext.SaveChanges();
                    return false;
                }
                foreach (var entity in workflowTypes)
                    WorkflowTypes.Add(entity);
                SaveChanges();
                TraceLog.TraceInfo("SuggestionsStorageContext.VersionConstants: replaced workflow types");

                // save the new version number
                version.Status = DatabaseVersion.OK;
                versionContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("SuggestionsStorageContext.VersionConstants failed", ex);

                // mark the version as corrupted
                version.Status = DatabaseVersion.Corrupted;
                versionContext.SaveChanges();
                return false;
            }
        }
    }

    // DbContext for the user DB
    public class UserStorageContext : DbContext
    {
        public UserStorageContext() : base(HostEnvironment.UserDataConnection) { }
        public UserStorageContext(string connection) : base(connection) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        // constant / shared tables
        public DbSet<ActionType> ActionTypes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Priority> Priorities { get; set; }
        public DbSet<DatabaseVersion> Versions { get; set; }

        // user-specific tables
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

        /// <summary>
        /// Check whether the schema version in the database is what the compiled code expects
        /// </summary>
        /// <returns>true if the schema versions match, false if the database is out of sync (fatal error)</returns>
        public bool CheckSchemaVersion()
        {
            var current = Versions.Any(v => v.VersionType == DatabaseVersion.Schema && v.VersionString == UserConstants.SchemaVersion);
            if (current == false)
                TraceLog.TraceError(String.Format("UserStorageContext.CheckSchemaVersion: User database schema version {0} not found", UserConstants.SchemaVersion));
            return current;
        }

        /// <summary>
        /// Initialize or update the database version if necessary
        /// <param name="me">Identity of the current unit of execution</param>
        /// </summary>
        /// <returns>true for success, false if database is broken</returns>
        public bool VersionConstants(string me)
        {
            try
            {
                bool updateDB = false;
                if (Versions.Any(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == UserConstants.ConstantsVersion) == false)
                {
                    // no database - create and lock the new version entry
                    TraceLog.TraceInfo(String.Format("UserStorageContext.VersionConstants: User database version {0} not found", UserConstants.ConstantsVersion));

                    // remove an existing database version (there should never be more than one...)
                    foreach (var existingVersion in Versions.Where(v => v.VersionType == DatabaseVersion.Constants).ToList())
                        Versions.Remove(existingVersion);
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
                    {
                        // try to update the database again - take a lock
                        TraceLog.TraceInfo("UserStorageContext.VersionConstants: User database corrupted");
                        dbVersion.Status = me;
                        SaveChanges();
                        updateDB = true;
                    }
                }
                if (updateDB == false)
                {
                    TraceLog.TraceInfo(String.Format("UserStorageContext.VersionConstants: User database version {0} is up to date",
                        UserConstants.ConstantsVersion));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("UserStorageContext.VersionConstants: could not find database version", ex);
                return false;
            }

            // update the default database values
            DatabaseVersion version = null;
            UserStorageContext versionContext = Storage.NewUserContext;
            try
            {
                // verify that this unit of execution owns the update lock for the database version
                version = versionContext.Versions.Single(v => v.VersionType == DatabaseVersion.Constants && v.VersionString == UserConstants.ConstantsVersion);
                if (version.Status != me)  // someone else is update the database
                    return true;

                TraceLog.TraceInfo(String.Format("UserStorageContext.VersionConstants: {0} updating User datatbase to version {1}",
                    me, UserConstants.ConstantsVersion));

                // replace action types
                foreach (var entity in UserConstants.DefaultActionTypes())
                {
                    if (ActionTypes.Any(e => e.ActionTypeID == entity.ActionTypeID))
                    {
                        var existing = ActionTypes.Single(e => e.ActionTypeID == entity.ActionTypeID);
                        existing.Copy(entity);
                    }
                    else
                        ActionTypes.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("UserStorageContext.VersionConstants: replaced action types");

                // replace colors
                foreach (var entity in UserConstants.DefaultColors())
                {
                    if (Colors.Any(e => e.ColorID == entity.ColorID))
                    {
                        var existing = Colors.Single(e => e.ColorID == entity.ColorID);
                        existing.Copy(entity);
                    }
                    else
                        Colors.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("UserStorageContext.VersionConstants: replaced colors");

                // replace permissions
                foreach (var entity in UserConstants.DefaultPermissions())
                {
                    if (Permissions.Any(e => e.PermissionID == entity.PermissionID))
                    {
                        var existing = Permissions.Single(e => e.PermissionID == entity.PermissionID);
                        existing.Copy(entity);
                    }
                    else
                        Permissions.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("UserStorageContext.VersionConstants: replaced permissions");

                // replace priorities
                foreach (var entity in UserConstants.DefaultPriorities())
                {
                    if (Priorities.Any(e => e.PriorityID == entity.PriorityID))
                    {
                        var existing = Priorities.Single(e => e.PriorityID == entity.PriorityID);
                        existing.Copy(entity);
                    }
                    else
                        Priorities.Add(entity);
                }
                SaveChanges();
                TraceLog.TraceInfo("UserStorageContext.VersionConstants: replaced priorities");

                // replace built-in users 
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
                TraceLog.TraceInfo("UserStorageContext.VersionConstants: replaced users");

                // replace built-in itemtypes and fields
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
                TraceLog.TraceInfo("UserStorageContext.VersionConstants: replaced item types and fields");

                // save the new version number
                version.Status = DatabaseVersion.OK;
                versionContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("UserStorageContext.VersionConstants failed", ex);

                // mark the version as corrupted
                version.Status = DatabaseVersion.Corrupted;
                versionContext.SaveChanges();
                return false;
            }            
        }

        public Operation CreateOperation(User user, string opType, int? code, object body, object oldBody)
        {
            Operation operation = null;
            try
            {
                // log the operation in the operations table
                Type bodyType = body.GetType();

                string name;
                Guid id = (Guid)bodyType.GetProperty("ID").GetValue(body, null);
                if (body is Suggestion)
                {   // Suggestion does not have a Name property, use State property
                    name = (string)bodyType.GetProperty("GroupDisplayName").GetValue(body, null);
                }
                else
                {
                    name = (string)bodyType.GetProperty("Name").GetValue(body, null);
                }

                // record the operation in the Operations table
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
                    TraceLog.TraceError("CreateOperation: failed to record operation: " + opType);
                }
            }
            catch (Exception ex)
            {   // log failure to record operation
                TraceLog.TraceException("CreateOperation: failed to record operation", ex);
            }

            return operation;
        }

        /// <summary>
        /// Get the User that owns the current Item
        /// </summary>
        /// <param name="item">Item to get the user for</param>
        /// <returns>User that owns the item</returns>
        public User CurrentUser(Item item)
        {
            if (item == null)
                return null;
            try
            {
                return Users.Include("UserCredentials").Single(u => u.ID == item.UserID);
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("CurrentUser: User for item {0} not found", item.Name), ex);
                return null;
            }
        }

        public Item GetOrCreateUserItemTypeList(User user, Guid itemTypeID)
        {
            return GetOrCreateUserList(user, SystemItemTypes.Names[itemTypeID], SystemItemTypes.NameValue);
        }

        public Item GetOrCreateEntityRef(User user, ServerEntity entity)
        {
            Item entityRefList = GetOrCreateEntityRefList(user);
            if (entityRefList == null)
                return null;

            var entityID = entity.ID.ToString();

            // retrieve the entity ref item inside the $User folder
            try
            {
                // get the entity ref item
                if (Items.Include("FieldValues").Any(i => i.UserID == user.ID && i.FolderID == entityRefList.FolderID && i.ParentID == entityRefList.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == entityID)))
                {
                    return Items.Include("FieldValues").Single(i => i.UserID == user.ID && i.FolderID == entityRefList.FolderID && i.ParentID == entityRefList.ID &&
                        i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == entityID));
                }
                else
                {
                    // create entity ref item 
                    DateTime now = DateTime.UtcNow;
                    var entityRefItemID = Guid.NewGuid();
                    var entityRefItem = new Item()
                    {
                        ID = entityRefItemID,
                        Name = entity.Name,
                        FolderID = entityRefList.FolderID,
                        UserID = user.ID,
                        ItemTypeID = SystemItemTypes.Reference,
                        ParentID = entityRefList.ID,
                        Created = now,
                        LastModified = now,
                        FieldValues = new List<FieldValue>()
                        {
                            new FieldValue()
                            {
                                ItemID = entityRefItemID,
                                FieldName = FieldNames.EntityRef,
                                Value = entityID,
                            },
                            new FieldValue()
                            {
                                ItemID = entityRefItemID,
                                FieldName = FieldNames.EntityType,
                                Value = entity.GetType().Name,
                            },
                        }
                    };
                    Items.Add(entityRefItem);
                    SaveChanges();
                    TraceLog.TraceInfo(String.Format("GetOrCreateEntityRef: created entity ref item {0} for user {1}", entity.Name, user.Name));
                    return entityRefItem;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("GetOrCreateEntityRef: created entity ref item {0} for user {1}", entity.Name, user.Name), ex);
                return null;
            }
        }

        public Item GetOrCreateEntityRefList(User user)
        {
            return GetOrCreateUserList(user, SystemEntities.EntityRefs, SystemItemTypes.Reference);
        }

        public Folder GetOrCreateUserFolder(User user)
        {
            try
            {
                // get the $User folder
                if (Folders.Any(f => f.UserID == user.ID && f.Name == SystemEntities.User))
                    return Folders.Single(f => f.UserID == user.ID && f.Name == SystemEntities.User);
                else
                {
                    // create the $User folder
                    var folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = user.ID, PermissionID = BuiltSteady.Zaplify.Shared.Entities.Permissions.Full };
                    var userFolder = new Folder()
                    {
                        ID = folderUser.FolderID,
                        SortOrder = 0,
                        Name = SystemEntities.User,
                        UserID = user.ID,
                        ItemTypeID = SystemItemTypes.System,
                        Items = new List<Item>(),
                        FolderUsers = new List<FolderUser>() { folderUser }
                    };
                    Folders.Add(userFolder);
                    SaveChanges();
                    TraceLog.TraceInfo("GetOrCreateUserFolder: created $User folder for user " + user.Name);
                    return userFolder;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("GetOrCreateUserFolder: could not find or create $User folder", ex);
                return null;
            }
        }

        public Item GetOrCreateUserList(User user, string listName, Guid itemTypeID)
        {
            Folder userFolder = GetOrCreateUserFolder(user);
            if (userFolder == null)
                return null;

            // retrieve the list inside the $User folder
            try
            {
                // get the list
                if (Items.Any(i => i.UserID == user.ID && i.FolderID == userFolder.ID && i.Name == listName))
                    return Items.Single(i => i.UserID == user.ID && i.FolderID == userFolder.ID && i.Name == listName);
                else
                {
                    // create list
                    DateTime now = DateTime.UtcNow;
                    var list = new Item()
                    {
                        ID = Guid.NewGuid(),
                        Name = listName,
                        FolderID = userFolder.ID,
                        UserID = user.ID,
                        IsList = true,
                        ItemTypeID = SystemItemTypes.NameValue,
                        ParentID = null,
                        Created = now,
                        LastModified = now
                    };
                    Items.Add(list);
                    SaveChanges();
                    TraceLog.TraceInfo(String.Format("GetOrCreateUserFolderList: created {0} list for user {1}", listName, user.Name));
                    return list;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("GetOrCreateUserFolderList: could not find or create {0} list for user {1}", listName, user.Name), ex);
                return null;
            }
        }
    }
}