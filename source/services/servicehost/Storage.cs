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
        /// Initialize or update the database version if necessary
        /// </summary>
        /// <param name="me">Identity of the current unit of execution</param>
        /// <returns>true for success, false if database is broken</returns>
        public bool VersionDatabase(string me)
        {
            try
            {
                bool updateDB = false;
                if (Versions.Any(v => v.VersionString == HostEnvironment.SuggestionsDatabaseVersion) == false)
                {
                    // no database - create and lock the new version entry
                    DatabaseVersion version = new DatabaseVersion() { VersionString = HostEnvironment.SuggestionsDatabaseVersion, Updating = me };
                    Versions.Add(version);
                    SaveChanges();
                    updateDB = true;
                }
                else
                {
                    var dbVersion = Versions.Single(v => v.VersionString == HostEnvironment.SuggestionsDatabaseVersion);
                    if (dbVersion.Updating == DatabaseVersion.Corrupted)
                    {
                        // try to update the database again - take a lock
                        dbVersion.Updating = me;
                        SaveChanges();
                        updateDB = true;
                    }
                }
                if (updateDB == false)
                    return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("SuggestionsStorageContext.VersionDatabase: could not find database version", ex);
                return false;
            }

            // update the default database values
            try
            {
                // verify that this unit of execution owns the update lock for the database version
                SuggestionsStorageContext versionContext = Storage.NewSuggestionsContext;
                DatabaseVersion version = versionContext.Versions.Single(v => v.VersionString == HostEnvironment.SuggestionsDatabaseVersion);
                if (version.Updating != me)  // someone else is update the database
                    return true;

                // replace intents 
                foreach (var entity in Intents.ToList())
                    Intents.Remove(entity);
                var intents = WorkflowConstants.DefaultIntents();
                if (intents == null)
                {
                    TraceLog.TraceError("SuggestionsStorageContext.VersionDatabase: could not find or load intents");
                    version.Updating = DatabaseVersion.Corrupted;
                    versionContext.SaveChanges();
                    return false;
                }
                foreach (var entity in intents)
                    Intents.Add(entity);
                SaveChanges();

                // replace workflow types
                foreach (var entity in WorkflowTypes.ToList())
                    WorkflowTypes.Remove(entity);
                var workflowTypes = WorkflowConstants.DefaultWorkflowTypes();
                if (workflowTypes == null)
                {
                    TraceLog.TraceError("SuggestionsStorageContext.VersionDatabase: could not find or load workflow definitions");
                    version.Updating = DatabaseVersion.Corrupted;
                    versionContext.SaveChanges();
                    return false;
                }
                foreach (var entity in workflowTypes)
                    WorkflowTypes.Add(entity);
                SaveChanges();

                // save the new version number
                version.Updating = DatabaseVersion.OK;
                versionContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("SuggestionsStorageContext.VersionDatabase failed", ex);
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
        /// Initialize or update the database version if necessary
        /// <param name="me">Identity of the current unit of execution</param>
        /// </summary>
        /// <returns>true for success, false if database is broken</returns>
        public bool VersionDatabase(string me)
        {
            try
            {
                bool updateDB = false;
                if (Versions.Any(v => v.VersionString == HostEnvironment.UserDatabaseVersion) == false)
                {
                    // no database - create and lock the new version entry
                    DatabaseVersion ver = new DatabaseVersion() { VersionString = HostEnvironment.UserDatabaseVersion, Updating = me };
                    Versions.Add(ver);
                    SaveChanges();
                    updateDB = true;
                }
                else
                {
                    var dbVersion = Versions.Single(v => v.VersionString == HostEnvironment.UserDatabaseVersion);
                    if (dbVersion.Updating == DatabaseVersion.Corrupted)
                    {
                        // try to update the database again - take a lock
                        dbVersion.Updating = me;
                        SaveChanges();
                        updateDB = true;
                    }
                }
                if (updateDB == false)
                    return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("UserStorageContext.VersionDatabase: could not find database version", ex);
                return false;
            }

            // update the default database values
            DatabaseVersion version = null;
            UserStorageContext versionContext = Storage.NewUserContext;
            try
            {
                // verify that this unit of execution owns the update lock for the database version
                version = versionContext.Versions.Single(v => v.VersionString == HostEnvironment.UserDatabaseVersion);
                if (version.Updating != me)  // someone else is update the database
                    return true;

                // replace action types
                foreach (var entity in ActionTypes.ToList())
                    ActionTypes.Remove(entity);
                foreach (var entity in UserDatabase.DefaultActionTypes())
                    ActionTypes.Add(entity);
                SaveChanges();

                // replace colors
                foreach (var entity in Colors.ToList())
                    Colors.Remove(entity);
                foreach (var entity in UserDatabase.DefaultColors())
                    Colors.Add(entity);
                SaveChanges();

                // replace permissions
                foreach (var entity in Permissions.ToList())
                    Permissions.Remove(entity);
                foreach (var entity in UserDatabase.DefaultPermissions())
                    Permissions.Add(entity);
                SaveChanges();

                // replace priorities
                foreach (var entity in Priorities.ToList())
                    Priorities.Remove(entity);
                foreach (var entity in UserDatabase.DefaultPriorities())
                    Priorities.Add(entity);
                SaveChanges();

                // replace built-in users 
                foreach (var user in UserDatabase.DefaultUsers())
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

                // replace built-in itemtypes and fields
                foreach (var itemType in UserDatabase.DefaultItemTypes())
                {
                    if (ItemTypes.Any(it => it.ID == itemType.ID))
                    {
                        var existing = ItemTypes.Include("Fields").Single(it => it.ID == itemType.ID);
                        existing.Copy(itemType);
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

                // save the new version number
                version.Updating = DatabaseVersion.OK;
                versionContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("UserStorageContext.VersionDatabase failed", ex);

                // mark the version as corrupted
                version.Updating = DatabaseVersion.OK;
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
    }

}