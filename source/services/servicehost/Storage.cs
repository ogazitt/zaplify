namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
    using System.Collections.Generic;

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

        public Folder GetOrCreateUserFolder(User user)
        {
            try
            {
                // get the $User folder
                if (Folders.Any(f => f.UserID == user.ID && f.Name == SystemFolders.User))
                    return Folders.Single(f => f.UserID == user.ID && f.Name == SystemFolders.User);
                else
                {
                    // create the $User folder
                    var folderUser = new FolderUser() { ID = Guid.NewGuid(), FolderID = Guid.NewGuid(), UserID = user.ID, PermissionID = BuiltSteady.Zaplify.Shared.Entities.Permissions.Full };
                    var userFolder = new Folder()
                    {
                        ID = folderUser.FolderID,
                        SortOrder = 0,
                        Name = SystemFolders.User,
                        UserID = user.ID,
                        ItemTypeID = SystemItemTypes.NameValue,
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