using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Web.Configuration;
using TaskStoreServerEntities;

namespace TaskStoreWeb.Models
{
    public class TaskStore : DbContext
    {
        // the default constructor loads the Connection appsetting (from web.config) 
        // which is the alias of the correct connection string (also from web.config)
        public TaskStore() : base(WebConfigurationManager.AppSettings["Connection"]) { }

        public TaskStore(string connstr) : base(connstr) { }

        private static TaskStore current;
        public static TaskStore Current
        {
            get
            {
                // only return a new context if one hasn't already been created
                if (current == null)
                {
                    current = new TaskStore();
                }
                return current;
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        // constant / shared tables
        public DbSet<TaskStoreServerEntities.Action> Actions { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<FieldType> FieldTypes { get; set; }
        public DbSet<Priority> Priorities { get; set; }

        // user-specific tables
        public DbSet<Field> Fields { get; set; }
        public DbSet<ListType> ListTypes { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TaskTag> TaskTags { get; set; }
        public DbSet<TaskList> TaskLists { get; set; }
        public DbSet<User> Users { get; set; }
    }
}