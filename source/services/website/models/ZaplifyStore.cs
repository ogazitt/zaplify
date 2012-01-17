using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Web.Configuration;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.Website.Models
{
    public class ZaplifyStore : DbContext
    {
        // the default constructor loads the Connection appsetting (from web.config) 
        // which is the alias of the correct connection string (also from web.config)
        public ZaplifyStore() : base(WebConfigurationManager.AppSettings["Connection"]) { }

        public ZaplifyStore(string connstr) : base(connstr) { }

        private static ZaplifyStore current;
        public static ZaplifyStore Current
        {
            get
            {
                // only return a new context if one hasn't already been created
                if (current == null)
                {
                    current = new ZaplifyStore();
                }
                return current;
            }
        }

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
}