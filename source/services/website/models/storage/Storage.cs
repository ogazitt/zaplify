namespace BuiltSteady.Zaplify.Website.Models
{
    using System.Data.Entity;
    using System.Web.Configuration;
    using BuiltSteady.Zaplify.ServerEntities;

    public static class Storage
    {
        private static StorageContext staticContext;

        public static StorageContext NewContext
        {
            get { return new StorageContext(); }
        }

        public static CredentialStorageContext NewCredentialContext
        {
            get { return new CredentialStorageContext(); }
        }

        public static StorageContext StaticContext
        {   // use a static context to access static data (serving values out of EF cache)
            get
            {
                if (staticContext == null)
                {
                    staticContext = new StorageContext();
                }
#if DEBUG
                // if in a debug build, always go to the database
                return new StorageContext();
#else
                return staticContext;
#endif
            }
        }
    }

    public class StorageContext : DbContext
    {
        // the default constructor loads the Connection appsetting (from web.config) 
        // which is the alias of the correct connection string (also from web.config)
        public StorageContext() : base(WebConfigurationManager.AppSettings["Connection"]) { }
        public StorageContext(string connstr) : base(connstr) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        // constant / shared tables
        public DbSet<ActionType> ActionTypes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<FieldType> FieldTypes { get; set; }
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
    }

    public class CredentialStorageContext : DbContext
    {
        // the default constructor loads the Connection appsetting (from web.config) 
        // which is the alias of the correct connection string (also from web.config)
        public CredentialStorageContext() : base(WebConfigurationManager.AppSettings["Connection"]) { }
        public CredentialStorageContext(string connstr) : base(connstr) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCredential>().Map(m => { m.ToTable("Users"); });
        }
        
        public DbSet<UserCredential> Credentials { get; set; }
    }
}