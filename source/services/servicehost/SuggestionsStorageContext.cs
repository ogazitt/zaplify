namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using BuiltSteady.Zaplify.ServerEntities;

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

}